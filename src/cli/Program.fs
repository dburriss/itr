module Itr.Cli.Program

open System
open Argu
open Fue.Data
open Fue.Compiler
open Spectre.Console
open Itr.Domain
open Itr.Features
open Itr.Adapters

// ---------------------------------------------------------------------------
// Output format
// ---------------------------------------------------------------------------

type OutputFormat = TableOutput | JsonOutput | TextOutput

let private parseOutputFormat (value: string option) : OutputFormat =
    match value with
    | Some "json" -> JsonOutput
    | Some "text" -> TextOutput
    | _ -> TableOutput

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
    | [<MainCommand>] Backlog_Id of backlog_id: string
    | Title of title: string
    | Repo of repo: string
    | Item_Type of item_type: string
    | Summary of summary: string
    | Priority of priority: string
    | Depends_On of depends_on: string
    | [<AltCommandLine("-i")>] Interactive

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
            | Interactive -> "prompt for each field interactively"

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
             | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type InfoArgs =
    | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
             | Backlog_Id _ -> "backlog item id to inspect"
             | Output _ -> "output mode: table (default) | json | text"

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

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskListArgs =
    | [<AltCommandLine("--backlog")>] Backlog_Id of backlog_id: string
    | [<AltCommandLine("--repo")>] Repo_Id of repo_id: string
    | State of state: string
    | [<AltCommandLine("-o")>] Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "filter by backlog item id"
            | Repo_Id _ -> "filter by repo id"
             | State _ -> "filter by task state (planning | planned | approved | in_progress | implemented | validated | archived)"
             | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskInfoArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string
    | [<AltCommandLine("-o")>] Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
             | Task_Id _ -> "task id to inspect"
             | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskPlanArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string
    | Ai
    | Debug

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Task_Id _ -> "task id to plan"
            | Ai -> "use OpenCode AI to generate plan content"
            | Debug -> "print raw HTTP responses to stderr during AI interaction"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskApproveArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Task_Id _ -> "task id to approve"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskArgs =
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<TaskListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<TaskInfoArgs>
    | [<CliPrefix(CliPrefix.None)>] Plan of ParseResults<TaskPlanArgs>
    | [<CliPrefix(CliPrefix.None)>] Approve of ParseResults<TaskApproveArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | List _ -> "list all tasks across a product"
            | Info _ -> "show detailed information about a task"
            | Plan _ -> "generate a plan for a task"
            | Approve _ -> "approve a task plan"

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>
    | [<CliPrefix(CliPrefix.None)>] Profiles of ParseResults<ProfilesArgs>
    | [<CliPrefix(CliPrefix.None)>] Product of ParseResults<ProductArgs>
    | [<CliPrefix(CliPrefix.None)>] Task of ParseResults<TaskArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"
            | Backlog _ -> "backlog commands"
            | Profiles _ -> "profile management commands"
            | Product _ -> "product management commands"
            | Task _ -> "task commands"

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
    let agentHarnessAdapter = OpenCodeHarnessAdapter()

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

        member _.ListAllTasks coordRoot =
            (taskStoreAdapter :> ITaskStore).ListAllTasks coordRoot

    interface IAgentHarness with
        member _.Prompt prompt debug =
            (agentHarnessAdapter :> IAgentHarness).Prompt prompt debug

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
    | TaskNotFound id -> $"Task not found: {TaskId.value id}"
    | InvalidTaskState(id, current) ->
        let stateStr =
            match current with
            | TaskState.Planning -> "planning"
            | TaskState.Planned -> "planned"
            | TaskState.Approved -> "approved"
            | TaskState.InProgress -> "in_progress"
            | TaskState.Implemented -> "implemented"
            | TaskState.Validated -> "validated"
            | TaskState.Archived -> "archived"
        $"Invalid state transition for task '{TaskId.value id}': current state is '{stateStr}'"
    | MissingPlanArtifact id -> $"Cannot approve task '{TaskId.value id}': plan artifact does not exist"

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

