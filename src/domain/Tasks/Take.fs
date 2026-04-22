module Itr.Domain.Tasks.Take

open Itr.Domain

type Input =
    { BacklogId: BacklogId
      TaskIdOverride: string option }

/// Generate a unique task id that does not collide with any existing id.
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

/// Pure pipeline: does not perform I/O. Returns the list of ItrTask values
/// for the caller to write.
let execute
    (productConfig: ProductConfig)
    (backlogItem: BacklogItem)
    (existingTasks: ItrTask list)
    (input: Input)
    (today: System.DateOnly)
    : Result<ItrTask list, TaskError> =

    let invalidRepo =
        backlogItem.Repos
        |> List.tryFind (fun repoId -> not (Map.containsKey repoId productConfig.Repos))

    match invalidRepo with
    | Some repoId -> Error(TaskStoreError("", $"Repo '{RepoId.value repoId}' is not listed in product.yaml"))
    | None ->

        match input.TaskIdOverride with
        | Some overrideId when backlogItem.Repos.Length > 1 -> Error TaskIdOverrideRequiresSingleRepo

        | Some overrideId ->
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
            let existingIdSet =
                existingTasks |> List.map (fun t -> TaskId.value t.Id) |> Set.ofList

            let isSingleRepo = backlogItem.Repos.Length = 1
            let isFirstTake = existingTasks.IsEmpty

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
