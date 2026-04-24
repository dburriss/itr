module Itr.Domain.Backlogs.Query

open Itr.Domain

type BacklogListFilter =
    { ViewId: string option
      Status: BacklogItemStatus option
      ItemType: BacklogItemType option
      ExcludeStatuses: BacklogItemStatus list
      OrderBy: string option }

/// Map a TaskError to a BacklogError for use in backlog-level pipelines.
let private taskErrorToBacklogError (e: TaskError) : BacklogError =
    match e with
    | TaskStoreError(path, msg) -> ProductConfigParseError(path, msg)
    | _ -> ProductConfigParseError("", $"%A{e}")

/// Load all backlog items, views, and tasks into a BacklogSnapshot.
let loadSnapshot
    (backlogStore: IBacklogStore)
    (taskStore: ITaskStore)
    (viewStore: IViewStore)
    (coordRoot: string)
    : Result<BacklogSnapshot, BacklogError> =

    match backlogStore.ListBacklogItems coordRoot with
    | Error e -> Error e
    | Ok activeItemTuples ->

        match backlogStore.ListArchivedBacklogItems coordRoot with
        | Error e -> Error e
        | Ok archivedItemTuples ->

            match viewStore.ListViews coordRoot with
            | Error e -> Error e
            | Ok views ->

                let itemViewMap =
                    views
                    |> List.fold
                        (fun (acc: Map<string, string>) view ->
                            view.Items
                            |> List.fold
                                (fun innerAcc itemId ->
                                    if Map.containsKey itemId innerAcc then
                                        eprintfn
                                            "Warning: item '%s' appears in multiple views; keeping first assignment"
                                            itemId

                                        innerAcc
                                    else
                                        Map.add itemId view.Id innerAcc)
                                acc)
                        Map.empty

                let activeSummaryResults =
                    activeItemTuples
                    |> List.map (fun (item, path) ->
                        let id = BacklogId.value item.Id

                        match taskStore.ListTasks coordRoot item.Id with
                        | Error e -> Error(taskErrorToBacklogError e)
                        | Ok taskTuples ->
                            let tasks = taskTuples |> List.map fst
                            let status = BacklogItemStatus.compute tasks

                            Ok
                                { Item = item
                                  Status = status
                                  ViewId = Map.tryFind id itemViewMap
                                  TaskCount = tasks.Length
                                  Path = path })

                let archivedSummaryResults =
                    archivedItemTuples
                    |> List.map (fun (item, path) ->
                        let id = BacklogId.value item.Id

                        match taskStore.ListArchivedTasks coordRoot item.Id with
                        | Error e -> Error(taskErrorToBacklogError e)
                        | Ok tasks ->
                            let status = BacklogItemStatus.compute tasks

                            Ok
                                { Item = item
                                  Status = status
                                  ViewId = Map.tryFind id itemViewMap
                                  TaskCount = tasks.Length
                                  Path = path })

                let allResults = activeSummaryResults @ archivedSummaryResults

                let errors =
                    allResults
                    |> List.choose (function
                        | Error e -> Some e
                        | Ok _ -> None)

                match errors with
                | e :: _ -> Error e
                | [] ->
                    let summaries =
                        allResults
                        |> List.choose (function
                            | Ok s -> Some s
                            | Error _ -> None)

                    Ok { Items = summaries; Views = views }

/// Priority order: high=0, medium=1, low=2, other/None=3 (case-insensitive)
let private priorityOrder (priority: string option) : int =
    match priority with
    | None -> 3
    | Some p ->
        match p.ToLowerInvariant() with
        | "high" -> 0
        | "medium" -> 1
        | "low" -> 2
        | _ -> 3

/// Type order: Bug=0, Feature=1, Chore=2, Refactor=3, Spike=4
let private typeOrder (t: BacklogItemType) : int =
    match t with
    | Bug -> 0
    | Feature -> 1
    | Chore -> 2
    | Refactor -> 3
    | Spike -> 4

