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
    | Ok activeItems ->

    // 2. Load archived items
    match backlogStore.ListArchivedBacklogItems coordRoot with
    | Error e -> Error e
    | Ok archivedItems ->

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

    // 5. Build summaries for active items (isArchived = false)
    let activeSummaryResults =
        activeItems
        |> List.map (fun item ->
            let id = BacklogId.value item.Id
            match taskStore.ListTasks coordRoot item.Id with
            | Error e -> Error e
            | Ok tasks ->
                let status = BacklogItemStatus.compute tasks false
                Ok
                    { Item = item
                      Status = status
                      ViewId = Map.tryFind id itemViewMap
                      TaskCount = tasks.Length })

    // 6. Build summaries for archived items (isArchived = true)
    // Tasks for archived items live under BACKLOG/_archive/<date-id>/tasks/
    // Use the original item id to look up tasks via the archive path directly.
    // Since ITaskStore.ListTasks constructs BACKLOG/<backlogId>/tasks/, we pass
    // coordRoot unchanged — archived items' tasks are not reachable this way.
    // Instead we just pass isArchived=true so status = Archived regardless of tasks.
    let archivedSummaryResults =
        archivedItems
        |> List.map (fun item ->
            let id = BacklogId.value item.Id
            let status = BacklogItemStatus.compute [] true
            Ok
                { Item = item
                  Status = status
                  ViewId = Map.tryFind id itemViewMap
                  TaskCount = 0 })

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
