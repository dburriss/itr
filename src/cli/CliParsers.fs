module Itr.Cli.CliParsers

open Itr.Domain
open Itr.Domain.Tasks

let backlogItemStatusToString (status: BacklogItemStatus) : string =
    match status with
    | BacklogItemStatus.Created -> "created"
    | BacklogItemStatus.Planning -> "planning"
    | BacklogItemStatus.Planned -> "planned"
    | BacklogItemStatus.Approved -> "approved"
    | BacklogItemStatus.InProgress -> "in-progress"
    | BacklogItemStatus.Completed -> "completed"
    | BacklogItemStatus.Archived -> "archived"

let tryParseBacklogItemStatus (s: string) : BacklogItemStatus option =
    match s with
    | "created" -> Some BacklogItemStatus.Created
    | "planning" -> Some BacklogItemStatus.Planning
    | "planned" -> Some BacklogItemStatus.Planned
    | "approved" -> Some BacklogItemStatus.Approved
    | "in-progress"
    | "inprogress" -> Some BacklogItemStatus.InProgress
    | "completed" -> Some BacklogItemStatus.Completed
    | "archived" -> Some BacklogItemStatus.Archived
    | _ -> None

let tryParseTaskState (s: string) : Result<TaskState, string> =
    match s with
    | "planning" -> Ok TaskState.Planning
    | "planned" -> Ok TaskState.Planned
    | "approved" -> Ok TaskState.Approved
    | "in_progress"
    | "in-progress" -> Ok TaskState.InProgress
    | "implemented" -> Ok TaskState.Implemented
    | "validated" -> Ok TaskState.Validated
    | "archived" -> Ok TaskState.Archived
    | other ->
        Error
            $"Unknown task state '{other}': must be planning | planned | approved | in_progress | implemented | validated | archived"

let taskStateToDisplayString (state: TaskState) : string =
    match state with
    | TaskState.Planning -> "planning"
    | TaskState.Planned -> "planned"
    | TaskState.Approved -> "approved"
    | TaskState.InProgress -> "in_progress"
    | TaskState.Implemented -> "implemented"
    | TaskState.Validated -> "validated"
    | TaskState.Archived -> "archived"
