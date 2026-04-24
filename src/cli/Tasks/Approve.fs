module Itr.Cli.Tasks.Approve

open Argu
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (deps: #ITaskStore & #IFileSystem)
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
            let planPath =
                ItrTask.planFile coordRoot task.SourceBacklog (TaskId.create rawTaskId)

            let planExists = fileSystem.FileExists planPath

            let approveInput: Tasks.Approve.Input = { Task = task; PlanExists = planExists }

            match Tasks.Approve.execute approveInput with
            | Error e -> Error(formatTaskError e)
            | Ok(updatedTask, wasAlreadyApproved) ->
                if wasAlreadyApproved then
                    printfn "Task '%s' is already approved." rawTaskId
                    Ok()
                else
                    match taskStore.WriteTask coordRoot updatedTask with
                    | Error e -> Error(formatTaskError e)
                    | Ok() ->
                        printfn "Task '%s' approved." rawTaskId
                        Ok()
