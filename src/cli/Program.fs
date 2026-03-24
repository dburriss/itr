module Itr.Cli.Program

open System
open Argu
open Spectre.Console
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
type AddArgs =
    | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
    | [<Mandatory>] Title of title: string
    | Repo of repo: string
    | Item_Type of item_type: string
    | Summary of summary: string
    | Priority of priority: string
    | Depends_On of depends_on: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "backlog item id (slug: [a-z0-9][a-z0-9-]*)"
            | Title _ -> "short title for the backlog item"
            | Repo _ -> "repo id to assign item to (required if product has multiple repos)"
            | Item_Type _ -> "item type: feature | bug | chore | spike (default: feature)"
            | Summary _ -> "longer description of the item"
            | Priority _ -> "priority label (e.g. high, medium, low)"
            | Depends_On _ -> "backlog item id this item depends on (can be repeated)"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ListArgs =
    | View of view: string
    | Status of status: string
    | Type of type_: string
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | View _ -> "filter by view id"
            | Status _ -> "filter by status: created | planning | planned | approved | in-progress | completed | archived"
            | Type _ -> "filter by item type: feature | bug | chore | spike"
            | Output _ -> "output mode: table (default) | json"

[<CliPrefix(CliPrefix.DoubleDash)>]
type InfoArgs =
    | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "backlog item id to inspect"
            | Output _ -> "output mode: table (default) | json"

[<CliPrefix(CliPrefix.DoubleDash)>]
type BacklogArgs =
    | [<CliPrefix(CliPrefix.None)>] Take of ParseResults<TakeArgs>
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AddArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<InfoArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Take _ -> "take a backlog item and create task files"
            | Add _ -> "create a new backlog item"
            | List _ -> "list backlog items"
            | Info _ -> "show detailed information about a backlog item"

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

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductInitArgs =
    | [<MainCommand; Mandatory>] Path of path: string
    | Id of id: string
    | Repo_Id of repo_id: string
    | Coord_Path of coord_path: string
    | Coord_Mode of coord_mode: string
    | Register_Profile of register_profile: string
    | No_Register

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "target directory for the new product"
            | Id _ -> "product id (slug: [a-z0-9][a-z0-9-]*)"
            | Repo_Id _ -> "repo id (defaults to product id)"
            | Coord_Path _ -> "coordination directory path (default: .itr)"
            | Coord_Mode _ -> "coordination mode: primary-repo or standalone (default: primary-repo)"
            | Register_Profile _ -> "register the new product in this portfolio profile"
            | No_Register -> "skip registration in itr.json"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductRegisterArgs =
    | [<MainCommand; Mandatory>] Path of path: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "path to an existing product root directory"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductArgs =
    | [<CliPrefix(CliPrefix.None)>] Init of ParseResults<ProductInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<ProductRegisterArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init _ -> "scaffold a new product"
            | Register _ -> "register an existing product root in the portfolio"

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>
    | [<CliPrefix(CliPrefix.None)>] Profiles of ParseResults<ProfilesArgs>
    | [<CliPrefix(CliPrefix.None)>] Product of ParseResults<ProductArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"
            | Backlog _ -> "backlog commands"
            | Profiles _ -> "profile management commands"
            | Product _ -> "product management commands"

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
    let viewStoreAdapter = YamlAdapter.ViewStoreAdapter()

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

        member _.LoadArchivedBacklogItem coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).LoadArchivedBacklogItem coordRoot backlogId

        member _.ArchiveBacklogItem coordRoot backlogId date =
            (backlogStoreAdapter :> IBacklogStore).ArchiveBacklogItem coordRoot backlogId date

        member _.BacklogItemExists coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).BacklogItemExists coordRoot backlogId

        member _.WriteBacklogItem coordRoot item =
            (backlogStoreAdapter :> IBacklogStore).WriteBacklogItem coordRoot item

        member _.ListBacklogItems coordRoot =
            (backlogStoreAdapter :> IBacklogStore).ListBacklogItems coordRoot

        member _.ListArchivedBacklogItems coordRoot =
            (backlogStoreAdapter :> IBacklogStore).ListArchivedBacklogItems coordRoot

    interface IViewStore with
        member _.ListViews coordRoot =
            (viewStoreAdapter :> IViewStore).ListViews coordRoot

    interface ITaskStore with
        member _.ListTasks coordRoot backlogId =
            (taskStoreAdapter :> ITaskStore).ListTasks coordRoot backlogId

        member _.ListArchivedTasks coordRoot backlogId =
            (taskStoreAdapter :> ITaskStore).ListArchivedTasks coordRoot backlogId

        member _.WriteTask coordRoot task =
            (taskStoreAdapter :> ITaskStore).WriteTask coordRoot task

        member _.ArchiveTask coordRoot backlogId taskId date =
            (taskStoreAdapter :> ITaskStore).ArchiveTask coordRoot backlogId taskId date

