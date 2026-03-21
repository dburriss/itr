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

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfilesAddArgs =
    | [<MainCommand; Mandatory>] Name of name: string
    | Git_Name of git_name: string
    | Git_Email of git_email: string
    | Set_Default

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Name _ -> "profile name (slug: [a-z0-9][a-z0-9-]*)"
            | Git_Name _ -> "git user name for this profile"
            | Git_Email _ -> "git user email for this profile"
            | Set_Default -> "set this profile as the default"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfilesArgs =
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<ProfilesAddArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Add _ -> "add a new profile to the portfolio"

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>
    | [<CliPrefix(CliPrefix.None)>] Profiles of ParseResults<ProfilesArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"
            | Backlog _ -> "backlog commands"
            | Profiles _ -> "profile management commands"

// ---------------------------------------------------------------------------
// Composition root
// ---------------------------------------------------------------------------

/// Composition root - combines all adapters into a single deps object
type AppDeps() =
    let envAdapter = EnvironmentAdapter()
    let fsAdapter = FileSystemAdapter()
    let portfolioConfigAdapter = PortfolioAdapter.PortfolioConfigAdapter(envAdapter, fsAdapter)
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

        member _.SaveConfig path portfolio =
            (portfolioConfigAdapter :> IPortfolioConfig).SaveConfig path portfolio

    interface IProductConfig with
        member _.LoadProductConfig productRoot =
            (productConfigAdapter :> IProductConfig).LoadProductConfig productRoot

    interface IBacklogStore with
        member _.LoadBacklogItem coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).LoadBacklogItem coordRoot backlogId

        member _.ArchiveBacklogItem coordRoot backlogId date =
            (backlogStoreAdapter :> IBacklogStore).ArchiveBacklogItem coordRoot backlogId date

    interface ITaskStore with
        member _.ListTasks coordRoot backlogId =
            (taskStoreAdapter :> ITaskStore).ListTasks coordRoot backlogId

        member _.WriteTask coordRoot task =
            (taskStoreAdapter :> ITaskStore).WriteTask coordRoot task

        member _.ArchiveTask coordRoot backlogId taskId date =
            (taskStoreAdapter :> ITaskStore).ArchiveTask coordRoot backlogId taskId date

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

let private formatPortfolioError (err: PortfolioError) : string =
    match err with
    | BootstrapWriteError(path, msg) -> $"Could not create itr.json at {path}: {msg}"
    | DuplicateProfileName name -> $"Profile '{name}' already exists."
    | InvalidProfileName(value, rules) -> $"Invalid profile name '{value}': {rules}"
    | other -> $"%A{other}"

// ---------------------------------------------------------------------------
// Project ProductDefinition to ProductConfig for task use case
// ---------------------------------------------------------------------------

let private toProductConfig (def: ProductDefinition) : ProductConfig =
    let repos =
        def.Repos |> Map.toSeq |> Seq.map (fun (k, v) -> RepoId k, v) |> Map.ofSeq

    { Id = def.Id; Repos = repos }

// ---------------------------------------------------------------------------
// backlog take handler
// ---------------------------------------------------------------------------

