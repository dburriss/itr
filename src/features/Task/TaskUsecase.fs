module Itr.Features.Task

open System.IO
open Itr.Domain

// ---------------------------------------------------------------------------
// TaskSummary type
// ---------------------------------------------------------------------------

type TaskSummary =
    { Task: ItrTask
      PlanApproved: bool
      TaskYamlPath: string
      PlanMdPath: string option }

// ---------------------------------------------------------------------------
// SiblingTask type (used in TaskDetail)
// ---------------------------------------------------------------------------

type SiblingTask =
    { Id: TaskId
      Repo: RepoId
      State: TaskState }

// ---------------------------------------------------------------------------
// TaskDetail type
// ---------------------------------------------------------------------------

type TaskDetail =
    { Task: ItrTask
      PlanExists: bool
      PlanApproved: bool
      Siblings: SiblingTask list
      TaskYamlPath: string
      PlanMdPath: string option }

// ---------------------------------------------------------------------------
// listTasks: wraps a list of (ItrTask * taskYamlPath) into TaskSummary list
// ---------------------------------------------------------------------------

/// Wrap raw tasks (with their yaml paths) into TaskSummary values.
/// PlanApproved = true for Approved | InProgress | Implemented | Validated | Archived.
/// PlanMdPath is Some if plan.md exists alongside task.yaml.
let listTasks (tasks: (ItrTask * string) list) : TaskSummary list =
    tasks
    |> List.map (fun (task, taskYamlPath) ->
        let planApproved =
            match task.State with
            | TaskState.Approved
            | TaskState.InProgress
            | TaskState.Implemented
            | TaskState.Validated
            | TaskState.Archived -> true
            | _ -> false
        let planMdPath =
            let dir = Path.GetDirectoryName(taskYamlPath)
            if isNull dir || dir = "" then None
            else
                let candidate = Path.Combine(dir, "plan.md")
                if File.Exists(candidate) then Some candidate else None
        { Task = task; PlanApproved = planApproved; TaskYamlPath = taskYamlPath; PlanMdPath = planMdPath })

// ---------------------------------------------------------------------------
// filterTasks: apply optional AND filters to a TaskSummary list
// ---------------------------------------------------------------------------

/// Filter task summaries by optional backlog id, repo id, state, and an exclude list.
/// All provided filters are applied as AND. Tasks whose state is in the exclude list are removed.
let filterTasks
    (backlogId: BacklogId option)
    (repo: RepoId option)
    (state: TaskState option)
    (exclude: TaskState list)
    (summaries: TaskSummary list)
    : TaskSummary list =
    summaries
    |> List.filter (fun s ->
        let matchesBacklog =
            match backlogId with
            | None -> true
            | Some bid -> s.Task.SourceBacklog = bid
        let matchesRepo =
            match repo with
            | None -> true
            | Some rid -> s.Task.Repo = rid
        let matchesState =
            match state with
            | None -> true
            | Some st -> s.Task.State = st
        let notExcluded = not (List.contains s.Task.State exclude)
        matchesBacklog && matchesRepo && matchesState && notExcluded)

// ---------------------------------------------------------------------------
// Input type for the use case
// ---------------------------------------------------------------------------

type TakeInput =
    { BacklogId: BacklogId
      TaskIdOverride: string option }

// ---------------------------------------------------------------------------
// TaskId derivation (pure)
// ---------------------------------------------------------------------------

/// Generate a unique task id that does not collide with any existing id.
/// Strategy:
///   - single-repo, no existing tasks → use backlog id
///   - single-repo re-take, or multi-repo → "<repo-id>-<backlog-id>"
///   - collision with either of the above → append numeric suffix "-2", "-3", ...
let private deriveTaskId
    (existingIds: Set<string>)
    (backlogId: BacklogId)
    (repoId: RepoId)
    (isSingleRepo: bool)
    (isFirstTake: bool)
    : TaskId =
    let bid = BacklogId.value backlogId
    let rid = RepoId.value repoId

    let baseId = if isSingleRepo && isFirstTake then bid else $"{rid}-{bid}"

    if not (Set.contains baseId existingIds) then
        TaskId.create baseId
    else
        let rec findSuffix n =
            let candidate = $"{baseId}-{n}"

            if not (Set.contains candidate existingIds) then
                TaskId.create candidate
            else
                findSuffix (n + 1)

        findSuffix 2

// ---------------------------------------------------------------------------
// The use case
// ---------------------------------------------------------------------------

