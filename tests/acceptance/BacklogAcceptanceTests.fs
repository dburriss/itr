module Itr.Tests.Acceptance.BacklogAcceptanceTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter

// ---------------------------------------------------------------------------
// Fixture
// ---------------------------------------------------------------------------

type BacklogFixture() =
    // product root = root; coord root = root/.itr
    let root = Path.Combine(Path.GetTempPath(), $"itr-backlog-tests-{Guid.NewGuid():N}")
    let coordRoot = Path.Combine(root, ".itr")

    do
        Directory.CreateDirectory(coordRoot) |> ignore
        Directory.CreateDirectory(Path.Combine(coordRoot, "BACKLOG")) |> ignore

        // product.yaml with two repos (multi-repo)
        let productYaml =
            """id: test-product
repos:
  main-repo:
    path: ./
  second-repo:
    path: ../second
coordination:
  mode: standalone
  path: .itr
"""
        File.WriteAllText(Path.Combine(root, "product.yaml"), productYaml)

    member _.Root = root
    member _.CoordRoot = coordRoot

    interface IDisposable with
        member _.Dispose() =
            if Directory.Exists(root) then
                Directory.Delete(root, true)

/// Fixture with a single-repo product
type SingleRepoBacklogFixture() =
    let root = Path.Combine(Path.GetTempPath(), $"itr-backlog-single-{Guid.NewGuid():N}")
    let coordRoot = Path.Combine(root, ".itr")

    do
        Directory.CreateDirectory(coordRoot) |> ignore
        Directory.CreateDirectory(Path.Combine(coordRoot, "BACKLOG")) |> ignore

        let productYaml =
            """id: test-product
repos:
  main-repo:
    path: ./
coordination:
  mode: standalone
  path: .itr
"""
        File.WriteAllText(Path.Combine(root, "product.yaml"), productYaml)

    member _.Root = root
    member _.CoordRoot = coordRoot

    interface IDisposable with
        member _.Dispose() =
            if Directory.Exists(root) then
                Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private today = DateOnly(2026, 3, 22)

let private mkProductConfig (root: string) =
    let productId =
        match ProductId.tryCreate "test-product" with
        | Ok id -> id
        | Error e -> failwithf "%A" e

    let repos =
        [ RepoId "main-repo", ({ Path = "./"; Url = None }: RepoConfig)
          RepoId "second-repo", ({ Path = "../second"; Url = None }: RepoConfig) ]
        |> Map.ofList

    { Id = productId; Repos = repos }

let private mkSingleRepoProductConfig () =
    let productId =
        match ProductId.tryCreate "test-product" with
        | Ok id -> id
        | Error e -> failwithf "%A" e

    let repos =
        [ RepoId "main-repo", ({ Path = "./"; Url = None }: RepoConfig) ]
        |> Map.ofList

    { Id = productId; Repos = repos }

let private runCreate (productConfig: Itr.Domain.ProductConfig) (coordRoot: string) (backlogId: string) (title: string) (repos: string list) (itemType: string option) =
    let backlogStore = BacklogStoreAdapter() :> IBacklogStore

    let input: Itr.Features.Backlog.CreateBacklogItemInput =
        { BacklogId = backlogId
          Title = title
          Repos = repos
          ItemType = itemType
          Priority = None
          Summary = None
          AcceptanceCriteria = []
          DependsOn = [] }

    // Check duplicate
    match BacklogId.tryCreate backlogId with
    | Error e -> Error(sprintf "%A" e)
    | Ok bid ->
        if backlogStore.BacklogItemExists coordRoot bid then
            Error(sprintf "%A" (DuplicateBacklogId bid))
        else
            match Itr.Features.Backlog.createBacklogItem productConfig input today with
            | Error e -> Error(sprintf "%A" e)
            | Ok item ->
                backlogStore.WriteBacklogItem coordRoot item
                |> Result.mapError (sprintf "%A")
                |> Result.map (fun () -> item)

// ---------------------------------------------------------------------------
// 6.1 Acceptance tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``minimal valid invocation writes item yaml`` () =
    use fixture = new BacklogFixture()
    let productConfig = mkProductConfig fixture.Root

    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" ["main-repo"] None with
    | Error msg -> failwithf "expected success, got error: %s" msg
    | Ok item ->
        let itemPath = Path.Combine(fixture.CoordRoot, "BACKLOG", "my-feature", "item.yaml")
        Assert.True(File.Exists(itemPath), $"Expected item.yaml at: {itemPath}")

        let yaml = File.ReadAllText(itemPath)
        Assert.Contains("id: my-feature", yaml)
        Assert.Contains("title: My Feature", yaml)
        Assert.Contains("main-repo", yaml)
        Assert.Contains("type: feature", yaml)
        Assert.Contains("created_at:", yaml)

[<Fact>]
let ``duplicate id is rejected`` () =
    use fixture = new BacklogFixture()
    let productConfig = mkProductConfig fixture.Root

    // First create
    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" ["main-repo"] None with
    | Error msg -> failwithf "first create failed: %s" msg
    | Ok _ ->

    // Second create should fail
    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" ["main-repo"] None with
    | Ok _ -> failwith "expected DuplicateBacklogId error"
    | Error msg -> Assert.Contains("DuplicateBacklogId", msg)

[<Fact>]
let ``unknown repo is rejected`` () =
    use fixture = new BacklogFixture()
    let productConfig = mkProductConfig fixture.Root

    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" ["unknown-repo"] None with
    | Ok _ -> failwith "expected RepoNotInProduct error"
    | Error msg -> Assert.Contains("RepoNotInProduct", msg)

[<Fact>]
let ``invalid type is rejected`` () =
    use fixture = new BacklogFixture()
    let productConfig = mkProductConfig fixture.Root

    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" ["main-repo"] (Some "invalid-type") with
    | Ok _ -> failwith "expected InvalidItemType error"
    | Error msg -> Assert.Contains("InvalidItemType", msg)

[<Fact>]
let ``single-repo product auto-resolves repo when omitted`` () =
    use fixture = new SingleRepoBacklogFixture()
    let productConfig = mkSingleRepoProductConfig ()

    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" [] None with
    | Error msg -> failwithf "expected success, got error: %s" msg
    | Ok item ->
        Assert.Equal(1, item.Repos.Length)
        Assert.Equal(RepoId "main-repo", item.Repos.[0])

[<Fact>]
let ``multi-repo product without repo returns error`` () =
    use fixture = new BacklogFixture()
    let productConfig = mkProductConfig fixture.Root

    match runCreate productConfig fixture.CoordRoot "my-feature" "My Feature" [] None with
    | Ok _ -> failwith "expected error when repo omitted for multi-repo product"
    | Error msg -> Assert.Contains("RepoNotInProduct", msg)

[<Fact>]
let ``created_at is set to today`` () =
    use fixture = new SingleRepoBacklogFixture()
    let productConfig = mkSingleRepoProductConfig ()

    match runCreate productConfig fixture.CoordRoot "dated-feature" "Dated Feature" [] None with
    | Error msg -> failwithf "expected success, got error: %s" msg
    | Ok item ->
        Assert.Equal(today, item.CreatedAt)

        let itemPath = Path.Combine(fixture.CoordRoot, "BACKLOG", "dated-feature", "item.yaml")
        let yaml = File.ReadAllText(itemPath)
        Assert.Contains("created_at: 2026-03-22", yaml)
