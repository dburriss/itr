module Itr.Cli.Program

open System
open Argu
open Fue.Data
open Fue.Compiler
open Spectre.Console
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Domain.Tasks
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliParsers

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
            | Item_Type _ -> "item type: feature | bug | chore | refactor | spike (default: feature)"
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
    | [<CustomCommandLine("--exclude")>] Exclude of status: string
    | Order_By of order_by: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | View _ -> "filter by view id"
            | Status _ -> "filter by status: created | planning | planned | approved | in-progress | completed | archived"
            | Type _ -> "filter by item type: feature | bug | chore | refactor | spike"
             | Output _ -> "output mode: table (default) | json | text"
            | Exclude _ -> "exclude items with this status (can be repeated)"
            | Order_By _ -> "override sort order: created | priority | type"

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
type ProfileAddArgs =
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
type ProfileListArgs =
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfileSetDefaultArgs =
    | [<MainCommand; Mandatory>] Name of name: string
    | Local
    | Global

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Name _ -> "profile name to set as default"
            | Local -> "update the local itr.json in the current product root"
            | Global -> "update the global itr.json"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfileArgs =
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<ProfileAddArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ProfileListArgs>
    | [<CliPrefix(CliPrefix.None)>] Set_Default of ParseResults<ProfileSetDefaultArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Add _ -> "add a new profile to the portfolio"
            | List _ -> "list all profiles in the portfolio"
            | Set_Default _ -> "set an existing profile as the default"

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
type ProductListArgs =
    | Profile of profile: string
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "profile name (defaults to active profile)"
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductInfoArgs =
    | [<MainCommand>] Product_Id of product_id: string
    | [<AltCommandLine("-o")>] Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Product_Id _ -> "product id to inspect (optional; auto-detected from current directory if omitted)"
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductArgs =
    | [<CliPrefix(CliPrefix.None)>] Init of ParseResults<ProductInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<ProductRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ProductListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<ProductInfoArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init _ -> "scaffold a new product"
            | Register _ -> "register an existing product root in the portfolio"
            | List _ -> "list products registered in the active profile"
            | Info _ -> "show detailed information about a product"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskListArgs =
    | [<AltCommandLine("--backlog")>] Backlog_Id of backlog_id: string
    | [<AltCommandLine("--repo")>] Repo_Id of repo_id: string
    | State of state: string
    | [<AltCommandLine("-o")>] Output of output: string
    | Exclude of state: string
    | Order_By of field: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "filter by backlog item id"
            | Repo_Id _ -> "filter by repo id"
             | State _ -> "filter by task state (planning | planned | approved | in_progress | implemented | validated | archived)"
             | Output _ -> "output mode: table (default) | json | text"
            | Exclude _ -> "exclude tasks with this state (planning | planned | approved | in_progress | implemented | validated | archived)"
            | Order_By _ -> "sort order: created | state"

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

[<CliPrefix(CliPrefix.DoubleDash)>]
type ViewListArgs =
    | [<AltCommandLine("-o")>] Output of output: string
    | Product of product: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Output _ -> "output mode: table (default) | json | text"
            | Product _ -> "product id (defaults to product resolved from working directory)"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ViewArgs =
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ViewListArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | List _ -> "list all named backlog views for a product"

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>
    | [<CustomCommandLine("profile")>] ProfileCmd of ParseResults<ProfileArgs>
    | [<CliPrefix(CliPrefix.None)>] Product of ParseResults<ProductArgs>
    | [<CliPrefix(CliPrefix.None)>] Task of ParseResults<TaskArgs>
    | [<CliPrefix(CliPrefix.None)>] View of ParseResults<ViewArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"
            | Backlog _ -> "backlog commands"
            | ProfileCmd _ -> "profile management commands"
            | Product _ -> "product management commands"
            | Task _ -> "task commands"
            | View _ -> "view commands"

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
    | DuplicateBacklogId id -> $"Backlog item '{BacklogId.value id}' already exists"
    | InvalidItemType value -> $"Invalid item type '{value}': must be feature | bug | chore | refactor | spike"
    | MissingTitle -> "--title is required"

let private formatTaskError (err: TaskError) : string =
    match err with
    | TaskNotFound id -> $"Task not found: {TaskId.value id}"
    | TaskIdConflict id -> $"Task id '{TaskId.value id}' already exists"
    | TaskIdOverrideRequiresSingleRepo -> "--task-id can only be used with single-repo backlog items"
    | InvalidTaskState(id, current) ->
        let stateStr = taskStateToDisplayString current
        $"Invalid state transition for task '{TaskId.value id}': current state is '{stateStr}'"
    | MissingPlanArtifact id -> $"Cannot approve task '{TaskId.value id}': plan artifact does not exist"
    | TaskStoreError(_, msg) -> msg

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
    | ProfileNotFound name -> $"Profile '{name}' not found. Run 'profile add {name}' to create it."
    | other -> $"%A{other}"

// ---------------------------------------------------------------------------
// Project ProductDefinition to ProductConfig for task use case
// ---------------------------------------------------------------------------

