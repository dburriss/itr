module Itr.Cli.Program

open System
open Argu
open Itr.Domain
open Itr.Features
open Itr.Adapters

// ---------------------------------------------------------------------------
// Argu argument DUs
// ---------------------------------------------------------------------------

[<CliPrefix(CliPrefix.DoubleDash)>]
type TakeArgs =
    | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
    | Task_Id of task_id: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "backlog item id to take"
            | Task_Id _ -> "override the generated task id (single-repo items only)"

[<CliPrefix(CliPrefix.DoubleDash)>]
type BacklogArgs =
    | [<CliPrefix(CliPrefix.None)>] Take of ParseResults<TakeArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Take _ -> "take a backlog item and create task files"

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"
            | Backlog _ -> "backlog commands"

// ---------------------------------------------------------------------------
// Composition root
// ---------------------------------------------------------------------------

/// Composition root - combines all adapters into a single deps object
type AppDeps() =
    let envAdapter = EnvironmentAdapter()
    let fsAdapter = FileSystemAdapter()
    let portfolioConfigAdapter = PortfolioAdapter.PortfolioConfigAdapter(envAdapter)
    let productConfigAdapter = YamlAdapter.ProductConfigAdapter()
    let backlogStoreAdapter = YamlAdapter.BacklogStoreAdapter()
    let taskStoreAdapter = YamlAdapter.TaskStoreAdapter()

    interface IEnvironment with
        member _.GetEnvVar name =
            (envAdapter :> IEnvironment).GetEnvVar name

        member _.HomeDirectory() =
            (envAdapter :> IEnvironment).HomeDirectory()

    interface IFileSystem with
        member _.ReadFile path =
            (fsAdapter :> IFileSystem).ReadFile path

        member _.WriteFile path content =
            (fsAdapter :> IFileSystem).WriteFile path content

        member _.FileExists path =
            (fsAdapter :> IFileSystem).FileExists path

        member _.DirectoryExists path =
            (fsAdapter :> IFileSystem).DirectoryExists path

    interface IPortfolioConfig with
        member _.ConfigPath() =
            (portfolioConfigAdapter :> IPortfolioConfig).ConfigPath()

        member _.LoadConfig path =
            (portfolioConfigAdapter :> IPortfolioConfig).LoadConfig path

    interface IProductConfig with
        member _.LoadProductConfig coordRoot =
            (productConfigAdapter :> IProductConfig).LoadProductConfig coordRoot

    interface IBacklogStore with
        member _.LoadBacklogItem coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).LoadBacklogItem coordRoot backlogId

    interface ITaskStore with
        member _.ListTasks coordRoot backlogId =
            (taskStoreAdapter :> ITaskStore).ListTasks coordRoot backlogId

        member _.WriteTask coordRoot task =
            (taskStoreAdapter :> ITaskStore).WriteTask coordRoot task

// ---------------------------------------------------------------------------
// Error formatting
// ---------------------------------------------------------------------------

let private formatTakeError (err: TakeError) : string =
    match err with
    | ProductConfigNotFound root -> $"product.yaml not found at: {root}"
    | ProductConfigParseError(path, msg) -> $"Failed to parse product config at {path}: {msg}"
    | BacklogItemNotFound id -> $"Backlog item not found: {BacklogId.value id}"
    | RepoNotInProduct id -> $"Repo '{RepoId.value id}' is not listed in product.yaml"
    | TaskIdConflict id -> $"Task id '{TaskId.value id}' already exists"
    | TaskIdOverrideRequiresSingleRepo -> "--task-id can only be used with single-repo backlog items"

// ---------------------------------------------------------------------------
// backlog take handler
// ---------------------------------------------------------------------------