let private handleBacklogTake
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (takeArgs: ParseResults<TakeArgs>)
    (outputJson: bool)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawBacklogId = takeArgs.GetResult Backlog_Id
    let taskIdOverride = takeArgs.TryGetResult Task_Id

    let backlogIdResult = BacklogId.tryCreate rawBacklogId

    match backlogIdResult with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->

        let productConfig = toProductConfig resolved.Definition
        let backlogStore = deps :> IBacklogStore
        let taskStore = deps :> ITaskStore

        let result =
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
                        let writeResults =
                            newTasks
                            |> List.map (fun task ->
                                taskStore.WriteTask coordRoot task
                                |> Result.map (fun () ->
                                    let taskId = TaskId.value task.Id

                                    let path =
                                        System.IO.Path.Combine(
                                            coordRoot,
                                            "BACKLOG",
                                            BacklogId.value backlogId,
                                            "tasks",
                                            taskId,
                                            "task.yaml"
                                        )

                                    (taskId, path))
                                |> Result.mapError formatTakeError)

                        let errors =
                            writeResults
                            |> List.choose (function
                                | Error e -> Some e
                                | Ok _ -> None)

                        match errors with
                        | e :: _ -> Error e
                        | [] ->
                            let written =
                                writeResults
                                |> List.choose (function
                                    | Ok v -> Some v
                                    | Error _ -> None)

                            Ok written)))

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

    // Resolve config path once and run bootstrap before any portfolio operation
    let configPath = (deps :> IPortfolioConfig).ConfigPath()

    let bootstrapResult =
        Portfolio.bootstrapIfMissing configPath
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match bootstrapResult with
    | Error msg -> Error msg
    | Ok wasCreated ->
        if wasCreated then
            printfn "Created default config at %s. Run 'itr init' to configure profiles and products." configPath

        match results.TryGetResult Backlog with
        | Some backlogResults ->
            match backlogResults.TryGetResult Take with
            | Some takeArgs ->
                // Resolve the product to get the coordination root
                let portfolioResult =
                    Portfolio.loadPortfolio (Some configPath)
                    |> Effect.run deps
                    |> Result.mapError (fun e -> $"%A{e}")
                    |> Result.bind (fun portfolio ->
                        Portfolio.resolveActiveProfile portfolio profile
                        |> Effect.run deps
                        |> Result.mapError (fun e -> $"%A{e}"))

                match portfolioResult with
                | Error msg -> Error msg
                | Ok activeProfile ->
                    // For now take the first product from the profile
                    match activeProfile.Products with
                    | [] -> Error "No products in active profile"
                    | productRef :: _ ->
                        let (ProductRoot root) = productRef.Root

                        // Load the product definition to get the canonical id
                        let productConfig = deps :> IProductConfig

                        match productConfig.LoadProductConfig root with
                        | Error e -> Error(formatPortfolioError e)
                        | Ok definition ->
                            let portfolioProductResult =
                                Portfolio.resolveProduct activeProfile (ProductId.value definition.Id)
                                |> Effect.run deps
                                |> Result.mapError (fun e -> $"%A{e}")

                            match portfolioProductResult with
                            | Error msg -> Error msg
                            | Ok resolved -> handleBacklogTake deps resolved takeArgs outputJson

            | None -> Error "Specify a backlog subcommand (e.g. backlog take <id>)"

        | None ->
            match results.TryGetResult Profiles with
            | Some profilesResults ->
                match profilesResults.TryGetResult Add with
                | Some addArgs ->
                    let name = addArgs.GetResult ProfilesAddArgs.Name
                    let gitName = addArgs.TryGetResult ProfilesAddArgs.Git_Name
                    let gitEmail = addArgs.TryGetResult ProfilesAddArgs.Git_Email
                    let setDefault = addArgs.Contains ProfilesAddArgs.Set_Default

                    // Validate: --git-email requires --git-name
                    match gitEmail, gitName with
                    | Some _, None ->
                        Error "--git-email requires --git-name to also be specified"
                    | _ ->
                        let gitIdentity =
                            gitName
                            |> Option.map (fun gn -> { Name = gn; Email = gitEmail })

                        let input: Portfolio.AddProfileInput =
                            { Name = name
                              GitIdentity = gitIdentity
                              SetAsDefault = setDefault }

                        let result =
                            Portfolio.addProfile configPath input
                            |> Effect.run deps
                            |> Result.mapError formatPortfolioError

                        match result with
                        | Error msg -> Error msg
                        | Ok updatedPortfolio ->
                            let portfolioConfig = deps :> IPortfolioConfig

                            portfolioConfig.SaveConfig configPath updatedPortfolio
                            |> Result.mapError formatPortfolioError
                            |> Result.map (fun () -> printfn "Added profile '%s'." name)

                | None -> Error "Specify a profiles subcommand (e.g. profiles add <name>)"

            | None ->
                // Legacy behaviour: just load portfolio
                let loadResult = Portfolio.loadPortfolio (Some configPath) |> Effect.run deps

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