let private toProductConfig (def: ProductDefinition) : ProductConfig =
    let repos =
        def.Repos |> Map.toSeq |> Seq.map (fun (k, v) -> RepoId k, v) |> Map.ofSeq

    { Id = def.Id; Repos = repos }

// ---------------------------------------------------------------------------
// resolvePortfolio helper — shared load→resolveActiveProfile boilerplate
// ---------------------------------------------------------------------------

let private resolvePortfolio
    (deps: AppDeps)
    (configPath: string)
    (profile: string option)
    : Result<Profile, string> =
    Portfolios.Query.load (Some configPath)
    |> Effect.run deps
    |> Result.mapError (fun e -> $"%A{e}")
    |> Result.bind (fun portfolio ->
        Portfolios.Query.resolveActiveProfile portfolio profile
        |> Effect.run deps
        |> Result.mapError (fun e -> $"%A{e}"))

// ---------------------------------------------------------------------------
// Active patterns for dispatch simplification
// ---------------------------------------------------------------------------

let private (|BacklogTake|_|) (r: ParseResults<BacklogArgs>) =
    r.TryGetResult BacklogArgs.Take

let private (|BacklogAdd|_|) (r: ParseResults<BacklogArgs>) =
    r.TryGetResult BacklogArgs.Add

let private (|BacklogList|_|) (r: ParseResults<BacklogArgs>) =
    r.TryGetResult BacklogArgs.List

let private (|BacklogInfo|_|) (r: ParseResults<BacklogArgs>) =
    r.TryGetResult BacklogArgs.Info

let private (|ProfileAdd|_|) (r: ParseResults<ProfileArgs>) =
    r.TryGetResult ProfileArgs.Add

let private (|ProfileList|_|) (r: ParseResults<ProfileArgs>) =
    r.TryGetResult ProfileArgs.List

let private (|ProfileSetDefault|_|) (r: ParseResults<ProfileArgs>) =
    r.TryGetResult ProfileArgs.Set_Default

let private (|ProductInit|_|) (r: ParseResults<ProductArgs>) =
    r.TryGetResult ProductArgs.Init

let private (|ProductRegister|_|) (r: ParseResults<ProductArgs>) =
    r.TryGetResult ProductArgs.Register

let private (|ProductList|_|) (r: ParseResults<ProductArgs>) =
    r.TryGetResult ProductArgs.List

let private (|ProductInfo|_|) (r: ParseResults<ProductArgs>) =
    r.TryGetResult ProductArgs.Info

let private (|TaskList|_|) (r: ParseResults<TaskArgs>) =
    r.TryGetResult TaskArgs.List

let private (|TaskInfo|_|) (r: ParseResults<TaskArgs>) =
    r.TryGetResult TaskArgs.Info

let private (|TaskPlan|_|) (r: ParseResults<TaskArgs>) =
    r.TryGetResult TaskArgs.Plan

let private (|TaskApprove|_|) (r: ParseResults<TaskArgs>) =
    r.TryGetResult TaskArgs.Approve

let private (|ViewList|_|) (r: ParseResults<ViewArgs>) =
    r.TryGetResult ViewArgs.List

// ---------------------------------------------------------------------------
// resolveProduct helper — resolve first product from profile
// ---------------------------------------------------------------------------

