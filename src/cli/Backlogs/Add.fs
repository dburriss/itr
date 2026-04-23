module Itr.Cli.Backlogs.Add

open System
open Argu
open Itr.Domain
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting
open Itr.Cli.InteractivePrompts

let private toProductConfig (def: ProductDefinition) : ProductConfig =
    let repos =
        def.Repos |> Map.toSeq |> Seq.map (fun (k, v) -> RepoId k, v) |> Map.ofSeq

    { Id = def.Id; Repos = repos }

let handle
    (deps: #IBacklogStore)
    (resolved: ResolvedProduct)
    (addArgs: ParseResults<AddArgs>)
    (format: OutputFormat)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let isInteractive = addArgs.Contains AddArgs.Interactive
    let rawBacklogIdOpt = addArgs.TryGetResult AddArgs.Backlog_Id
    let titleOpt = addArgs.TryGetResult AddArgs.Title
    let repo = addArgs.TryGetResult AddArgs.Repo
    let itemType = addArgs.TryGetResult AddArgs.Item_Type
    let summary = addArgs.TryGetResult AddArgs.Summary
    let priority = addArgs.TryGetResult AddArgs.Priority
    let dependsOn = addArgs.GetResults AddArgs.Depends_On

    if not isInteractive then
        match rawBacklogIdOpt with
        | None -> Error "--backlog-id is required when not using --interactive"
        | Some _ ->
            match titleOpt with
            | None -> Error "--title is required when not using --interactive"
            | Some _ ->

                let rawBacklogId = rawBacklogIdOpt.Value
                let title = titleOpt.Value
                let productConfig = toProductConfig resolved.Definition
                let backlogStore = deps :> IBacklogStore

                match BacklogId.tryCreate rawBacklogId with
                | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
                | Ok backlogId ->
                    if backlogStore.BacklogItemExists coordRoot backlogId then
                        Error(formatBacklogError (DuplicateBacklogId backlogId))
                    else
                        let input: Backlogs.Create.Input =
                            { BacklogId = rawBacklogId
                              Title = title
                              Repos = repo |> Option.toList
                              ItemType = itemType
                              Priority = priority
                              Summary = summary
                              AcceptanceCriteria = []
                              DependsOn = dependsOn }

                        let today = DateOnly.FromDateTime(DateTime.UtcNow)

                        match Backlogs.Create.execute productConfig input today with
                        | Error e -> Error(formatBacklogError e)
                        | Ok item ->
                            match backlogStore.WriteBacklogItem coordRoot item with
                            | Error e -> Error(formatBacklogError e)
                            | Ok() ->
                                let id = BacklogId.value item.Id
                                let path = BacklogItem.itemFile coordRoot item.Id

                                match format with
                                | Json -> printfn """{ "ok": true, "id": "%s" }""" id
                                | _ -> printfn "Created backlog item '%s' → %s" id path

                                Ok()
    else
        let backlogStore = deps :> IBacklogStore
        let productConfig = toProductConfig resolved.Definition

        let prefilled: PrefilledArgs =
            { BacklogId = rawBacklogIdOpt
              Title = titleOpt
              Repo = repo
              ItemType = itemType
              Priority = priority
              Summary = summary
              DependsOn = dependsOn }

        match promptBacklogAdd backlogStore coordRoot productConfig prefilled with
        | Error msg ->
            eprintfn "Error: %s" msg
            Error msg
        | Ok input ->
            match BacklogId.tryCreate input.BacklogId with
            | Error _ -> Error $"Invalid backlog id '{input.BacklogId}': must match [a-z0-9][a-z0-9-]*"
            | Ok backlogId ->
                if backlogStore.BacklogItemExists coordRoot backlogId then
                    Error(formatBacklogError (DuplicateBacklogId backlogId))
                else
                    let today = DateOnly.FromDateTime(DateTime.UtcNow)

                    match Backlogs.Create.execute productConfig input today with
                    | Error e -> Error(formatBacklogError e)
                    | Ok item ->
                        match backlogStore.WriteBacklogItem coordRoot item with
                        | Error e -> Error(formatBacklogError e)
                        | Ok() ->
                            let id = BacklogId.value item.Id
                            let path = BacklogItem.itemFile coordRoot item.Id

                            match format with
                            | Json -> printfn """{ "ok": true, "id": "%s" }""" id
                            | _ -> printfn "Created backlog item '%s' → %s" id path

                            Ok()