// ---------------------------------------------------------------------------
// Error formatting
// ---------------------------------------------------------------------------

let private formatBacklogError (err: BacklogError) : string =
    match err with
    | ProductConfigNotFound root -> $"product.yaml not found at: {root}"
    | ProductConfigParseError(path, msg) -> $"Failed to parse product config at {path}: {msg}"
    | BacklogItemNotFound id -> $"Backlog item not found: {BacklogId.value id}"
    | RepoNotInProduct id ->
        if RepoId.value id = "" then
            "--repo is required when the product has multiple repos"
        else
            $"Repo '{RepoId.value id}' is not listed in product.yaml"
    | TaskIdConflict id -> $"Task id '{TaskId.value id}' already exists"
    | TaskIdOverrideRequiresSingleRepo -> "--task-id can only be used with single-repo backlog items"
    | DuplicateBacklogId id -> $"Backlog item '{BacklogId.value id}' already exists"
    | InvalidItemType value -> $"Invalid item type '{value}': must be feature | bug | chore | spike"
    | MissingTitle -> "--title is required"

let private formatPortfolioError (err: PortfolioError) : string =
    match err with
    | BootstrapWriteError(path, msg) -> $"Could not create itr.json at {path}: {msg}"
    | DuplicateProfileName name -> $"Profile '{name}' already exists."
    | InvalidProfileName(value, rules) -> $"Invalid profile name '{value}': {rules}"
    | InvalidProductId(value, rules) -> $"Invalid product id '{value}': {rules}"
    | ProductConfigError(root, msg) -> $"Product config error at '{root}': {msg}"
    | ProductNotFound id -> $"Product '{id}' not found."
    | CoordRootNotFound(id, path) -> $"Coordination root for '{id}' not found at: {path}"
    | DuplicateProductId(profile, id) -> $"Product '{id}' is already registered in profile '{profile}'."
    | other -> $"%A{other}"

// ---------------------------------------------------------------------------
// Project ProductDefinition to ProductConfig for task use case
// ---------------------------------------------------------------------------

let private toProductConfig (def: ProductDefinition) : ProductConfig =
    let repos =
        def.Repos |> Map.toSeq |> Seq.map (fun (k, v) -> RepoId k, v) |> Map.ofSeq

    { Id = def.Id; Repos = repos }

let private backlogItemStatusToString (status: BacklogItemStatus) : string =
    match status with
    | BacklogItemStatus.Created -> "created"
    | BacklogItemStatus.Planning -> "planning"
    | BacklogItemStatus.Planned -> "planned"
    | BacklogItemStatus.Approved -> "approved"
    | BacklogItemStatus.InProgress -> "in-progress"
    | BacklogItemStatus.Completed -> "completed"
    | BacklogItemStatus.Archived -> "archived"

let private tryParseBacklogItemStatus (s: string) : BacklogItemStatus option =
    match s with
    | "created" -> Some BacklogItemStatus.Created
    | "planning" -> Some BacklogItemStatus.Planning
    | "planned" -> Some BacklogItemStatus.Planned
    | "approved" -> Some BacklogItemStatus.Approved
    | "in-progress" | "inprogress" -> Some BacklogItemStatus.InProgress
    | "completed" -> Some BacklogItemStatus.Completed
    | "archived" -> Some BacklogItemStatus.Archived
    | _ -> None

