module Itr.Cli.Backlogs.Take

open System
open Argu
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let private toProductConfig (def: ProductDefinition) : ProductConfig =
    let repos =
        def.Repos |> Map.toSeq |> Seq.map (fun (k, v) -> RepoId k, v) |> Map.ofSeq

    { Id = def.Id; Repos = repos }

let handle
    (deps: #IBacklogStore & #ITaskStore)
    (resolved: ResolvedProduct)
    (takeArgs: ParseResults<TakeArgs>)
    (format: OutputFormat)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawBacklogId = takeArgs.GetResult TakeArgs.Backlog_Id
    let taskIdOverride = takeArgs.TryGetResult TakeArgs.Task_Id

    let backlogIdResult = BacklogId.tryCreate rawBacklogId

    match backlogIdResult with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->

        let productConfig = toProductConfig resolved.Definition
        let backlogStore = deps :> IBacklogStore
        let taskStore = deps :> ITaskStore

        let result =
            backlogStore.LoadBacklogItem coordRoot backlogId
            |> Result.mapError formatBacklogError
            |> Result.bind (fun (backlogItem, _) ->
                taskStore.ListTasks coordRoot backlogId
                |> Result.mapError formatTaskError
                |> Result.map (List.map fst)
                |> Result.bind (fun existingTasks ->
                    let input: Tasks.Take.Input =
                        { Tasks.Take.Input.BacklogId = backlogId
                          Tasks.Take.Input.TaskIdOverride = taskIdOverride }

                    let today = DateOnly.FromDateTime(DateTime.UtcNow)

                    Tasks.Take.execute productConfig backlogItem existingTasks input today
                    |> Result.mapError formatTaskError
                    |> Result.bind (fun newTasks ->
                        let writeResults =
                            newTasks
                            |> List.map (fun task ->
                                taskStore.WriteTask coordRoot task
                                |> Result.map (fun () ->
                                    let taskId = TaskId.value task.Id
                                    let path = ItrTask.taskFile coordRoot task.SourceBacklog task.Id
                                    (taskId, path))
                                |> Result.mapError formatTaskError)

                        let errors =
                            writeResults
                            |> List.choose (function
                                | Error e -> Some e
                                | Ok _ -> None)

                        match errors with
                        | e :: _ -> Error e
                        | [] ->
                            let written =
                                writeResults
                                |> List.choose (function
                                    | Ok v -> Some v
                                    | Error _ -> None)

                            Ok written)))

        match result with
        | Error msg -> Error msg
        | Ok written ->
            match format with
            | Json ->
                let items =
                    written
                    |> List.map (fun (id, path) -> $"""  {{ "id": "{id}", "path": "{path}" }}""")
                    |> String.concat ",\n"

                printfn """{ "ok": true, "tasks": ["""
                printfn "%s" items
                printfn "] }"
            | _ -> written |> List.iter (fun (id, path) -> printfn "Created task: %s → %s" id path)

            Ok()