let private resolveProduct
    (deps: AppDeps)
    (activeProfile: Profile)
    : Result<ResolvedProduct, string> =
    match activeProfile.Products with
    | [] -> Error "No products in active profile"
    | productRef :: _ ->
        let (ProductRoot root) = productRef.Root
        let productConfig = deps :> IProductConfig

        match productConfig.LoadProductConfig root with
        | Error e -> Error(formatPortfolioError e)
        | Ok definition ->
            Portfolios.Query.resolveProduct activeProfile (ProductId.value definition.Id)
            |> Effect.run deps
            |> Result.mapError (fun e -> $"%A{e}")

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

    let format = listArgs.TryGetResult TaskListArgs.Output |> OutputFormat.tryParse

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

    let excludeResult =
        listArgs.TryGetResult TaskListArgs.Exclude
        |> Option.map (fun s ->
            match tryParseTaskState s with
            | Ok st -> Ok [ st ]
            | Error msg -> Error msg)
        |> Option.defaultValue (Ok [])

    let orderByResult =
        listArgs.TryGetResult TaskListArgs.Order_By
        |> Option.map (fun s ->
            match s with
            | "created" -> Ok "created"
            | "state" -> Ok "state"
            | other -> Error $"Unknown order-by value '{other}': must be created | state")
        |> Option.defaultValue (Ok "created")

    match backlogIdResult, stateResult, excludeResult, orderByResult with
    | Error msg, _, _, _ -> Error msg
    | _, Error msg, _, _ -> Error msg
    | _, _, Error msg, _ -> Error msg
    | _, _, _, Error msg -> Error msg
    | Ok backlogIdFilter, Ok stateFilter, Ok excludeList, Ok orderBy ->
        match taskStore.ListAllTasks coordRoot with
        | Error e -> Error(formatTaskError e)
        | Ok allTasks ->
            let summaries = Tasks.Query.list allTasks
            let filterInput: Tasks.Query.FilterInput = { BacklogId = backlogIdFilter; Repo = repoId; State = stateFilter; Exclude = excludeList }
            let filtered = Tasks.Query.filter filterInput summaries

            let taskStatePriority state =
                match state with
                | TaskState.Planning -> 7
                | TaskState.Planned -> 6
                | TaskState.Approved -> 5
                | TaskState.InProgress -> 4
                | TaskState.Implemented -> 3
                | TaskState.Validated -> 2
                | TaskState.Archived -> 1

            let ordered =
                match orderBy with
                | "state" -> filtered |> List.sortByDescending (fun s -> taskStatePriority s.Task.State)
                | _ -> filtered |> List.sortBy (fun s -> s.Task.CreatedAt)

            let summaryRows : TaskListSummary list =
                ordered
                |> List.map (fun s ->
                    { Task = s.Task
                      TaskYamlPath = s.TaskYamlPath
                      PlanMdPath = s.PlanMdPath })

            TaskFormatter.formatList format summaryRows
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
    let format = infoArgs.TryGetResult TaskInfoArgs.Output |> OutputFormat.tryParse

    let taskId = TaskId.create rawTaskId
    let taskStore = deps :> ITaskStore

    match taskStore.ListAllTasks coordRoot with
    | Error e -> Error(formatTaskError e)
    | Ok allTaskTuples ->
        let allTasks = allTaskTuples |> List.map fst
        match allTaskTuples |> List.tryFind (fun (t, _) -> t.Id = taskId) with
        | None -> Error(formatTaskError (TaskNotFound taskId))
        | Some (_, taskYamlPath) ->

            let detailInput: Tasks.Query.DetailInput = { TaskId = taskId; AllTasks = allTasks; TaskYamlPath = taskYamlPath }
            match Tasks.Query.getDetail detailInput with
            | Error e -> Error(formatTaskError e)
            | Ok detail ->
                let taskDetailView : TaskDetailView =
                    { Task = detail.Task
                      Siblings = detail.Siblings
                      PlanExists = detail.PlanExists
                      PlanApproved = detail.PlanApproved
                      TaskYamlPath = detail.TaskYamlPath
                      PlanMdPath = detail.PlanMdPath }
                TaskFormatter.formatDetail format taskDetailView
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

    match taskStore.ListAllTasks coordRoot with
    | Error e -> Error(formatTaskError e)
    | Ok allTaskTuples ->
        let allTasks = allTaskTuples |> List.map fst
        match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
        | None -> Error(formatTaskError (TaskNotFound taskId))
        | Some task ->
            match Tasks.Plan.execute task with
            | Error e -> Error(formatTaskError e)
            | Ok (updatedTask, wasAlreadyPlanned) ->
                if wasAlreadyPlanned then
                    printfn "Re-planning task %s (was already planned)." rawTaskId

                let backlogId = updatedTask.SourceBacklog

                match backlogStore.LoadBacklogItem coordRoot backlogId with
                | Error e -> Error(formatBacklogError e)
                | Ok (backlogItem, _) ->
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

                    let exeDir = AppDomain.CurrentDomain.BaseDirectory
                    let templatePath = IO.Path.Combine(exeDir, "assets", "plan-template.md")
                    let promptPath = IO.Path.Combine(exeDir, "assets", "plan-prompt.md")

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

                    let planContentResult =
                        match skeletonResult with
                        | Error e -> Error e
                        | Ok skeleton ->
                            if useAi then
                                let globalAgentConfig = resolved.Profile.AgentConfig

                                let (ProductRoot productRoot) = resolved.Product.Root
                                let agentConfig =
                                    match PortfolioAdapter.LoadLocalConfig productRoot with
                                    | Some localConfig -> localConfig
                                    | None -> globalAgentConfig

                                let harness =
                                    AgentHarnessSelector.selectHarness
                                        agentConfig.Protocol
                                        agentConfig.Command
                                        agentConfig.Args
                                        coordRoot

                                match fileSystem.ReadFile promptPath with
                                | Error _ ->
                                    Error $"Could not read plan-prompt.md from {promptPath}"
                                | Ok promptTemplate ->
                                    let renderedPrompt =
                                        init
                                        |> add "planSkeleton" skeleton
                                        |> fromNoneHtmlText promptTemplate

                                    harness.Prompt renderedPrompt debug
                                    |> Result.mapError (fun e -> e)
                            else
                                Ok skeleton

                    match planContentResult with
                    | Error msg -> Error msg
                    | Ok rawPlanContent ->
                        let planContent = Itr.Adapters.AcpMessages.trimPreamble rawPlanContent
                        let planPath = ItrTask.planFile coordRoot backlogId (TaskId.create rawTaskId)

                        match fileSystem.WriteFile planPath planContent with
                        | Error e -> Error $"Failed to write plan.md: %A{e}"
                        | Ok () ->
                            match taskStore.WriteTask coordRoot updatedTask with
                            | Error e -> Error(formatTaskError e)
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
    | Error e -> Error(formatTaskError e)
    | Ok allTaskTuples ->
        let allTasks = allTaskTuples |> List.map fst
        match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
        | None -> Error(formatTaskError (TaskNotFound taskId))
        | Some task ->
            let planPath = ItrTask.planFile coordRoot task.SourceBacklog (TaskId.create rawTaskId)
            let planExists = fileSystem.FileExists planPath

            let approveInput: Tasks.Approve.Input = { Task = task; PlanExists = planExists }
            match Tasks.Approve.execute approveInput with
            | Error e -> Error(formatTaskError e)
            | Ok (updatedTask, wasAlreadyApproved) ->
                if wasAlreadyApproved then
                    printfn "Task '%s' is already approved." rawTaskId
                    Ok()
                else
                    match taskStore.WriteTask coordRoot updatedTask with
                    | Error e -> Error(formatTaskError e)
                    | Ok () ->
                        printfn "Task '%s' approved." rawTaskId
                        Ok()

