module Itr.Domain.Tasks.Approve

open Itr.Domain

type Input = { Task: ItrTask; PlanExists: bool }

/// Validate that a task can be approved and return the task updated to Approved state.
/// Returns (updatedTask, wasAlreadyApproved) on success.
let execute (input: Input) : Result<ItrTask * bool, TaskError> =
    match input.Task.State with
    | TaskState.Approved -> Ok(input.Task, true)
    | TaskState.Planned ->
        if input.PlanExists then
            Ok(
                { input.Task with
                    State = TaskState.Approved },
                false
            )
        else
            Error(MissingPlanArtifact input.Task.Id)
    | other -> Error(InvalidTaskState(input.Task.Id, other))
