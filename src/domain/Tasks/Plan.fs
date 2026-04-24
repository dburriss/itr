module Itr.Domain.Tasks.Plan

open Itr.Domain

/// Validate that a task can be planned and return the task updated to Planned state.
/// Returns (updatedTask, wasAlreadyPlanned) on success.
let execute (task: ItrTask) : Result<ItrTask * bool, TaskError> =
    match task.State with
    | TaskState.Planning -> Ok({ task with State = TaskState.Planned }, false)
    | TaskState.Planned -> Ok(task, true)
    | other -> Error(InvalidTaskState(task.Id, other))
