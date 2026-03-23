module Itr.Tests.Acceptance.BacklogAcceptanceTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter
open Itr.Features

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

// ---------------------------------------------------------------------------
// Backlog list acceptance tests (8.1-8.6)
// ---------------------------------------------------------------------------

let private writeItemYaml (coordRoot: string) (id: string) (itemType: string) (createdAt: string) =
    let itemDir = Path.Combine(coordRoot, "BACKLOG", id)
    Directory.CreateDirectory(itemDir) |> ignore
    let yaml =
        $"""id: {id}
title: {id} title
repos:
  - main-repo
type: {itemType}
created_at: {createdAt}
"""
    File.WriteAllText(Path.Combine(itemDir, "item.yaml"), yaml)

let private writeTaskYamlForList (coordRoot: string) (backlogId: string) (taskId: string) (state: string) =
    let taskDir = Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskId)
    Directory.CreateDirectory(taskDir) |> ignore
    let yaml =
        $"""id: {taskId}
source:
  backlog: {backlogId}
repo: main-repo
state: {state}
created_at: 2026-01-01
"""
    File.WriteAllText(Path.Combine(taskDir, "task.yaml"), yaml)

let private writeViewYaml (coordRoot: string) (viewId: string) (items: string list) =
    let viewsDir = Path.Combine(coordRoot, "BACKLOG", "_views")
    Directory.CreateDirectory(viewsDir) |> ignore
    let itemsYaml =
        items
        |> List.map (fun i -> $"  - {i}")
        |> String.concat "\n"
    let yaml = $"id: {viewId}\ndescription: test view\nitems:\n{itemsYaml}\n"
    File.WriteAllText(Path.Combine(viewsDir, $"{viewId}.yaml"), yaml)

let private loadSnapshotForTest (coordRoot: string) =
    let backlogStore = BacklogStoreAdapter() :> IBacklogStore
    let taskStore = TaskStoreAdapter() :> ITaskStore
    let viewStore = ViewStoreAdapter() :> IViewStore
    Backlog.loadSnapshot backlogStore taskStore viewStore coordRoot

[<Fact>]
let ``8.1 list returns all active items sorted by creation date`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "item-b" "feature" "2026-02-01"
    writeItemYaml fixture.CoordRoot "item-a" "feature" "2026-01-01"
    writeItemYaml fixture.CoordRoot "item-c" "feature" "2026-03-01"

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = None; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(3, items.Length)
        Assert.Equal("item-a", BacklogId.value items.[0].Item.Id)
        Assert.Equal("item-b", BacklogId.value items.[1].Item.Id)
        Assert.Equal("item-c", BacklogId.value items.[2].Item.Id)

[<Fact>]
let ``8.2 list filtered by view returns only matching items`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "feat-1" "feature" "2026-01-01"
    writeItemYaml fixture.CoordRoot "feat-2" "feature" "2026-01-02"
    writeItemYaml fixture.CoordRoot "feat-3" "feature" "2026-01-03"
    writeViewYaml fixture.CoordRoot "test-view" [ "feat-1"; "feat-3" ]

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = Some "test-view"; Status = None; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(2, items.Length)
        let ids = items |> List.map (fun s -> BacklogId.value s.Item.Id) |> Set.ofList
        Assert.Contains("feat-1", ids)
        Assert.Contains("feat-3", ids)

[<Fact>]
let ``8.3 list filtered by type returns only matching items`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "feat-x" "feature" "2026-01-01"
    writeItemYaml fixture.CoordRoot "bug-x" "bug" "2026-01-02"
    writeItemYaml fixture.CoordRoot "bug-y" "bug" "2026-01-03"

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = None; ItemType = Some Bug }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(2, items.Length)
        Assert.True(items |> List.forall (fun s -> s.Item.Type = Bug))

[<Fact>]
let ``8.4 list with no items returns empty`` () =
    use fixture = new SingleRepoBacklogFixture()

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = None; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Empty(items)

[<Fact>]
let ``8.5 task count is correct`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "my-item" "feature" "2026-01-01"
    writeTaskYamlForList fixture.CoordRoot "my-item" "task-1" "planning"
    writeTaskYamlForList fixture.CoordRoot "my-item" "task-2" "planning"

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = None; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(1, items.Length)
        Assert.Equal(2, items.[0].TaskCount)

[<Fact>]
let ``8.6 multi-view membership warns and first-match wins`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "shared-item" "feature" "2026-01-01"
    // Write two views alphabetically; a-view comes first
    writeViewYaml fixture.CoordRoot "a-view" [ "shared-item" ]
    writeViewYaml fixture.CoordRoot "b-view" [ "shared-item" ]

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = None; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(1, items.Length)
        // First-match alphabetically should be a-view
        Assert.Equal(Some "a-view", items.[0].ViewId)

// ---------------------------------------------------------------------------
// Archived item acceptance tests (8.7-8.8)
// ---------------------------------------------------------------------------

let private writeArchivedItemYaml (coordRoot: string) (id: string) (archiveDirName: string) =
    let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive", archiveDirName)
    Directory.CreateDirectory(archiveDir) |> ignore
    let yaml =
        $"""id: {id}
title: {id} title
repos:
  - main-repo
type: feature
created_at: 2026-01-01
"""
    File.WriteAllText(Path.Combine(archiveDir, "item.yaml"), yaml)

[<Fact>]
let ``8.7 status archived returns only archived items`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "active-item" "feature" "2026-01-01"
    writeArchivedItemYaml fixture.CoordRoot "archived-item" "2026-01-15-archived-item"

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = Some BacklogItemStatus.Archived; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(1, items.Length)
        Assert.Equal("archived-item", BacklogId.value items.[0].Item.Id)
        Assert.Equal(BacklogItemStatus.Archived, items.[0].Status)

[<Fact>]
let ``8.8 default list includes both active and archived items in snapshot`` () =
    use fixture = new SingleRepoBacklogFixture()
    writeItemYaml fixture.CoordRoot "active-item" "feature" "2026-02-01"
    writeArchivedItemYaml fixture.CoordRoot "archived-item" "2026-01-15-archived-item"

    match loadSnapshotForTest fixture.CoordRoot with
    | Error e -> failwithf "expected Ok, got %A" e
    | Ok snapshot ->
        let filter: Backlog.BacklogListFilter = { ViewId = None; Status = None; ItemType = None }
        let items = Backlog.listBacklogItems filter snapshot
        Assert.Equal(2, items.Length)
        let ids = items |> List.map (fun s -> BacklogId.value s.Item.Id) |> Set.ofList
        Assert.Contains("active-item", ids)
        Assert.Contains("archived-item", ids)
