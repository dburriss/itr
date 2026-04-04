module Itr.Features.Backlog

open System
open Itr.Domain

// ---------------------------------------------------------------------------
// Input type for the use case
// ---------------------------------------------------------------------------

type CreateBacklogItemInput =
    { BacklogId: string
      Title: string
      Repos: string list
      ItemType: string option
      Priority: string option
      Summary: string option
      AcceptanceCriteria: string list
      DependsOn: string list }

// ---------------------------------------------------------------------------
// Pure createBacklogItem function
// ---------------------------------------------------------------------------

/// Pure pipeline: validates inputs and builds a BacklogItem.
/// Does not perform any I/O.
let createBacklogItem
    (productConfig: ProductConfig)
    (input: CreateBacklogItemInput)
    (today: System.DateOnly)
    : Result<BacklogItem, BacklogError> =

    // 1. Validate backlog id
    match BacklogId.tryCreate input.BacklogId with
    | Error e -> Error e
    | Ok backlogId ->

    // 2. Validate title
    if System.String.IsNullOrWhiteSpace(input.Title) then
        Error MissingTitle
    else

    // 3. Validate item type
    let itemTypeStr = input.ItemType |> Option.defaultValue "feature"
    match BacklogItemType.tryParse itemTypeStr with
    | Error e -> Error e
    | Ok itemType ->

    // 4. Resolve repos
    let resolvedReposResult =
        match input.Repos with
        | [] ->
            // Auto-resolve if single repo
            if productConfig.Repos.Count = 1 then
                productConfig.Repos |> Map.toList |> List.map fst |> Ok
            else
                Error(RepoNotInProduct(RepoId ""))
        | repos -> Ok (repos |> List.map RepoId)

    match resolvedReposResult with
    | Error e -> Error e
    | Ok repoIds ->

    // 5. Validate all repos exist in product config
    let unknownRepo =
        repoIds |> List.tryFind (fun repoId -> not (Map.containsKey repoId productConfig.Repos))

    match unknownRepo with
    | Some repoId -> Error(RepoNotInProduct repoId)
    | None ->

    // 6. Validate dependency ids (format only)
    let depResults =
        input.DependsOn |> List.map BacklogId.tryCreate

    let depErrors = depResults |> List.choose (function Error e -> Some e | Ok _ -> None)
    match depErrors with
    | e :: _ -> Error e
    | [] ->

    let deps = depResults |> List.choose (function Ok id -> Some id | Error _ -> None)

    Ok
        { Id = backlogId
          Title = input.Title
          Repos = repoIds
          Type = itemType
          Priority = input.Priority
          Summary = input.Summary
          AcceptanceCriteria = input.AcceptanceCriteria
          Dependencies = deps
          CreatedAt = today }

// ---------------------------------------------------------------------------
// BacklogListFilter
// ---------------------------------------------------------------------------

type BacklogListFilter =
    { ViewId: string option
      Status: BacklogItemStatus option
      ItemType: BacklogItemType option }

// ---------------------------------------------------------------------------
// loadSnapshot
// ---------------------------------------------------------------------------

