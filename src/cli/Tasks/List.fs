module Itr.Cli.Tasks.List

open Argu
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.CliParsers
open Itr.Cli.ErrorFormatting

let handle
    (deps: #ITaskStore)
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

    let repoId = listArgs.TryGetResult TaskListArgs.Repo_Id |> Option.map RepoId

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

            let filterInput: Tasks.Query.FilterInput =
                { BacklogId = backlogIdFilter
                  Repo = repoId
                  State = stateFilter
                  Exclude = excludeList }

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

            let summaryRows: TaskListSummary list =
                ordered
                |> List.map (fun s ->
                    { Task = s.Task
                      TaskYamlPath = s.TaskYamlPath
                      PlanMdPath = s.PlanMdPath })

            TaskFormatter.formatList format summaryRows
            Ok()
