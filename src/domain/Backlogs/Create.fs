module Itr.Domain.Backlogs.Create

open Itr.Domain

/// Map a TaskError to a BacklogError for use in backlog-level pipelines.
let private taskErrorToBacklogError (e: TaskError) : BacklogError =
    match e with
    | TaskStoreError(path, msg) -> ProductConfigParseError(path, msg)
    | _ -> ProductConfigParseError("", $"%A{e}")

type Input =
    { BacklogId: string
      Title: string
      Repos: string list
      ItemType: string option
      Priority: string option
      Summary: string option
      AcceptanceCriteria: string list
      DependsOn: string list }

/// Pure pipeline: validates inputs and builds a BacklogItem.
/// Does not perform any I/O.
let execute
    (productConfig: ProductConfig)
    (input: Input)
    (today: System.DateOnly)
    : Result<BacklogItem, BacklogError> =

    match BacklogId.tryCreate input.BacklogId with
    | Error _ -> Error(BacklogItemNotFound(BacklogId input.BacklogId))
    | Ok backlogId ->

    if System.String.IsNullOrWhiteSpace(input.Title) then
        Error MissingTitle
    else

    let itemTypeStr = input.ItemType |> Option.defaultValue "feature"
    match BacklogItemType.tryParse itemTypeStr with
    | Error e -> Error e
    | Ok itemType ->

    let resolvedReposResult =
        match input.Repos with
        | [] ->
            if productConfig.Repos.Count = 1 then
                productConfig.Repos |> Map.toList |> List.map fst |> Ok
            else
                Error(RepoNotInProduct(RepoId ""))
        | repos -> Ok (repos |> List.map RepoId)

    match resolvedReposResult with
    | Error e -> Error e
    | Ok repoIds ->

    let unknownRepo =
        repoIds |> List.tryFind (fun repoId -> not (Map.containsKey repoId productConfig.Repos))

    match unknownRepo with
    | Some repoId -> Error(RepoNotInProduct repoId)
    | None ->

    let depResults =
        input.DependsOn |> List.map BacklogId.tryCreate

    let depErrors = depResults |> List.choose (function Error _ -> Some () | Ok _ -> None)
    match depErrors with
    | _ :: _ -> Error(BacklogItemNotFound(BacklogId ""))
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