/// Load all backlog items, views, and tasks into a BacklogSnapshot.
/// Items are sorted by CreatedAt ascending.
/// Multi-view membership is resolved first-match (alphabetical filename) wins;
/// a warning is emitted to stderr for duplicates.
let loadSnapshot
    (backlogStore: IBacklogStore)
    (taskStore: ITaskStore)
    (viewStore: IViewStore)
    (coordRoot: string)
    : Result<BacklogSnapshot, BacklogError> =

    // 1. Load active items
    match backlogStore.ListBacklogItems coordRoot with
    | Error e -> Error e
    | Ok activeItemTuples ->

    // 2. Load archived items
    match backlogStore.ListArchivedBacklogItems coordRoot with
    | Error e -> Error e
    | Ok archivedItemTuples ->

    // 3. Load views
    match viewStore.ListViews coordRoot with
    | Error e -> Error e
    | Ok views ->

    // 4. Build item → viewId map (first-match wins; warn on duplicates)
    let itemViewMap =
        views
        |> List.fold (fun (acc: Map<string, string>) view ->
            view.Items
            |> List.fold (fun innerAcc itemId ->
                if Map.containsKey itemId innerAcc then
                    eprintfn "Warning: item '%s' appears in multiple views; keeping first assignment" itemId
                    innerAcc
                else
                    Map.add itemId view.Id innerAcc
            ) acc
        ) Map.empty

    // 5. Build summaries for active items
    let activeSummaryResults =
        activeItemTuples
        |> List.map (fun (item, path) ->
            let id = BacklogId.value item.Id
            match taskStore.ListTasks coordRoot item.Id with
            | Error e -> Error e
            | Ok tasks ->
                let status = BacklogItemStatus.compute tasks
                Ok
                    { Item = item
                      Status = status
                      ViewId = Map.tryFind id itemViewMap
                      TaskCount = tasks.Length
                      Path = path })

    // 6. Build summaries for archived items
    let archivedSummaryResults =
        archivedItemTuples
        |> List.map (fun (item, path) ->
            let id = BacklogId.value item.Id
            match taskStore.ListArchivedTasks coordRoot item.Id with
            | Error e -> Error e
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
            |> List.sortBy (fun s -> s.Item.CreatedAt)

        Ok { Items = summaries }

// ---------------------------------------------------------------------------
// listBacklogItems (pure filter over snapshot)
// ---------------------------------------------------------------------------

/// Pure filter: returns items from the snapshot matching the given filter.
let listBacklogItems (filter: BacklogListFilter) (snapshot: BacklogSnapshot) : BacklogItemSummary list =
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

        viewMatch && statusMatch && typeMatch)

// ---------------------------------------------------------------------------
// getBacklogItemDetail
// ---------------------------------------------------------------------------

/// Load a single backlog item with full detail: tasks, computed status, and view membership.
/// Checks active items first; falls back to archived items if not found.
let getBacklogItemDetail
    (backlogStore: IBacklogStore)
    (taskStore: ITaskStore)
    (viewStore: IViewStore)
    (coordRoot: string)
    (backlogId: BacklogId)
    : Result<BacklogItemDetail, BacklogError> =

    // 1. Try loading the active item; on not-found, check archive
    let itemResult =
        match backlogStore.LoadBacklogItem coordRoot backlogId with
        | Ok (item, path) -> Ok(item, path, false)
        | Error(BacklogItemNotFound _) ->
            match backlogStore.LoadArchivedBacklogItem coordRoot backlogId with
            | Error e -> Error e
            | Ok None -> Error(BacklogItemNotFound backlogId)
            | Ok(Some (item, path)) -> Ok(item, path, true)
        | Error e -> Error e

    match itemResult with
    | Error e -> Error e
    | Ok(item, itemPath, isArchived) ->

    // 2. Load tasks — archived items have tasks under the archive folder
    let tasksResult =
        if isArchived then
            taskStore.ListArchivedTasks coordRoot backlogId
        else
            taskStore.ListTasks coordRoot backlogId

    match tasksResult with
    | Error e -> Error e
    | Ok tasks ->

    // 3. Load views to find view membership
    match viewStore.ListViews coordRoot with
    | Error e -> Error e
    | Ok views ->

    // 4. Find the first view that contains this item
    let idStr = BacklogId.value backlogId
    let viewId =
        views
        |> List.tryPick (fun view ->
            if List.contains idStr view.Items then Some view.Id else None)

    // 5. Compute status from task states (TaskState.Archived tasks yield BacklogItemStatus.Archived)
    let status = BacklogItemStatus.compute tasks

    Ok
        { Item = item
          Status = status
          ViewId = viewId
          Tasks = tasks
          Path = itemPath }
