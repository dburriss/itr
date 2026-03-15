module Itr.Features.Task

open Itr.Domain

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

    let baseId =
        if isSingleRepo && isFirstTake then bid else $"{rid}-{bid}"

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
let takeBacklogItem
    (productConfig: ProductConfig)
    (backlogItem: BacklogItem)
    (existingTasks: ItrTask list)
    (input: TakeInput)
    (today: System.DateOnly)
    : Result<ItrTask list, TakeError> =

    // 1. Validate all repos on the item exist in product.yaml
    let invalidRepo =
        backlogItem.Repos
        |> List.tryFind (fun repoId -> not (Map.containsKey repoId productConfig.Repos))

    match invalidRepo with
    | Some repoId -> Error(RepoNotInProduct repoId)
    | None ->

    // 2. Handle --task-id override
    match input.TaskIdOverride with
    | Some overrideId when backlogItem.Repos.Length > 1 ->
        Error TaskIdOverrideRequiresSingleRepo

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
                        State = Planning
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
                          State = Planning
                          CreatedAt = today }

                    (task :: acc, Set.add (TaskId.value taskId) allocatedIds))
                ([], Set.empty)

        Ok(List.rev tasks)