// ---------------------------------------------------------------------------
// product info handler
// ---------------------------------------------------------------------------

let private handleProductInfo
    (configPath: string)
    (deps: AppDeps)
    (infoArgs: ParseResults<ProductInfoArgs>)
    : Result<unit, string> =
    let format = infoArgs.TryGetResult ProductInfoArgs.Output |> OutputFormat.tryParse
    let productConfig = deps :> IProductConfig
    let fileSystem = deps :> IFileSystem

    let definitionResult : Result<string * ProductDefinition, string> =
        match infoArgs.TryGetResult ProductInfoArgs.Product_Id with
        | Some rawId ->
            let portfolioResult =
                Portfolios.Query.load (Some configPath)
                |> Effect.run deps
                |> Result.mapError formatPortfolioError

            match portfolioResult with
            | Error msg -> Error msg
            | Ok portfolio ->
                match Portfolios.Query.resolveActiveProfile portfolio None |> Effect.run deps with
                | Error e -> Error(formatPortfolioError e)
                | Ok activeProfile ->
                    match Portfolios.Query.loadAllDefinitions activeProfile deps with
                    | Error e -> Error(formatPortfolioError e)
                    | Ok pairs ->
                        match pairs |> List.tryFind (fun (_, def) -> ProductId.value def.Id = rawId) with
                        | None -> Error $"Product '{rawId}' not found in active profile."
                        | Some (productRef, definition) ->
                            let (ProductRoot root) = productRef.Root
                            Ok(root, definition)
        | None ->
            let cwd = IO.Directory.GetCurrentDirectory()
            match ProductLocator.locateProductRoot fileSystem cwd with
            | None ->
                Error "No product ID provided and no product.yaml found in current directory or any parent directory."
            | Some productRoot ->
                match productConfig.LoadProductConfig productRoot with
                | Error e -> Error(formatPortfolioError e)
                | Ok definition -> Ok(productRoot, definition)

    match definitionResult with
    | Error msg -> Error msg
    | Ok (productRoot, definition) ->
        let id = ProductId.value definition.Id
        let description = definition.Description |> Option.defaultValue ""

        let docs =
            definition.Docs
            |> Map.toList
            |> List.map (fun (key, relPath) ->
                let absPath = IO.Path.GetFullPath(IO.Path.Combine(productRoot, relPath))
                key, absPath)

        let repos =
            definition.Repos
            |> Map.toList
            |> List.map (fun (key, repoConfig) ->
                let absPath = IO.Path.GetFullPath(IO.Path.Combine(productRoot, repoConfig.Path))
                key, absPath, repoConfig.Url)

        let coordMode = definition.Coordination.Mode
        let coordRepo = definition.Coordination.Repo |> Option.defaultValue ""
        let coordPath = definition.Coordination.Path |> Option.defaultValue ""

        let data : ProductInfoData =
            { Id = id
              Description = description
              Docs = docs
              Repos = repos
              CoordMode = coordMode
              CoordRepo = coordRepo
              CoordPath = coordPath }

        PortfolioFormatter.formatProductInfo format data
        Ok()

// ---------------------------------------------------------------------------
// product list handler
// ---------------------------------------------------------------------------

let private handleProductList
    (configPath: string)
    (deps: AppDeps)
    (listArgs: ParseResults<ProductListArgs>)
    : Result<unit, string> =
    let portfolioResult =
        Portfolios.Query.load (Some configPath)
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match portfolioResult with
    | Error msg -> Error msg
    | Ok portfolio ->
        if portfolio.Profiles.IsEmpty then
            Error "No profiles found. Run 'itr profile add <name>' to create one."
        else
            let flagProfile = listArgs.TryGetResult ProductListArgs.Profile
            let format = listArgs.TryGetResult ProductListArgs.Output |> OutputFormat.tryParse

            match Portfolios.Query.resolveActiveProfile portfolio flagProfile |> Effect.run deps with
            | Error e -> Error(formatPortfolioError e)
            | Ok profile ->
                match Portfolios.Query.loadAllDefinitions profile deps with
                | Error e -> Error(formatPortfolioError e)
                | Ok pairs ->
                    let rows : ProductRow list =
                        pairs
                        |> List.map (fun (_, definition) ->
                            { Id = ProductId.value definition.Id
                              RepoCount = definition.Repos.Count
                              CoordRoot = definition.CoordRoot.AbsolutePath })

                    PortfolioFormatter.formatProductList format rows
                    Ok()

