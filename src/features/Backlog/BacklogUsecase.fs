module Itr.Features.Backlog

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