let private handleBacklogTake
    (deps: AppDeps)
    (coordRoot: string)
    (takeArgs: ParseResults<TakeArgs>)
    (outputJson: bool)
    : Result<unit, string> =
    let rawBacklogId = takeArgs.GetResult Backlog_Id
    let taskIdOverride = takeArgs.TryGetResult Task_Id

    let backlogIdResult = BacklogId.tryCreate rawBacklogId

    match backlogIdResult with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->

    let productStore = deps :> IProductConfig
    let backlogStore = deps :> IBacklogStore
    let taskStore = deps :> ITaskStore

    let result =
        productStore.LoadProductConfig coordRoot
        |> Result.mapError formatTakeError
        |> Result.bind (fun productConfig ->
            backlogStore.LoadBacklogItem coordRoot backlogId
            |> Result.mapError formatTakeError
            |> Result.bind (fun backlogItem ->
                taskStore.ListTasks coordRoot backlogId
                |> Result.mapError formatTakeError
                |> Result.bind (fun existingTasks ->
                    let input =
                        { Task.TakeInput.BacklogId = backlogId
                          Task.TakeInput.TaskIdOverride = taskIdOverride }

                    let today = DateOnly.FromDateTime(DateTime.UtcNow)

                    Task.takeBacklogItem productConfig backlogItem existingTasks input today
                    |> Result.mapError formatTakeError
                    |> Result.bind (fun newTasks ->
                        // Re-read existing ids before writing to guard against TOCTOU
                        let freshExistingResult = taskStore.ListTasks coordRoot backlogId

                        let writeResults =
                            newTasks
                            |> List.map (fun task ->
                                taskStore.WriteTask coordRoot task
                                |> Result.map (fun () ->
                                    let taskId = TaskId.value task.Id
                                    let path = System.IO.Path.Combine(coordRoot, "TASKS", BacklogId.value backlogId, $"{taskId}-task.yaml")
                                    (taskId, path))
                                |> Result.mapError formatTakeError)

                        let errors =
                            writeResults |> List.choose (function | Error e -> Some e | Ok _ -> None)

                        match errors with
                        | e :: _ -> Error e
                        | [] ->
                            let written = writeResults |> List.choose (function | Ok v -> Some v | Error _ -> None)
                            Ok written))))

    match result with
    | Error msg -> Error msg
    | Ok written ->
        if outputJson then
            let items =
                written
                |> List.map (fun (id, path) -> $"""  {{ "id": "{id}", "path": "{path}" }}""")
                |> String.concat ",\n"

            printfn """{ "ok": true, "tasks": ["""
            printfn "%s" items
            printfn "] }"
        else
            written |> List.iter (fun (id, path) -> printfn "Created task: %s → %s" id path)

        Ok()

// ---------------------------------------------------------------------------
// Dispatch
// ---------------------------------------------------------------------------

let private dispatch (deps: AppDeps) (results: ParseResults<CliArgs>) : Result<unit, string> =
    let profile = results.TryGetResult Profile
    let outputJson = results.TryGetResult Output |> Option.exists (fun v -> v = "json")

    match results.TryGetResult Backlog with
    | Some backlogResults ->
        match backlogResults.TryGetResult Take with
        | Some takeArgs ->
            // Resolve the product to get the coordination root
            let portfolioResult =
                Portfolio.loadPortfolio None |> Effect.run deps
                |> Result.mapError (fun e -> $"%A{e}")
                |> Result.bind (fun portfolio ->
                    Portfolio.resolveActiveProfile portfolio profile |> Effect.run deps
                    |> Result.mapError (fun e -> $"%A{e}"))

            match portfolioResult with
            | Error msg -> Error msg
            | Ok profile ->
                // For now take the first product from the profile
                match profile.Products with
                | [] -> Error "No products in active profile"
                | productRef :: _ ->
                    let portfolioProductResult =
                        Portfolio.resolveProduct profile (productRef.Id |> ProductId.value) |> Effect.run deps
                        |> Result.mapError (fun e -> $"%A{e}")

                    match portfolioProductResult with
                    | Error msg -> Error msg
                    | Ok resolved ->
                        handleBacklogTake deps resolved.CoordRoot.AbsolutePath takeArgs outputJson

        | None -> Error "Specify a backlog subcommand (e.g. backlog take <id>)"

    | None ->
        // Legacy behaviour: just load portfolio
        let loadResult = Portfolio.loadPortfolio None |> Effect.run deps

        loadResult
        |> Result.bind (fun portfolio -> Portfolio.resolveActiveProfile portfolio profile |> Effect.run deps)
        |> Result.map (fun _ -> printfn "itr cli")
        |> Result.mapError (fun e -> $"%A{e}")

[<EntryPoint>]
let main argv =
    let deps = AppDeps()
    let parser = ArgumentParser.Create<CliArgs>(programName = "itr")

    try
        let results = parser.Parse argv

        match dispatch deps results with
        | Ok() -> 0
        | Error msg ->
            eprintfn "Error: %s" msg
            1
    with :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1
