module Itr.Tests.Communication.BacklogDomainTests

open System
open Xunit
open Itr.Domain
open Itr.Features

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private today = DateOnly(2026, 3, 22)

let private mkProductConfig repos =
    let productId =
        match ProductId.tryCreate "test-product" with
        | Ok id -> id
        | Error e -> failwithf "%A" e

    { Id = productId
      Repos =
        repos
        |> List.map (fun (k, v) -> RepoId k, ({ Path = v; Url = None }: RepoConfig))
        |> Map.ofList }

let private mkInput (backlogId: string) (title: string) (repos: string list) (itemType: string option) : Backlog.CreateBacklogItemInput =
    { BacklogId = backlogId
      Title = title
      Repos = repos
      ItemType = itemType
      Priority = None
      Summary = None
      AcceptanceCriteria = []
      DependsOn = [] }

// ---------------------------------------------------------------------------
// Type default behavior
// ---------------------------------------------------------------------------

[<Fact>]
let ``type defaults to feature when omitted`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["main-repo"] None

    match Backlog.createBacklogItem productConfig input today with
    | Ok item -> Assert.Equal(Feature, item.Type)
    | Error e -> failwithf "expected success, got: %A" e

[<Fact>]
let ``explicit feature type is accepted`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["main-repo"] (Some "feature")

    match Backlog.createBacklogItem productConfig input today with
    | Ok item -> Assert.Equal(Feature, item.Type)
    | Error e -> failwithf "expected success, got: %A" e

[<Fact>]
let ``bug type is accepted`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-bug" "My Bug" ["main-repo"] (Some "bug")

    match Backlog.createBacklogItem productConfig input today with
    | Ok item -> Assert.Equal(Bug, item.Type)
    | Error e -> failwithf "expected success, got: %A" e

// ---------------------------------------------------------------------------
// Duplicate id message
// ---------------------------------------------------------------------------

[<Fact>]
let ``DuplicateBacklogId carries the id`` () =
    match BacklogId.tryCreate "my-feature" with
    | Error e -> failwithf "unexpected: %A" e
    | Ok bid ->
        let err = DuplicateBacklogId bid
        match err with
        | DuplicateBacklogId id -> Assert.Equal("my-feature", BacklogId.value id)
        | other -> failwithf "unexpected case: %A" other

// ---------------------------------------------------------------------------
// Unknown repo message
// ---------------------------------------------------------------------------

[<Fact>]
let ``unknown repo returns RepoNotInProduct with the repo id`` () =
    let productConfig = mkProductConfig [ "known-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["unknown-repo"] None

    match Backlog.createBacklogItem productConfig input today with
    | Error(RepoNotInProduct(RepoId id)) -> Assert.Equal("unknown-repo", id)
    | other -> failwithf "expected RepoNotInProduct, got: %A" other

[<Fact>]
let ``multi-repo product with missing repo returns RepoNotInProduct`` () =
    let productConfig = mkProductConfig [ "repo-a", "."; "repo-b", "." ]
    let input = mkInput "my-feature" "My Feature" [] None

    match Backlog.createBacklogItem productConfig input today with
    | Error(RepoNotInProduct _) -> Assert.True(true)
    | other -> failwithf "expected RepoNotInProduct, got: %A" other

// ---------------------------------------------------------------------------
// InvalidItemType
// ---------------------------------------------------------------------------

[<Fact>]
let ``invalid item type returns InvalidItemType with the value`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["main-repo"] (Some "invalid-type")

    match Backlog.createBacklogItem productConfig input today with
    | Error(InvalidItemType value) -> Assert.Equal("invalid-type", value)
    | other -> failwithf "expected InvalidItemType, got: %A" other