/// Default multi-key sort: type → priority → CreatedAt ascending
let private defaultSort (items: BacklogItemSummary list) : BacklogItemSummary list =
    items
    |> List.sortWith (fun a b ->
        let tc = compare (typeOrder a.Item.Type) (typeOrder b.Item.Type)

        if tc <> 0 then
            tc
        else
            let pc = compare (priorityOrder a.Item.Priority) (priorityOrder b.Item.Priority)

            if pc <> 0 then
                pc
            else
                compare a.Item.CreatedAt b.Item.CreatedAt)

/// Pure filter + ordering: returns items from the snapshot matching the given filter.
let list (filter: BacklogListFilter) (snapshot: BacklogSnapshot) : BacklogItemSummary list =
    let filtered =
        snapshot.Items
        |> List.filter (fun s ->
            let viewMatch =
                match filter.ViewId with
                | None -> true
                | Some viewId -> s.ViewId = Some viewId

            let statusMatch =
                match filter.Status with
                | None -> true
                | Some status -> s.Status = status

            let typeMatch =
                match filter.ItemType with
                | None -> true
                | Some itemType -> s.Item.Type = itemType

            let excludeMatch = not (List.contains s.Status filter.ExcludeStatuses)

            viewMatch && statusMatch && typeMatch && excludeMatch)

    match filter.OrderBy with
    | Some "created" -> filtered |> List.sortBy (fun s -> s.Item.CreatedAt)
    | Some "priority" -> filtered |> List.sortBy (fun s -> priorityOrder s.Item.Priority)
    | Some "type" -> filtered |> List.sortBy (fun s -> typeOrder s.Item.Type)
    | _ ->
        match filter.ViewId with
        | Some viewId ->
            match snapshot.Views |> List.tryFind (fun v -> v.Id = viewId) with
            | None -> defaultSort filtered
            | Some view ->
                let viewItemIndex = view.Items |> List.mapi (fun i id -> id, i) |> Map.ofList

                let inView, notInView =
                    filtered
                    |> List.partition (fun s -> Map.containsKey (BacklogId.value s.Item.Id) viewItemIndex)

                let sortedInView =
                    inView |> List.sortBy (fun s -> viewItemIndex.[BacklogId.value s.Item.Id])

                let sortedNotInView = defaultSort notInView
                sortedInView @ sortedNotInView
        | None -> defaultSort filtered

/// Load a single backlog item with full detail: tasks, computed status, and view membership.
let getDetail
    (backlogStore: IBacklogStore)
    (taskStore: ITaskStore)
    (viewStore: IViewStore)
    (coordRoot: string)
    (backlogId: BacklogId)
    : Result<BacklogItemDetail, BacklogError> =

    let itemResult =
        match backlogStore.LoadBacklogItem coordRoot backlogId with
        | Ok(item, path) -> Ok(item, path, false)
        | Error(BacklogItemNotFound _) ->
            match backlogStore.LoadArchivedBacklogItem coordRoot backlogId with
            | Error e -> Error e
            | Ok None -> Error(BacklogItemNotFound backlogId)
            | Ok(Some(item, path)) -> Ok(item, path, true)
        | Error e -> Error e

    match itemResult with
    | Error e -> Error e
    | Ok(item, itemPath, isArchived) ->

        let tasksResult =
            if isArchived then
                taskStore.ListArchivedTasks coordRoot backlogId
            else
                taskStore.ListTasks coordRoot backlogId |> Result.map (List.map fst)

        match tasksResult with
        | Error e -> Error(taskErrorToBacklogError e)
        | Ok tasks ->

            match viewStore.ListViews coordRoot with
            | Error e -> Error e
            | Ok views ->

                let idStr = BacklogId.value backlogId

                let viewId =
                    views
                    |> List.tryPick (fun view ->
                        if List.contains idStr view.Items then
                            Some view.Id
                        else
                            None)

                let status = BacklogItemStatus.compute tasks

                Ok
                    { Item = item
                      Status = status
                      ViewId = viewId
                      Tasks = tasks
                      Path = itemPath }
