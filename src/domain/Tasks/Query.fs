module Itr.Domain.Tasks.Query

open System.IO
open Itr.Domain

type TaskSummary =
    { Task: ItrTask
      PlanApproved: bool
      TaskYamlPath: string
      PlanMdPath: string option }

type SiblingTask =
    { Id: TaskId
      Repo: RepoId
      State: TaskState }

type TaskDetail =
    { Task: ItrTask
      PlanExists: bool
      PlanApproved: bool
      Siblings: SiblingTask list
      TaskYamlPath: string
      PlanMdPath: string option }

/// Wrap raw tasks (with their yaml paths) into TaskSummary values.
let list (tasks: (ItrTask * string) list) : TaskSummary list =
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

            if isNull dir || dir = "" then
                None
            else
                let candidate = Path.Combine(dir, "plan.md")
                if File.Exists(candidate) then Some candidate else None

        { Task = task
          PlanApproved = planApproved
          TaskYamlPath = taskYamlPath
          PlanMdPath = planMdPath })

type FilterInput =
    { BacklogId: BacklogId option
      Repo: RepoId option
      State: TaskState option
      Exclude: TaskState list }

/// Filter task summaries by optional backlog id, repo id, state, and an exclude list.
let filter (input: FilterInput) (summaries: TaskSummary list) : TaskSummary list =
    summaries
    |> List.filter (fun s ->
        let matchesBacklog =
            match input.BacklogId with
            | None -> true
            | Some bid -> s.Task.SourceBacklog = bid

        let matchesRepo =
            match input.Repo with
            | None -> true
            | Some rid -> s.Task.Repo = rid

        let matchesState =
            match input.State with
            | None -> true
            | Some st -> s.Task.State = st

        let notExcluded = not (List.contains s.Task.State input.Exclude)
        matchesBacklog && matchesRepo && matchesState && notExcluded)

type DetailInput =
    { TaskId: TaskId
      AllTasks: ItrTask list
      TaskYamlPath: string }

/// Return the full detail record for the task with the given id.
let getDetail (input: DetailInput) : Result<TaskDetail, TaskError> =
    match input.AllTasks |> List.tryFind (fun t -> t.Id = input.TaskId) with
    | None -> Error(TaskNotFound input.TaskId)
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
            let dir = Path.GetDirectoryName(input.TaskYamlPath)

            if isNull dir || dir = "" then
                None
            else
                let candidate = Path.Combine(dir, "plan.md")
                if File.Exists(candidate) then Some candidate else None

        let planExists = planMdPath.IsSome

        let siblings =
            input.AllTasks
            |> List.filter (fun t -> t.Id <> input.TaskId && t.SourceBacklog = task.SourceBacklog)
            |> List.map (fun t ->
                { Id = t.Id
                  Repo = t.Repo
                  State = t.State })

        Ok
            { Task = task
              PlanExists = planExists
              PlanApproved = planApproved
              Siblings = siblings
              TaskYamlPath = input.TaskYamlPath
              PlanMdPath = planMdPath }