// ---------------------------------------------------------------------------
// profile list handler
// ---------------------------------------------------------------------------

let private handleProfileList
    (configPath: string)
    (portfolio: Portfolio)
    (listArgs: ParseResults<ProfileListArgs>)
    : Result<unit, string> =
    let format = listArgs.TryGetResult ProfileListArgs.Output |> OutputFormat.tryParse

    let profiles =
        portfolio.Profiles
        |> Map.toList
        |> List.map (fun (name, profile) ->
            let nameStr = ProfileName.value name
            let isDefault =
                match portfolio.DefaultProfile with
                | Some d -> d = name
                | None -> false
            let gitName =
                profile.GitIdentity |> Option.map (fun g -> g.Name) |> Option.defaultValue ""
            let gitEmail =
                profile.GitIdentity
                |> Option.bind (fun g -> g.Email)
                |> Option.defaultValue ""
            let productCount = profile.Products.Length
            ({ Name = nameStr
               IsDefault = isDefault
               GitName = gitName
               GitEmail = gitEmail
               ProductCount = productCount } : ProfileRow))

    PortfolioFormatter.formatProfileList format profiles
    Ok()

// ---------------------------------------------------------------------------
// view list handler
// ---------------------------------------------------------------------------

let private handleViewList
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (listArgs: ParseResults<ViewListArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let viewStore = deps :> IViewStore
    let backlogStore = deps :> IBacklogStore
    let format = listArgs.TryGetResult ViewListArgs.Output |> OutputFormat.tryParse

    match viewStore.ListViews coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok views ->
        match backlogStore.ListArchivedBacklogItems coordRoot with
        | Error e -> Error(formatBacklogError e)
        | Ok archivedItems ->
            let archivedIds =
                archivedItems |> List.map (fun (item, _) -> BacklogId.value item.Id) |> Set.ofList

            if views.IsEmpty then
                match format with
                | Json -> printfn "[]"
                | Text -> () // no output for empty in text mode
                | Table -> printfn "No views defined."
            else
                let rows =
                    views
                    |> List.map (fun view ->
                        let description = view.Description |> Option.defaultValue ""
                        let total = view.Items.Length
                        let archived =
                            view.Items
                            |> List.filter (fun id -> archivedIds.Contains(id))
                            |> List.length
                        (view.Id, description, total, archived))

                match format with
                | Json ->
                    let items =
                        rows
                        |> List.map (fun (id, description, total, archived) ->
                            let descJson = description.Replace("\"", "\\\"")
                            sprintf "  { \"id\": \"%s\", \"description\": \"%s\", \"items\": %d, \"archived\": %d }"
                                id descJson total archived)
                        |> String.concat ",\n"
                    printfn "["
                    printfn "%s" items
                    printfn "]"
                | Text ->
                    rows
                    |> List.iter (fun (id, description, total, archived) ->
                        printfn "%s\t%s\t%d\t%d" id description total archived)
                | Table ->
                    let table = Spectre.Console.Table()
                    table.AddColumn("Id") |> ignore
                    table.AddColumn("Description") |> ignore
                    table.AddColumn("Items") |> ignore
                    table.AddColumn("Archived") |> ignore

                    rows
                    |> List.iter (fun (id, description, total, archived) ->
                        table.AddRow(id, description, string total, string archived) |> ignore)

                    AnsiConsole.Write(table)

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
    let format = listArgs.TryGetResult ListArgs.Output |> OutputFormat.tryParse

    let statusFilter =
        listArgs.TryGetResult ListArgs.Status
        |> Option.bind tryParseBacklogItemStatus

    let typeFilter =
        listArgs.TryGetResult ListArgs.Type
        |> Option.bind (fun t ->
            match BacklogItemType.tryParse t with
            | Ok bt -> Some bt
            | Error _ -> None)

    let excludeStatuses =
        listArgs.GetResults ListArgs.Exclude
        |> List.choose tryParseBacklogItemStatus

    let orderBy = listArgs.TryGetResult ListArgs.Order_By

    let filter: Backlogs.Query.BacklogListFilter =
        { ViewId = viewFilter
          Status = statusFilter
          ItemType = typeFilter
          ExcludeStatuses = excludeStatuses
          OrderBy = orderBy }

    match Backlogs.Query.loadSnapshot backlogStore taskStore viewStore coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok snapshot ->
        let items = Backlogs.Query.list filter snapshot
        BacklogFormatter.formatList format items
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
    let format = infoArgs.TryGetResult InfoArgs.Output |> OutputFormat.tryParse

    match BacklogId.tryCreate rawBacklogId with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->
        let backlogStore = deps :> IBacklogStore
        let taskStore = deps :> ITaskStore
        let viewStore = deps :> IViewStore

        match Backlogs.Query.getDetail backlogStore taskStore viewStore coordRoot backlogId with
        | Error e -> Error(formatBacklogError e)
        | Ok detail ->
            BacklogFormatter.formatDetail format detail
            Ok()

// ---------------------------------------------------------------------------
// backlog take handler
// ---------------------------------------------------------------------------

let private handleBacklogTake
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (takeArgs: ParseResults<TakeArgs>)
    (format: OutputFormat)
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
            |> Result.bind (fun (backlogItem, _) ->
                taskStore.ListTasks coordRoot backlogId
                |> Result.mapError formatTaskError
                |> Result.map (List.map fst)
                |> Result.bind (fun existingTasks ->
                    let input: Tasks.Take.Input =
                        { Tasks.Take.Input.BacklogId = backlogId
                          Tasks.Take.Input.TaskIdOverride = taskIdOverride }

                    let today = DateOnly.FromDateTime(DateTime.UtcNow)

                    Tasks.Take.execute productConfig backlogItem existingTasks input today
                    |> Result.mapError formatTaskError
                    |> Result.bind (fun newTasks ->
                        let writeResults =
                            newTasks
                            |> List.map (fun task ->
                                taskStore.WriteTask coordRoot task
                                |> Result.map (fun () ->
                                    let taskId = TaskId.value task.Id
                                    let path = ItrTask.taskFile coordRoot task.SourceBacklog task.Id
                                    (taskId, path))
                                |> Result.mapError formatTaskError)

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
            match format with
            | Json ->
                let items =
                    written
                    |> List.map (fun (id, path) -> $"""  {{ "id": "{id}", "path": "{path}" }}""")
                    |> String.concat ",\n"

                printfn """{ "ok": true, "tasks": ["""
                printfn "%s" items
                printfn "] }"
            | _ ->
                written |> List.iter (fun (id, path) -> printfn "Created task: %s → %s" id path)

            Ok()

// ---------------------------------------------------------------------------
// backlog add handler
// ---------------------------------------------------------------------------

let private handleBacklogAdd
    (deps: AppDeps)
    (resolved: ResolvedProduct)
    (addArgs: ParseResults<AddArgs>)
    (format: OutputFormat)
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

        match BacklogId.tryCreate rawBacklogId with
        | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
        | Ok backlogId ->
            if backlogStore.BacklogItemExists coordRoot backlogId then
                Error(formatBacklogError (DuplicateBacklogId backlogId))
            else
                let input: Backlogs.Create.Input =
                    { BacklogId = rawBacklogId
                      Title = title
                      Repos = repo |> Option.toList
                      ItemType = itemType
                      Priority = priority
                      Summary = summary
                      AcceptanceCriteria = []
                      DependsOn = dependsOn }

                let today = DateOnly.FromDateTime(DateTime.UtcNow)

                match Backlogs.Create.execute productConfig input today with
                | Error e -> Error(formatBacklogError e)
                | Ok item ->
                    match backlogStore.WriteBacklogItem coordRoot item with
                    | Error e -> Error(formatBacklogError e)
                    | Ok() ->
                        let id = BacklogId.value item.Id
                        let path = BacklogItem.itemFile coordRoot item.Id

                        match format with
                        | Json -> printfn """{ "ok": true, "id": "%s" }""" id
                        | _ -> printfn "Created backlog item '%s' → %s" id path

                        Ok()
    else
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
            match BacklogId.tryCreate input.BacklogId with
            | Error _ -> Error $"Invalid backlog id '{input.BacklogId}': must match [a-z0-9][a-z0-9-]*"
            | Ok backlogId ->
                if backlogStore.BacklogItemExists coordRoot backlogId then
                    Error(formatBacklogError (DuplicateBacklogId backlogId))
                else
                    let today = DateOnly.FromDateTime(DateTime.UtcNow)

                    match Backlogs.Create.execute productConfig input today with
                    | Error e -> Error(formatBacklogError e)
                    | Ok item ->
                        match backlogStore.WriteBacklogItem coordRoot item with
                        | Error e -> Error(formatBacklogError e)
                        | Ok() ->
                            let id = BacklogId.value item.Id
                            let path = BacklogItem.itemFile coordRoot item.Id

                            match format with
                            | Json -> printfn """{ "ok": true, "id": "%s" }""" id
                            | _ -> printfn "Created backlog item '%s' → %s" id path

                            Ok()

// ---------------------------------------------------------------------------
// product register handler
// ---------------------------------------------------------------------------

let private handleProductRegister
    (deps: AppDeps)
    (configPath: string)
    (profile: string option)
    (registerArgs: ParseResults<ProductRegisterArgs>)
    (format: OutputFormat)
    : Result<unit, string> =
    let path = registerArgs.GetResult ProductRegisterArgs.Path

    let input: Portfolios.RegisterProduct.Input =
        { Path = path
          Profile = profile }

    let result =
        Portfolios.RegisterProduct.execute configPath input
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match result with
    | Error msg -> Error msg
    | Ok updatedPortfolio ->
        let portfolioConfig = deps :> IPortfolioConfig

        portfolioConfig.SaveConfig configPath updatedPortfolio
        |> Result.mapError formatPortfolioError
        |> Result.map (fun () ->
            let productConfig = deps :> IProductConfig

            match productConfig.LoadProductConfig path with
            | Ok definition ->
                let id = ProductId.value definition.Id

                match format with
                | Json -> printfn """{ "ok": true, "id": "%s", "path": "%s" }""" id path
                | _ -> printfn "Registered product '%s' from '%s'." id path
            | Error _ ->
                match format with
                | Json -> printfn """{ "ok": true, "path": "%s" }""" path
                | _ -> printfn "Registered product from '%s'." path)

// ---------------------------------------------------------------------------
// Dispatch
// ---------------------------------------------------------------------------

let private dispatch (deps: AppDeps) (results: ParseResults<CliArgs>) : Result<unit, string> =
    let profile = results.TryGetResult Profile
    let format = results.TryGetResult Output |> OutputFormat.tryParse

    let configPath = (deps :> IPortfolioConfig).ConfigPath()

    let bootstrapResult =
        Portfolios.BootstrapIfMissing.execute configPath
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match bootstrapResult with
    | Error msg -> Error msg
    | Ok wasCreated ->
        if wasCreated then
            printfn "Created default config at %s. Run 'itr init' to configure profiles and products." configPath

        match results.TryGetResult Backlog,
              results.TryGetResult CliArgs.ProfileCmd,
              results.TryGetResult Product,
              results.TryGetResult Task,
              results.TryGetResult View with

        | Some backlogResults, _, _, _, _ ->
            match backlogResults with
            | BacklogTake takeArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleBacklogTake deps resolved takeArgs format)

            | BacklogAdd addArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleBacklogAdd deps resolved addArgs format)

            | BacklogList listArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleBacklogList deps resolved listArgs)

            | BacklogInfo infoArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleBacklogInfo deps resolved infoArgs)

            | _ -> Error "Specify a backlog subcommand (e.g. backlog take <id> or backlog add <id> or backlog list or backlog info <id>)"

        | _, Some profilesResults, _, _, _ ->
            match profilesResults with
            | ProfileAdd addArgs ->
                let name = addArgs.GetResult ProfileAddArgs.Name
                let gitName = addArgs.TryGetResult ProfileAddArgs.Git_Name
                let gitEmail = addArgs.TryGetResult ProfileAddArgs.Git_Email
                let setDefault = addArgs.Contains ProfileAddArgs.Set_Default

                match gitEmail, gitName with
                | Some _, None ->
                    Error "--git-email requires --git-name to also be specified"
                | _ ->
                    let gitIdentity =
                        gitName
                        |> Option.map (fun gn -> { Name = gn; Email = gitEmail })

                    let input: Portfolios.AddProfile.Input =
                        { Name = name
                          GitIdentity = gitIdentity
                          SetAsDefault = setDefault }

                    let result =
                        Portfolios.AddProfile.execute configPath input
                        |> Effect.run deps
                        |> Result.mapError formatPortfolioError

                    match result with
                    | Error msg -> Error msg
                    | Ok updatedPortfolio ->
                        let portfolioConfig = deps :> IPortfolioConfig

                        portfolioConfig.SaveConfig configPath updatedPortfolio
                        |> Result.mapError formatPortfolioError
                        |> Result.map (fun () -> printfn "Added profile '%s'." name)

            | ProfileList listArgs ->
                let portfolioResult =
                    Portfolios.Query.load (Some configPath)
                    |> Effect.run deps
                    |> Result.mapError formatPortfolioError

                match portfolioResult with
                | Error msg -> Error msg
                | Ok portfolio -> handleProfileList configPath portfolio listArgs

            | ProfileSetDefault setDefaultArgs ->
                let name = setDefaultArgs.GetResult ProfileSetDefaultArgs.Name
                let useLocal = setDefaultArgs.Contains ProfileSetDefaultArgs.Local
                let useGlobal = setDefaultArgs.Contains ProfileSetDefaultArgs.Global
                let portfolioConfig = deps :> IPortfolioConfig
                let fs = deps :> IFileSystem

                let targetPathResult : Result<string * string, string> =
                    if useGlobal then
                        Ok(configPath, configPath)
                    elif useLocal then
                        let cwd = System.IO.Directory.GetCurrentDirectory()
                        let productYamlPath = System.IO.Path.Combine(cwd, "product.yaml")

                        if fs.FileExists productYamlPath then
                            let localConfigPath = System.IO.Path.Combine(cwd, "itr.json")
                            Ok(localConfigPath, localConfigPath)
                        else
                            Error "--local flag requires a product context. Run this command from within a product directory or specify --global instead."
                    else
                        let cwd = System.IO.Directory.GetCurrentDirectory()
                        let productYamlPath = System.IO.Path.Combine(cwd, "product.yaml")
                        let localConfigPath = System.IO.Path.Combine(cwd, "itr.json")

                        if fs.FileExists productYamlPath && fs.FileExists localConfigPath then
                            Ok(localConfigPath, localConfigPath)
                        else
                            Ok(configPath, configPath)

                match targetPathResult with
                | Error msg -> Error msg
                | Ok(targetPath, displayPath) ->
                    let bootstrapResult2 =
                        if not (fs.FileExists targetPath) then
                            Portfolios.BootstrapIfMissing.execute targetPath
                            |> Effect.run deps
                            |> Result.mapError formatPortfolioError
                            |> Result.map (fun _ -> ())
                        else
                            Ok()

                    match bootstrapResult2 with
                    | Error msg -> Error msg
                    | Ok() ->
                        let input: Portfolios.SetDefaultProfile.Input = { Name = name }

                        let result =
                            Portfolios.SetDefaultProfile.execute targetPath input
                            |> Effect.run deps
                            |> Result.mapError formatPortfolioError

                        match result with
                        | Error msg -> Error msg
                        | Ok updatedPortfolio ->
                            portfolioConfig.SaveConfig targetPath updatedPortfolio
                            |> Result.mapError formatPortfolioError
                            |> Result.map (fun () ->
                                printfn "Profile '%s' set as default. (%s)" name displayPath)

            | _ -> Error "Specify a profile subcommand (e.g. profile add <name>, profile list, or profile set-default <name>)"

        | _, _, Some productResults, _, _ ->
            match productResults with
            | ProductInit initArgs ->
                let rawPath = initArgs.GetResult ProductInitArgs.Path

                let rawId =
                    match initArgs.TryGetResult ProductInitArgs.Id with
                    | Some id -> id
                    | None -> AnsiConsole.Ask<string>("Product id:")

                let rawRepoId =
                    match initArgs.TryGetResult ProductInitArgs.Repo_Id with
                    | Some repoId -> repoId
                    | None ->
                        let answer = AnsiConsole.Ask<string>($"Repo id (default: same as id):")
                        if String.IsNullOrWhiteSpace(answer) then rawId else answer

                let coordPath = initArgs.TryGetResult ProductInitArgs.Coord_Path |> Option.defaultValue ".itr"
                let coordMode = initArgs.TryGetResult ProductInitArgs.Coord_Mode |> Option.defaultValue "primary-repo"

                let registerProfile =
                    if initArgs.Contains ProductInitArgs.No_Register then
                        None
                    else
                        match initArgs.TryGetResult ProductInitArgs.Register_Profile with
                        | Some p -> Some p
                        | None ->
                            let answer = AnsiConsole.Ask<string>("Register in profile (leave blank to skip):")
                            if String.IsNullOrWhiteSpace(answer) then None else Some answer

                let input: Portfolios.InitProduct.Input =
                    { Id = rawId
                      Path = rawPath
                      RepoId = rawRepoId
                      CoordPath = coordPath
                      CoordinationMode = coordMode
                      RegisterProfile = registerProfile }

                let result =
                    Portfolios.InitProduct.execute configPath input
                    |> Effect.run deps
                    |> Result.mapError formatPortfolioError

                match result with
                | Error msg -> Error msg
                | Ok maybePortfolio ->
                    printfn "Initialized product '%s' at %s." rawId rawPath

                    match maybePortfolio, registerProfile with
                    | Some _, Some prof -> printfn "Registered in profile '%s'." prof
                    | _ -> ()

                    Ok()

            | ProductRegister registerArgs ->
                handleProductRegister deps configPath profile registerArgs format

            | ProductList listArgs ->
                handleProductList configPath deps listArgs

            | ProductInfo infoArgs ->
                handleProductInfo configPath deps infoArgs

            | _ -> Error "Specify a product subcommand (e.g. product init <path>, product register <path>, product list, or product info)"

        | _, _, _, Some taskResults, _ ->
            match taskResults with
            | TaskList listArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleTaskList deps resolved listArgs)

            | TaskInfo infoArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleTaskInfo deps resolved infoArgs)

            | TaskPlan planArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleTaskPlan deps resolved planArgs)

            | TaskApprove approveArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> handleTaskApprove deps resolved approveArgs)

            | _ -> Error "Specify a task subcommand (e.g. task list or task info <id> or task plan <id> or task approve <id>)"

        | _, _, _, _, Some viewResults ->
            match viewResults with
            | ViewList listArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (fun activeProfile ->
                    let productIdOpt = listArgs.TryGetResult ViewListArgs.Product

                    match productIdOpt with
                    | Some rawId ->
                        Portfolios.Query.resolveProduct activeProfile rawId
                        |> Effect.run deps
                        |> Result.mapError (fun e -> $"%A{e}")
                    | None ->
                        resolveProduct deps activeProfile)
                |> Result.bind (fun resolved -> handleViewList deps resolved listArgs)

            | _ -> Error "Specify a view subcommand (e.g. view list)"

        | _ ->
            let loadResult = Portfolios.Query.load (Some configPath) |> Effect.run deps

            loadResult
            |> Result.bind (fun portfolio -> Portfolios.Query.resolveActiveProfile portfolio profile |> Effect.run deps)
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
