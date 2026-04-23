module Itr.Cli.Tasks.Info

open Argu
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (deps: #ITaskStore)
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
