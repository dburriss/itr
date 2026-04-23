module Itr.Cli.Tasks.Plan

open System
open Argu
open Fue.Data
open Fue.Compiler
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (deps: #ITaskStore & #IBacklogStore & #IFileSystem & #IAgentHarness)
    (resolved: ResolvedProduct)
    (planArgs: ParseResults<TaskPlanArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawTaskId = planArgs.GetResult TaskPlanArgs.Task_Id
    let useAi = planArgs.Contains TaskPlanArgs.Ai
    let debug = planArgs.Contains TaskPlanArgs.Debug

    let taskId = TaskId.create rawTaskId
    let taskStore = deps :> ITaskStore
    let backlogStore = deps :> IBacklogStore
    let fileSystem = deps :> IFileSystem

    match taskStore.ListAllTasks coordRoot with
    | Error e -> Error(formatTaskError e)
    | Ok allTaskTuples ->
        let allTasks = allTaskTuples |> List.map fst

        match allTasks |> List.tryFind (fun t -> t.Id = taskId) with
        | None -> Error(formatTaskError (TaskNotFound taskId))
        | Some task ->
            match Tasks.Plan.execute task with
            | Error e -> Error(formatTaskError e)
            | Ok(updatedTask, wasAlreadyPlanned) ->
                if wasAlreadyPlanned then
                    printfn "Re-planning task %s (was already planned)." rawTaskId

                let backlogId = updatedTask.SourceBacklog

                match backlogStore.LoadBacklogItem coordRoot backlogId with
                | Error e -> Error(formatBacklogError e)
                | Ok(backlogItem, _) ->
                    let repo = RepoId.value updatedTask.Repo
                    let backlogIdStr = BacklogId.value backlogId
                    let title = backlogItem.Title
                    let summary = backlogItem.Summary |> Option.defaultValue ""
                    let deps_ = backlogItem.Dependencies |> List.map BacklogId.value
                    let ac = backlogItem.AcceptanceCriteria

                    let dependenciesStr =
                        if deps_.IsEmpty then
                            "- none"
                        else
                            deps_ |> List.map (fun d -> $"- {d}") |> String.concat "\n"

                    let acceptanceCriteriaStr =
                        if ac.IsEmpty then
                            "- none"
                        else
                            ac |> List.map (fun c -> $"- {c}") |> String.concat "\n"

                    let exeDir = AppDomain.CurrentDomain.BaseDirectory
                    let templatePath = IO.Path.Combine(exeDir, "assets", "plan-template.md")
                    let promptPath = IO.Path.Combine(exeDir, "assets", "plan-prompt.md")

                    let skeletonResult =
                        match fileSystem.ReadFile templatePath with
                        | Error _ -> Error $"Could not read plan-template.md from {templatePath}"
                        | Ok template ->
                            let rendered =
                                init
                                |> add "title" title
                                |> add "taskId" rawTaskId
                                |> add "backlogId" backlogIdStr
                                |> add "repo" repo
                                |> add "summary" summary
                                |> add "dependencies" dependenciesStr
                                |> add "acceptanceCriteria" acceptanceCriteriaStr
                                |> fromNoneHtmlText template

                            Ok rendered

                    let planContentResult =
                        match skeletonResult with
                        | Error e -> Error e
                        | Ok skeleton ->
                            if useAi then
                                let globalAgentConfig = resolved.Profile.AgentConfig

                                let (ProductRoot productRoot) = resolved.Product.Root

                                let agentConfig =
                                    match PortfolioAdapter.LoadLocalConfig productRoot with
                                    | Some localConfig -> localConfig
                                    | None -> globalAgentConfig

                                let harness =
                                    AgentHarnessSelector.selectHarness
                                        agentConfig.Protocol
                                        agentConfig.Command
                                        agentConfig.Args
                                        coordRoot

                                match fileSystem.ReadFile promptPath with
                                | Error _ -> Error $"Could not read plan-prompt.md from {promptPath}"
                                | Ok promptTemplate ->
                                    let renderedPrompt =
                                        init |> add "planSkeleton" skeleton |> fromNoneHtmlText promptTemplate

                                    harness.Prompt renderedPrompt debug |> Result.mapError (fun e -> e)
                            else
                                Ok skeleton

                    match planContentResult with
                    | Error msg -> Error msg
                    | Ok rawPlanContent ->
                        let planContent = Itr.Adapters.AcpMessages.trimPreamble rawPlanContent
                        let planPath = ItrTask.planFile coordRoot backlogId (TaskId.create rawTaskId)

                        match fileSystem.WriteFile planPath planContent with
                        | Error e -> Error $"Failed to write plan.md: %A{e}"
                        | Ok() ->
                            match taskStore.WriteTask coordRoot updatedTask with
                            | Error e -> Error(formatTaskError e)
                            | Ok() ->
                                printfn "Plan written: %s" planPath
                                Ok()