/// Pure pipeline: does not perform I/O. Returns the list of ItrTask values
/// for the caller to write. Accepts already-loaded data as parameters.
/// Note: repo validation (RepoNotInProduct) is a BacklogError concern; callers validate
/// repos before invoking this function. This function only returns TaskError.
let takeBacklogItem
    (productConfig: ProductConfig)
    (backlogItem: BacklogItem)
    (existingTasks: ItrTask list)
    (input: TakeInput)
    (today: System.DateOnly)
    : Result<ItrTask list, TaskError> =

    // 1. Validate all repos on the item exist in product.yaml
    let invalidRepo =
        backlogItem.Repos
        |> List.tryFind (fun repoId -> not (Map.containsKey repoId productConfig.Repos))

    match invalidRepo with
    | Some repoId -> Error(TaskStoreError("", $"Repo '{RepoId.value repoId}' is not listed in product.yaml"))
    | None ->

        // 2. Handle --task-id override
        match input.TaskIdOverride with
        | Some overrideId when backlogItem.Repos.Length > 1 -> Error TaskIdOverrideRequiresSingleRepo

        | Some overrideId ->
            // Single-repo with explicit task id
            match TaskId.tryCreate overrideId with
            | Error _ -> Error(TaskIdConflict(TaskId.create overrideId))
            | Ok taskId ->
                let existingIdSet =
                    existingTasks |> List.map (fun t -> TaskId.value t.Id) |> Set.ofList

                if Set.contains (TaskId.value taskId) existingIdSet then
                    Error(TaskIdConflict taskId)
                else
                    let repo = backlogItem.Repos |> List.head

                    Ok
                        [ { Id = taskId
                            SourceBacklog = input.BacklogId
                            Repo = repo
                            State = TaskState.Planning
                            CreatedAt = today } ]

        | None ->
            // Auto-derive task ids
            let existingIdSet =
                existingTasks |> List.map (fun t -> TaskId.value t.Id) |> Set.ofList

            let isSingleRepo = backlogItem.Repos.Length = 1
            let isFirstTake = existingTasks.IsEmpty

            // Track ids already allocated in this call to avoid intra-call collisions
            let tasks, _ =
                backlogItem.Repos
                |> List.fold
                    (fun (acc, allocatedIds) repoId ->
                        let allExisting = Set.union existingIdSet allocatedIds

                        let taskId =
                            deriveTaskId allExisting input.BacklogId repoId isSingleRepo isFirstTake

                        let task =
                            { Id = taskId
                              SourceBacklog = input.BacklogId
                              Repo = repoId
                              State = TaskState.Planning
                              CreatedAt = today }

                        (task :: acc, Set.add (TaskId.value taskId) allocatedIds))
                    ([], Set.empty)

            Ok(List.rev tasks)

// ---------------------------------------------------------------------------
// getTaskDetail: pure function returning TaskDetail for a single task id
// ---------------------------------------------------------------------------

/// Return the full detail record for the task with the given id.
/// taskYamlPath is the absolute path to task.yaml (provided by caller).
/// planMdPath is derived from taskYamlPath directory if plan.md exists.
/// allTasks is used to find sibling tasks sharing the same SourceBacklog.
let getTaskDetail
    (taskId: TaskId)
    (allTasks: ItrTask list)
    (taskYamlPath: string)
    : Result<TaskDetail, TaskError> =

    match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
    | None -> Error(TaskNotFound taskId)
    | Some task ->
        let planApproved =
            match task.State with
            | TaskState.Approved
            | TaskState.InProgress
            | TaskState.Implemented
            | TaskState.Validated
            | TaskState.Archived -> true
            | _ -> false

        let planMdPath =
            let dir = Path.GetDirectoryName(taskYamlPath)
            if isNull dir || dir = "" then None
            else
                let candidate = Path.Combine(dir, "plan.md")
                if File.Exists(candidate) then Some candidate else None

        let planExists = planMdPath.IsSome

        let siblings =
            allTasks
            |> List.filter (fun t -> t.Id <> taskId && t.SourceBacklog = task.SourceBacklog)
            |> List.map (fun t -> { Id = t.Id; Repo = t.Repo; State = t.State })

        Ok
            { Task = task
              PlanExists = planExists
              PlanApproved = planApproved
              Siblings = siblings
              TaskYamlPath = taskYamlPath
              PlanMdPath = planMdPath }

// ---------------------------------------------------------------------------
// planTask: pure function to validate state and return updated task
// ---------------------------------------------------------------------------

/// Validate that a task can be planned and return the task updated to Planned state.
/// Returns (updatedTask, wasAlreadyPlanned) on success.
/// Allowed states: Planning → Planned; Planned → Planned (re-plan).
/// States beyond Planned return InvalidTaskState error.
let planTask
    (task: ItrTask)
    : Result<ItrTask * bool, TaskError> =
    match task.State with
    | TaskState.Planning ->
        Ok ({ task with State = TaskState.Planned }, false)
    | TaskState.Planned ->
        Ok (task, true)
    | other ->
        Error (InvalidTaskState (task.Id, other))

// ---------------------------------------------------------------------------
// approveTask: pure function to validate state and return updated task
// ---------------------------------------------------------------------------

/// Validate that a task can be approved and return the task updated to Approved state.
/// Returns (updatedTask, wasAlreadyApproved) on success.
/// Requires planExists = true; Planned → Approved; Approved → Approved (idempotent).
/// Other states return InvalidTaskState error.
let approveTask
    (task: ItrTask)
    (planExists: bool)
    : Result<ItrTask * bool, TaskError> =
    match task.State with
    | TaskState.Approved ->
        Ok (task, true)
    | TaskState.Planned ->
        if planExists then
            Ok ({ task with State = TaskState.Approved }, false)
        else
            Error (MissingPlanArtifact task.Id)
    | other ->
        Error (InvalidTaskState (task.Id, other))