// ---------------------------------------------------------------------------
// backlog list handler
// ---------------------------------------------------------------------------

let private handleBacklogList
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (listArgs: ParseResults<ListArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let backlogStore = deps :> IBacklogStore
    let taskStore = deps :> ITaskStore
    let viewStore = deps :> IViewStore

    let viewFilter = listArgs.TryGetResult ListArgs.View
    let outputJson = listArgs.TryGetResult ListArgs.Output |> Option.exists (fun v -> v = "json")

    let statusFilter =
        listArgs.TryGetResult ListArgs.Status
        |> Option.bind tryParseBacklogItemStatus

    let typeFilter =
        listArgs.TryGetResult ListArgs.Type
        |> Option.bind (fun t ->
            match BacklogItemType.tryParse t with
            | Ok bt -> Some bt
            | Error _ -> None)

    let filter: Backlog.BacklogListFilter =
        { ViewId = viewFilter
          Status = statusFilter
          ItemType = typeFilter }

    match Backlog.loadSnapshot backlogStore taskStore viewStore coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok snapshot ->
        let items = Backlog.listBacklogItems filter snapshot

        if outputJson then
            let jsonItems =
                items
                |> List.map (fun s ->
                    let id = BacklogId.value s.Item.Id
                    let itemType = BacklogItemType.toString s.Item.Type
                    let priority = s.Item.Priority |> Option.defaultValue ""
                    let status = backlogItemStatusToString s.Status
                    let viewId = s.ViewId |> Option.defaultValue ""
                    let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")
                    sprintf """  { "id": "%s", "type": "%s", "priority": "%s", "status": "%s", "view": "%s", "taskCount": %d, "createdAt": "%s" }"""
                        id itemType priority status viewId s.TaskCount createdAt)
                |> String.concat ",\n"
            printfn "["
            if not (List.isEmpty items) then
                printfn "%s" jsonItems
            printfn "]"
        else
            let table = Table()
            table.AddColumn("ID") |> ignore
            table.AddColumn("Type") |> ignore
            table.AddColumn("Priority") |> ignore
            table.AddColumn("Status") |> ignore
            table.AddColumn("View") |> ignore
            table.AddColumn("Tasks") |> ignore
            table.AddColumn("Created") |> ignore

            items
            |> List.iter (fun s ->
                let id = BacklogId.value s.Item.Id
                let itemType = BacklogItemType.toString s.Item.Type
                let priority = s.Item.Priority |> Option.defaultValue "-"
                let status = backlogItemStatusToString s.Status
                let viewId = s.ViewId |> Option.defaultValue "-"
                let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")
                table.AddRow(id, itemType, priority, status, viewId, string s.TaskCount, createdAt) |> ignore)

            AnsiConsole.Write(table)

        Ok()

// ---------------------------------------------------------------------------
// backlog info handler
// ---------------------------------------------------------------------------

let private handleBacklogInfo
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (infoArgs: ParseResults<InfoArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawBacklogId = infoArgs.GetResult InfoArgs.Backlog_Id
    let outputJson = infoArgs.TryGetResult InfoArgs.Output |> Option.exists (fun v -> v = "json")

    match BacklogId.tryCreate rawBacklogId with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->
        let backlogStore = deps :> IBacklogStore
        let taskStore = deps :> ITaskStore
        let viewStore = deps :> IViewStore

        match Backlog.getBacklogItemDetail backlogStore taskStore viewStore coordRoot backlogId with
        | Error e -> Error(formatBacklogError e)
        | Ok detail ->
            let item = detail.Item
            let id = BacklogId.value item.Id
            let itemType = BacklogItemType.toString item.Type
            let priority = item.Priority |> Option.defaultValue ""
            let status = backlogItemStatusToString detail.Status
            let viewId = detail.ViewId |> Option.defaultValue ""
            let createdAt = item.CreatedAt.ToString("yyyy-MM-dd")
            let summary = item.Summary |> Option.defaultValue ""
            let ac = item.AcceptanceCriteria
            let deps_ = item.Dependencies |> List.map BacklogId.value
            let repos = item.Repos |> List.map RepoId.value

            if outputJson then
                let acJson =
                    ac
                    |> List.map (fun s -> sprintf "    \"%s\"" (s.Replace("\"", "\\\"")))
                    |> String.concat ",\n"
                let depsJson =
                    deps_
                    |> List.map (fun s -> sprintf "    \"%s\"" s)
                    |> String.concat ",\n"
                let reposJson =
                    repos
                    |> List.map (fun s -> sprintf "    \"%s\"" s)
                    |> String.concat ",\n"
                let tasksJson =
                    detail.Tasks
                    |> List.map (fun t ->
                        let tid = TaskId.value t.Id
                        let repo = RepoId.value t.Repo
                        let state =
                            match t.State with
                            | TaskState.Planning -> "planning"
                            | TaskState.Planned -> "planned"
                            | TaskState.Approved -> "approved"
                            | TaskState.InProgress -> "in-progress"
                            | TaskState.Implemented -> "implemented"
                            | TaskState.Validated -> "validated"
                        sprintf "    { \"id\": \"%s\", \"repo\": \"%s\", \"state\": \"%s\" }" tid repo state)
                    |> String.concat ",\n"

                printfn "{"
                printfn "  \"id\": \"%s\"," id
                printfn "  \"title\": \"%s\"," (item.Title.Replace("\"", "\\\""))
                printfn "  \"type\": \"%s\"," itemType
                printfn "  \"priority\": \"%s\"," priority
                printfn "  \"status\": \"%s\"," status
                printfn "  \"summary\": \"%s\"," (summary.Replace("\"", "\\\""))
                printfn "  \"acceptanceCriteria\": ["
                if not (List.isEmpty ac) then printfn "%s" acJson
                printfn "  ],"
                printfn "  \"dependencies\": ["
                if not (List.isEmpty deps_) then printfn "%s" depsJson
                printfn "  ],"
                printfn "  \"repos\": ["
                if not (List.isEmpty repos) then printfn "%s" reposJson
                printfn "  ],"
                printfn "  \"createdAt\": \"%s\"," createdAt
                printfn "  \"tasks\": ["
                if not (List.isEmpty detail.Tasks) then printfn "%s" tasksJson
                printfn "  ]"
                printfn "}"
            else
                // Detail table
                let infoTable = Table()
                infoTable.AddColumn("Field") |> ignore
                infoTable.AddColumn("Value") |> ignore
                infoTable.AddRow("id", id) |> ignore
                infoTable.AddRow("title", item.Title) |> ignore
                infoTable.AddRow("type", itemType) |> ignore
                infoTable.AddRow("priority", priority) |> ignore
                infoTable.AddRow("status", status) |> ignore
                infoTable.AddRow("view", viewId) |> ignore
                infoTable.AddRow("summary", summary) |> ignore
                infoTable.AddRow("acceptance criteria", String.concat "\n" ac) |> ignore
                infoTable.AddRow("dependencies", String.concat ", " deps_) |> ignore
                infoTable.AddRow("repos", String.concat ", " repos) |> ignore
                infoTable.AddRow("created", createdAt) |> ignore
                AnsiConsole.Write(infoTable)

                // Tasks table
                let tasksTable = Table()
                tasksTable.AddColumn("Task ID") |> ignore
                tasksTable.AddColumn("Repo") |> ignore
                tasksTable.AddColumn("State") |> ignore

                detail.Tasks
                |> List.iter (fun t ->
                    let tid = TaskId.value t.Id
                    let repo = RepoId.value t.Repo
                    let state =
                        match t.State with
                        | TaskState.Planning -> "planning"
                        | TaskState.Planned -> "planned"
                        | TaskState.Approved -> "approved"
                        | TaskState.InProgress -> "in-progress"
                        | TaskState.Implemented -> "implemented"
                        | TaskState.Validated -> "validated"
                    tasksTable.AddRow(tid, repo, state) |> ignore)

                AnsiConsole.Write(tasksTable)

            Ok()

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
    let rawBacklogId = takeArgs.GetResult TakeArgs.Backlog_Id
    let taskIdOverride = takeArgs.TryGetResult TakeArgs.Task_Id

    let backlogIdResult = BacklogId.tryCreate rawBacklogId

    match backlogIdResult with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->

        let productConfig = toProductConfig resolved.Definition
        let backlogStore = deps :> IBacklogStore
        let taskStore = deps :> ITaskStore

        let result =
            backlogStore.LoadBacklogItem coordRoot backlogId
            |> Result.mapError formatBacklogError
            |> Result.bind (fun backlogItem ->
                taskStore.ListTasks coordRoot backlogId
                |> Result.mapError formatBacklogError
                |> Result.bind (fun existingTasks ->
                    let input =
                        { Task.TakeInput.BacklogId = backlogId
                          Task.TakeInput.TaskIdOverride = taskIdOverride }

                    let today = DateOnly.FromDateTime(DateTime.UtcNow)

                    Task.takeBacklogItem productConfig backlogItem existingTasks input today
                    |> Result.mapError formatBacklogError
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
                                |> Result.mapError formatBacklogError)

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
// backlog add handler
// ---------------------------------------------------------------------------

let private handleBacklogAdd
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (addArgs: ParseResults<AddArgs>)
    (outputJson: bool)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawBacklogId = addArgs.GetResult AddArgs.Backlog_Id
    let title = addArgs.GetResult AddArgs.Title
    let repo = addArgs.TryGetResult AddArgs.Repo
    let itemType = addArgs.TryGetResult AddArgs.Item_Type
    let summary = addArgs.TryGetResult AddArgs.Summary
    let priority = addArgs.TryGetResult AddArgs.Priority
    let dependsOn = addArgs.GetResults AddArgs.Depends_On

    let productConfig = toProductConfig resolved.Definition
    let backlogStore = deps :> IBacklogStore

    // Check for duplicate
    match BacklogId.tryCreate rawBacklogId with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->
        if backlogStore.BacklogItemExists coordRoot backlogId then
            Error(formatBacklogError (DuplicateBacklogId backlogId))
        else
            let input: Backlog.CreateBacklogItemInput =
                { BacklogId = rawBacklogId
                  Title = title
                  Repos = repo |> Option.toList
                  ItemType = itemType
                  Priority = priority
                  Summary = summary
                  AcceptanceCriteria = []
                  DependsOn = dependsOn }

            let today = DateOnly.FromDateTime(DateTime.UtcNow)

            match Backlog.createBacklogItem productConfig input today with
            | Error e -> Error(formatBacklogError e)
            | Ok item ->
                match backlogStore.WriteBacklogItem coordRoot item with
                | Error e -> Error(formatBacklogError e)
                | Ok() ->
                    let id = BacklogId.value item.Id
                    let path = System.IO.Path.Combine(coordRoot, "BACKLOG", id, "item.yaml")

                    if outputJson then
                        printfn """{ "ok": true, "id": "%s" }""" id
                    else
                        printfn "Created backlog item '%s' → %s" id path

                    Ok()

// ---------------------------------------------------------------------------
// product register handler
// ---------------------------------------------------------------------------

let private handleProductRegister
    (deps: AppDeps)
    (configPath: string)
    (profile: string option)
    (registerArgs: ParseResults<ProductRegisterArgs>)
    (outputJson: bool)
    : Result<unit, string> =
    let path = registerArgs.GetResult ProductRegisterArgs.Path

    let input: Portfolio.RegisterProductInput =
        { Path = path
          Profile = profile }

    let result =
        Portfolio.registerProduct configPath input
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match result with
    | Error msg -> Error msg
    | Ok updatedPortfolio ->
        let portfolioConfig = deps :> IPortfolioConfig

        portfolioConfig.SaveConfig configPath updatedPortfolio
        |> Result.mapError formatPortfolioError
        |> Result.map (fun () ->
            // Load product.yaml to get the canonical id for the success message
            let productConfig = deps :> IProductConfig

            match productConfig.LoadProductConfig path with
            | Ok definition ->
                let id = ProductId.value definition.Id

                if outputJson then
                    printfn """{ "ok": true, "id": "%s", "path": "%s" }""" id path
                else
                    printfn "Registered product '%s' from '%s'." id path
            | Error _ ->
                // Fallback if product.yaml read fails after registration
                if outputJson then
                    printfn """{ "ok": true, "path": "%s" }""" path
                else
                    printfn "Registered product from '%s'." path)

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

            | None ->
                match backlogResults.TryGetResult BacklogArgs.Add with
                | Some addArgs ->
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
                        match activeProfile.Products with
                        | [] -> Error "No products in active profile"
                        | productRef :: _ ->
                            let (ProductRoot root) = productRef.Root
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
                                | Ok resolved -> handleBacklogAdd deps resolved addArgs outputJson

                | None ->
                    match backlogResults.TryGetResult BacklogArgs.List with
                    | Some listArgs ->
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
                            match activeProfile.Products with
                            | [] -> Error "No products in active profile"
                            | productRef :: _ ->
                                let (ProductRoot root) = productRef.Root
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
                                    | Ok resolved -> handleBacklogList deps resolved listArgs

                    | None ->
                        match backlogResults.TryGetResult BacklogArgs.Info with
                        | Some infoArgs ->
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
                                match activeProfile.Products with
                                | [] -> Error "No products in active profile"
                                | productRef :: _ ->
                                    let (ProductRoot root) = productRef.Root
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
                                        | Ok resolved -> handleBacklogInfo deps resolved infoArgs

                        | None -> Error "Specify a backlog subcommand (e.g. backlog take <id> or backlog add <id> or backlog list or backlog info <id>)"

        | None ->
            match results.TryGetResult Profiles with
            | Some profilesResults ->
                match profilesResults.TryGetResult ProfilesArgs.Add with
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
                match results.TryGetResult Product with
                | Some productResults ->
                    match productResults.TryGetResult Init with
                    | Some initArgs ->
                        let rawPath = initArgs.GetResult ProductInitArgs.Path

                        // 2.4 Interactive prompt for missing id
                        let rawId =
                            match initArgs.TryGetResult ProductInitArgs.Id with
                            | Some id -> id
                            | None -> AnsiConsole.Ask<string>("Product id:")

                        // 2.5 Interactive prompt for missing repo-id
                        let rawRepoId =
                            match initArgs.TryGetResult ProductInitArgs.Repo_Id with
                            | Some repoId -> repoId
                            | None ->
                                let answer = AnsiConsole.Ask<string>($"Repo id (default: same as id):")
                                if String.IsNullOrWhiteSpace(answer) then rawId else answer

                        let coordPath = initArgs.TryGetResult ProductInitArgs.Coord_Path |> Option.defaultValue ".itr"
                        let coordMode = initArgs.TryGetResult ProductInitArgs.Coord_Mode |> Option.defaultValue "primary-repo"

                        // 2.6 Interactive prompt for registration profile
                        let registerProfile =
                            if initArgs.Contains ProductInitArgs.No_Register then
                                None
                            else
                                match initArgs.TryGetResult ProductInitArgs.Register_Profile with
                                | Some p -> Some p
                                | None ->
                                    let answer = AnsiConsole.Ask<string>("Register in profile (leave blank to skip):")
                                    if String.IsNullOrWhiteSpace(answer) then None else Some answer

                        let input: Portfolio.InitProductInput =
                            { Id = rawId
                              Path = rawPath
                              RepoId = rawRepoId
                              CoordPath = coordPath
                              CoordinationMode = coordMode
                              RegisterProfile = registerProfile }

                        let result =
                            Portfolio.initProduct configPath input
                            |> Effect.run deps
                            |> Result.mapError formatPortfolioError

                        match result with
                        | Error msg -> Error msg
                        | Ok maybePortfolio ->
                            // 2.7 Success message
                            printfn "Initialized product '%s' at %s." rawId rawPath

                            // 2.8 Registration success message
                            match maybePortfolio, registerProfile with
                            | Some _, Some prof -> printfn "Registered in profile '%s'." prof
                            | _ -> ()

                            Ok()

                    | None ->
                        match productResults.TryGetResult Register with
                        | Some registerArgs ->
                            handleProductRegister deps configPath profile registerArgs outputJson
                        | None -> Error "Specify a product subcommand (e.g. product init <path> or product register <path>)"

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