let private tryParseTaskState (s: string) : Result<TaskState, string> =
    match s with
    | "planning" -> Ok TaskState.Planning
    | "planned" -> Ok TaskState.Planned
    | "approved" -> Ok TaskState.Approved
    | "in_progress" | "in-progress" -> Ok TaskState.InProgress
    | "implemented" -> Ok TaskState.Implemented
    | "validated" -> Ok TaskState.Validated
    | "archived" -> Ok TaskState.Archived
    | other -> Error $"Unknown task state '{other}': must be planning | planned | approved | in_progress | implemented | validated | archived"

let private taskStateToDisplayString (state: TaskState) : string =
    match state with
    | TaskState.Planning -> "planning"
    | TaskState.Planned -> "planned"
    | TaskState.Approved -> "approved"
    | TaskState.InProgress -> "in_progress"
    | TaskState.Implemented -> "implemented"
    | TaskState.Validated -> "validated"
    | TaskState.Archived -> "archived"

// ---------------------------------------------------------------------------
// task list handler
// ---------------------------------------------------------------------------

let private handleTaskList
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (listArgs: ParseResults<TaskListArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let taskStore = deps :> ITaskStore

    let format = listArgs.TryGetResult TaskListArgs.Output |> parseOutputFormat

    // Parse optional filters
    let backlogIdResult =
        listArgs.TryGetResult TaskListArgs.Backlog_Id
        |> Option.map (fun s ->
            match BacklogId.tryCreate s with
            | Ok bid -> Ok(Some bid)
            | Error _ -> Error $"Invalid backlog id '{s}': must match [a-z0-9][a-z0-9-]*")
        |> Option.defaultValue (Ok None)

    let repoId =
        listArgs.TryGetResult TaskListArgs.Repo_Id
        |> Option.map RepoId

    let stateResult =
        listArgs.TryGetResult TaskListArgs.State
        |> Option.map (fun s ->
            match tryParseTaskState s with
            | Ok st -> Ok(Some st)
            | Error msg -> Error msg)
        |> Option.defaultValue (Ok None)

    match backlogIdResult, stateResult with
    | Error msg, _ -> Error msg
    | _, Error msg -> Error msg
    | Ok backlogIdFilter, Ok stateFilter ->
        match taskStore.ListAllTasks coordRoot with
        | Error e -> Error(formatBacklogError e)
        | Ok allTasks ->
            // Apply implicit exclusion of archived tasks when no --state filter provided
            let tasksToProcess =
                match stateFilter with
                | None -> allTasks |> List.filter (fun t -> t.State <> TaskState.Archived)
                | Some _ -> allTasks

            let summaries = Task.listTasks tasksToProcess
            let filtered = Task.filterTasks backlogIdFilter repoId stateFilter summaries

            if filtered.IsEmpty then
                match format with
                | TextOutput -> () // no output in text mode for empty results
                | _ -> printfn "No tasks found."
            else
                match format with
                | JsonOutput ->
                    let items =
                        filtered
                        |> List.map (fun s ->
                            let id = TaskId.value s.Task.Id
                            let backlog = BacklogId.value s.Task.SourceBacklog
                            let repo = RepoId.value s.Task.Repo
                            let state = taskStateToDisplayString s.Task.State
                            let planApproved = if s.PlanApproved then "true" else "false"
                            sprintf "    { \"id\": \"%s\", \"backlog\": \"%s\", \"repo\": \"%s\", \"state\": \"%s\", \"planApproved\": %s }"
                                id backlog repo state planApproved)
                        |> String.concat ",\n"
                    printfn "{ \"tasks\": ["
                    printfn "%s" items
                    printfn "] }"
                | TextOutput ->
                    filtered
                    |> List.iter (fun s ->
                        let id = TaskId.value s.Task.Id
                        let backlog = BacklogId.value s.Task.SourceBacklog
                        let repo = RepoId.value s.Task.Repo
                        let state = taskStateToDisplayString s.Task.State
                        let planApproved = if s.PlanApproved then "yes" else "no"
                        printfn "%s\t%s\t%s\t%s\t%s" id backlog repo state planApproved)
                | TableOutput ->
                    let table = Table()
                    table.AddColumn("Id") |> ignore
                    table.AddColumn("Backlog") |> ignore
                    table.AddColumn("Repo") |> ignore
                    table.AddColumn("State") |> ignore
                    table.AddColumn("Plan Approved") |> ignore

                    filtered
                    |> List.iter (fun s ->
                        let id = TaskId.value s.Task.Id
                        let backlog = BacklogId.value s.Task.SourceBacklog
                        let repo = RepoId.value s.Task.Repo
                        let state = taskStateToDisplayString s.Task.State
                        let planApproved = if s.PlanApproved then "yes" else "no"
                        table.AddRow(id, backlog, repo, state, planApproved) |> ignore)

                    AnsiConsole.Write(table)

            Ok()

// ---------------------------------------------------------------------------
// task info handler
// ---------------------------------------------------------------------------

let private handleTaskInfo
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (infoArgs: ParseResults<TaskInfoArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawTaskId = infoArgs.GetResult TaskInfoArgs.Task_Id
    let format = infoArgs.TryGetResult TaskInfoArgs.Output |> parseOutputFormat

    let taskId = TaskId.create rawTaskId
    let taskStore = deps :> ITaskStore
    let fileSystem = deps :> IFileSystem

    match taskStore.ListAllTasks coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok allTasks ->
        // Resolve plan path: need the SourceBacklog of the target task first
        match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
        | None -> Error(formatBacklogError (TaskNotFound taskId))
        | Some targetTask ->
            let backlogId = BacklogId.value targetTask.SourceBacklog
            let planPath =
                System.IO.Path.Combine(
                    coordRoot, "BACKLOG", backlogId, "tasks", rawTaskId, "plan.md")
            let planExists = fileSystem.FileExists planPath

            match Task.getTaskDetail taskId allTasks planExists with
            | Error e -> Error(formatBacklogError e)
            | Ok detail ->
                let task = detail.Task
                let id = TaskId.value task.Id
                let backlog = BacklogId.value task.SourceBacklog
                let repo = RepoId.value task.Repo
                let state = taskStateToDisplayString task.State
                let createdAt = task.CreatedAt.ToString("yyyy-MM-dd")
                let planExistsStr = if detail.PlanExists then "yes" else "no"
                let planApprovedStr = if detail.PlanApproved then "yes" else "no"

                match format with
                | JsonOutput ->
                    let siblingsJson =
                        detail.Siblings
                        |> List.map (fun s ->
                            sprintf "    { \"id\": \"%s\", \"repo\": \"%s\", \"state\": \"%s\" }"
                                (TaskId.value s.Id)
                                (RepoId.value s.Repo)
                                (taskStateToDisplayString s.State))
                        |> String.concat ",\n"
                    printfn "{"
                    printfn "  \"id\": \"%s\"," id
                    printfn "  \"backlog\": \"%s\"," backlog
                    printfn "  \"repo\": \"%s\"," repo
                    printfn "  \"state\": \"%s\"," state
                    printfn "  \"planExists\": %b," detail.PlanExists
                    printfn "  \"planApproved\": %b," detail.PlanApproved
                    printfn "  \"createdAt\": \"%s\"," createdAt
                    printfn "  \"siblings\": ["
                    if not (List.isEmpty detail.Siblings) then printfn "%s" siblingsJson
                    printfn "  ]"
                    printfn "}"
                | TextOutput ->
                    let siblingsStr =
                        if detail.Siblings.IsEmpty then "-"
                        else detail.Siblings |> List.map (fun s -> TaskId.value s.Id) |> String.concat ","
                    printfn "id\t%s" id
                    printfn "backlog\t%s" backlog
                    printfn "repo\t%s" repo
                    printfn "state\t%s" state
                    printfn "plan exists\t%s" planExistsStr
                    printfn "plan approved\t%s" planApprovedStr
                    printfn "created\t%s" createdAt
                    printfn "siblings\t%s" siblingsStr
                | TableOutput ->
                    let infoTable = Table()
                    infoTable.AddColumn("Field") |> ignore
                    infoTable.AddColumn("Value") |> ignore
                    infoTable.AddRow("id", id) |> ignore
                    infoTable.AddRow("backlog", backlog) |> ignore
                    infoTable.AddRow("repo", repo) |> ignore
                    infoTable.AddRow("state", state) |> ignore
                    infoTable.AddRow("plan exists", planExistsStr) |> ignore
                    infoTable.AddRow("plan approved", planApprovedStr) |> ignore
                    infoTable.AddRow("created", createdAt) |> ignore
                    AnsiConsole.Write(infoTable)

                    if detail.Siblings.IsEmpty then
                        printfn "siblings: (none)"
                    else
                        let sibTable = Table()
                        sibTable.AddColumn("Id") |> ignore
                        sibTable.AddColumn("Repo") |> ignore
                        sibTable.AddColumn("State") |> ignore
                        detail.Siblings
                        |> List.iter (fun s ->
                            sibTable.AddRow(
                                TaskId.value s.Id,
                                RepoId.value s.Repo,
                                taskStateToDisplayString s.State) |> ignore)
                        AnsiConsole.Write(sibTable)

                Ok()

// ---------------------------------------------------------------------------
// task plan handler
// ---------------------------------------------------------------------------

let private handleTaskPlan
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (planArgs: ParseResults<TaskPlanArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawTaskId = planArgs.GetResult TaskPlanArgs.Task_Id
    let useAi = planArgs.Contains TaskPlanArgs.Ai
    let debug = planArgs.Contains TaskPlanArgs.Debug

    let taskId = TaskId.create rawTaskId
    let taskStore = deps :> ITaskStore
    let backlogStore = deps :> IBacklogStore
    let fileSystem = deps :> IFileSystem

    // Load all tasks to find the target
    match taskStore.ListAllTasks coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok allTasks ->
        match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
        | None -> Error(formatBacklogError (TaskNotFound taskId))
        | Some task ->
            // Validate state and get updated task
            match Task.planTask task with
            | Error e -> Error(formatBacklogError e)
            | Ok (updatedTask, wasAlreadyPlanned) ->
                if wasAlreadyPlanned then
                    printfn "Re-planning task %s (was already planned)." rawTaskId

                let backlogId = updatedTask.SourceBacklog

                // Load backlog item for template data
                match backlogStore.LoadBacklogItem coordRoot backlogId with
                | Error e -> Error(formatBacklogError e)
                | Ok backlogItem ->
                    let repo = RepoId.value updatedTask.Repo
                    let backlogIdStr = BacklogId.value backlogId
                    let title = backlogItem.Title
                    let summary = backlogItem.Summary |> Option.defaultValue ""
                    let deps_ = backlogItem.Dependencies |> List.map BacklogId.value
                    let ac = backlogItem.AcceptanceCriteria

                    let dependenciesStr =
                        if deps_.IsEmpty then "- none"
                        else deps_ |> List.map (fun d -> $"- {d}") |> String.concat "\n"

                    let acceptanceCriteriaStr =
                        if ac.IsEmpty then "- none"
                        else ac |> List.map (fun c -> $"- {c}") |> String.concat "\n"

                    // Resolve asset directory relative to the executable
                    let exeDir = AppDomain.CurrentDomain.BaseDirectory
                    let templatePath = IO.Path.Combine(exeDir, "assets", "plan-template.md")
                    let promptPath = IO.Path.Combine(exeDir, "assets", "plan-prompt.md")

                    // Always render the template skeleton first (metadata filled, open sections intact)
                    let skeletonResult =
                        match fileSystem.ReadFile templatePath with
                        | Error _ -> Error $"Could not read plan-template.md from {templatePath}"
                        | Ok template ->
                            let rendered =
                                init
                                |> add "title" title
                                |> add "taskId" rawTaskId
                                |> add "backlogId" backlogIdStr
                                |> add "repo" repo
                                |> add "summary" summary
                                |> add "dependencies" dependenciesStr
                                |> add "acceptanceCriteria" acceptanceCriteriaStr
                                |> fromNoneHtmlText template
                            Ok rendered

                    // Generate plan content
                    let planContentResult =
                        match skeletonResult with
                        | Error e -> Error e
                        | Ok skeleton ->
                            if useAi then
                                // Send the rendered skeleton to the AI to fill the open sections
                                match fileSystem.ReadFile promptPath with
                                | Error _ ->
                                    Error $"Could not read plan-prompt.md from {promptPath}"
                                | Ok promptTemplate ->
                                    let renderedPrompt =
                                        init
                                        |> add "planSkeleton" skeleton
                                        |> fromNoneHtmlText promptTemplate

                                    let harness = deps :> IAgentHarness
                                    harness.Prompt renderedPrompt debug
                                    |> Result.mapError (fun e -> e)
                            else
                                Ok skeleton

                    match planContentResult with
                    | Error msg -> Error msg
                    | Ok planContent ->
                        // Write plan.md
                        let planPath =
                            IO.Path.Combine(
                                coordRoot, "BACKLOG", backlogIdStr, "tasks", rawTaskId, "plan.md")

                        match fileSystem.WriteFile planPath planContent with
                        | Error e -> Error $"Failed to write plan.md: %A{e}"
                        | Ok () ->
                            // Write updated task
                            match taskStore.WriteTask coordRoot updatedTask with
                            | Error e -> Error(formatBacklogError e)
                            | Ok () ->
                                printfn "Plan written: %s" planPath
                                Ok()

// ---------------------------------------------------------------------------
// task approve handler
// ---------------------------------------------------------------------------

let private handleTaskApprove
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (approveArgs: ParseResults<TaskApproveArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawTaskId = approveArgs.GetResult TaskApproveArgs.Task_Id
    let taskId = TaskId.create rawTaskId
    let taskStore = deps :> ITaskStore
    let fileSystem = deps :> IFileSystem

    match taskStore.ListAllTasks coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok allTasks ->
        match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
        | None -> Error(formatBacklogError (TaskNotFound taskId))
        | Some task ->
            let backlogId = BacklogId.value task.SourceBacklog
            let planPath =
                System.IO.Path.Combine(
                    coordRoot, "BACKLOG", backlogId, "tasks", rawTaskId, "plan.md")
            let planExists = fileSystem.FileExists planPath

            match Task.approveTask task planExists with
            | Error e -> Error(formatBacklogError e)
            | Ok (updatedTask, wasAlreadyApproved) ->
                if wasAlreadyApproved then
                    printfn "Task '%s' is already approved." rawTaskId
                    Ok()
                else
                    match taskStore.WriteTask coordRoot updatedTask with
                    | Error e -> Error(formatBacklogError e)
                    | Ok () ->
                        printfn "Task '%s' approved." rawTaskId
                        Ok()

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
    let format = listArgs.TryGetResult ListArgs.Output |> parseOutputFormat

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

        match format with
        | JsonOutput ->
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
        | TextOutput ->
            items
            |> List.iter (fun s ->
                let id = BacklogId.value s.Item.Id
                let itemType = BacklogItemType.toString s.Item.Type
                let priority = s.Item.Priority |> Option.defaultValue "-"
                let status = backlogItemStatusToString s.Status
                let viewId = s.ViewId |> Option.defaultValue "-"
                let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")
                let title = s.Item.Title
                printfn "%s\t%s\t%s\t%s\t%s\t%d\t%s\t%s" id itemType priority status viewId s.TaskCount createdAt title)
        | TableOutput ->
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
    let format = infoArgs.TryGetResult InfoArgs.Output |> parseOutputFormat

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

            match format with
            | JsonOutput ->
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
                            | TaskState.Archived -> "archived"
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
            | TextOutput ->
                let priorityStr = item.Priority |> Option.defaultValue "-"
                let viewStr = detail.ViewId |> Option.defaultValue "-"
                let reposStr = if repos.IsEmpty then "-" else String.concat "," repos
                let depsStr = if deps_.IsEmpty then "-" else String.concat "," deps_
                let summaryStr = summary.Replace('\n', ' ').Replace('\r', ' ')
                let acStr =
                    if ac.IsEmpty then "-"
                    else ac |> List.map (fun s -> s.Replace('\n', ' ').Replace('\r', ' ')) |> String.concat ","
                let tasksStr =
                    if detail.Tasks.IsEmpty then "-"
                    else detail.Tasks |> List.map (fun t -> TaskId.value t.Id) |> String.concat ","
                printfn "id\t%s" id
                printfn "title\t%s" item.Title
                printfn "type\t%s" itemType
                printfn "priority\t%s" priorityStr
                printfn "status\t%s" status
                printfn "view\t%s" viewStr
                printfn "summary\t%s" summaryStr
                printfn "acceptance criteria\t%s" acStr
                printfn "dependencies\t%s" depsStr
                printfn "repos\t%s" reposStr
                printfn "created\t%s" createdAt
                printfn "taskCount\t%d" detail.Tasks.Length
                printfn "tasks\t%s" tasksStr
            | TableOutput ->
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
                        | TaskState.Archived -> "archived"
                    tasksTable.AddRow(tid, repo, state) |> ignore)

                AnsiConsole.Write(tasksTable)

            Ok()

// ---------------------------------------------------------------------------
// backlog take handler
// ---------------------------------------------------------------------------
// Note: write-command handlers (handleBacklogTake, handleBacklogAdd, handleProductRegister)
// intentionally retain `outputJson: bool` — they are not scripting targets and are excluded
// from the OutputFormat refactor at MVP.

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
    let isInteractive = addArgs.Contains AddArgs.Interactive
    let rawBacklogIdOpt = addArgs.TryGetResult AddArgs.Backlog_Id
    let titleOpt = addArgs.TryGetResult AddArgs.Title
    let repo = addArgs.TryGetResult AddArgs.Repo
    let itemType = addArgs.TryGetResult AddArgs.Item_Type
    let summary = addArgs.TryGetResult AddArgs.Summary
    let priority = addArgs.TryGetResult AddArgs.Priority
    let dependsOn = addArgs.GetResults AddArgs.Depends_On

    // Runtime validation for non-interactive mode
    if not isInteractive then
        match rawBacklogIdOpt with
        | None -> Error "--backlog-id is required when not using --interactive"
        | Some _ ->
        match titleOpt with
        | None -> Error "--title is required when not using --interactive"
        | Some _ ->

        let rawBacklogId = rawBacklogIdOpt.Value
        let title = titleOpt.Value
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
    else
        // Interactive mode
        let backlogStore = deps :> IBacklogStore
        let productConfig = toProductConfig resolved.Definition

        let prefilled: InteractivePrompts.PrefilledArgs =
            { BacklogId = rawBacklogIdOpt
              Title = titleOpt
              Repo = repo
              ItemType = itemType
              Priority = priority
              Summary = summary
              DependsOn = dependsOn }

        match InteractivePrompts.promptBacklogAdd backlogStore coordRoot productConfig prefilled with
        | Error msg ->
            eprintfn "Error: %s" msg
            Error msg
        | Ok input ->
            // Check for duplicate before creating
            match BacklogId.tryCreate input.BacklogId with
            | Error _ -> Error $"Invalid backlog id '{input.BacklogId}': must match [a-z0-9][a-z0-9-]*"
            | Ok backlogId ->
                if backlogStore.BacklogItemExists coordRoot backlogId then
                    Error(formatBacklogError (DuplicateBacklogId backlogId))
                else
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
                    match results.TryGetResult Task with
                    | Some taskResults ->
                        match taskResults.TryGetResult TaskArgs.List with
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
                                        | Ok resolved -> handleTaskList deps resolved listArgs

                        | None ->
                            match taskResults.TryGetResult TaskArgs.Info with
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
                                            | Ok resolved -> handleTaskInfo deps resolved infoArgs

                            | None ->
                                match taskResults.TryGetResult TaskArgs.Plan with
                                | Some planArgs ->
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
                                                 | Ok resolved -> handleTaskPlan deps resolved planArgs

                                 | None ->
                                     match taskResults.TryGetResult TaskArgs.Approve with
                                     | Some approveArgs ->
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
                                                     | Ok resolved -> handleTaskApprove deps resolved approveArgs

                                     | None -> Error "Specify a task subcommand (e.g. task list or task info <id> or task plan <id> or task approve <id>)"

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
