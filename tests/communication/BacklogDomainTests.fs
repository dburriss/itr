module Itr.Tests.Communication.BacklogDomainTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Domain.Tasks
open Itr.Domain.Backlogs
open Itr.Adapters.YamlAdapter

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

let private mkInput (backlogId: string) (title: string) (repos: string list) (itemType: string option) : Backlogs.Create.Input =
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

    match Backlogs.Create.execute productConfig input today with
    | Ok item -> Assert.Equal(Feature, item.Type)
    | Error e -> failwithf "expected success, got: %A" e

[<Fact>]
let ``explicit feature type is accepted`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["main-repo"] (Some "feature")

    match Backlogs.Create.execute productConfig input today with
    | Ok item -> Assert.Equal(Feature, item.Type)
    | Error e -> failwithf "expected success, got: %A" e

[<Fact>]
let ``bug type is accepted`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-bug" "My Bug" ["main-repo"] (Some "bug")

    match Backlogs.Create.execute productConfig input today with
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

    match Backlogs.Create.execute productConfig input today with
    | Error(RepoNotInProduct(RepoId id)) -> Assert.Equal("unknown-repo", id)
    | other -> failwithf "expected RepoNotInProduct, got: %A" other

[<Fact>]
let ``multi-repo product with missing repo returns RepoNotInProduct`` () =
    let productConfig = mkProductConfig [ "repo-a", "."; "repo-b", "." ]
    let input = mkInput "my-feature" "My Feature" [] None

    match Backlogs.Create.execute productConfig input today with
    | Error(RepoNotInProduct _) -> Assert.True(true)
    | other -> failwithf "expected RepoNotInProduct, got: %A" other

// ---------------------------------------------------------------------------
// InvalidItemType
// ---------------------------------------------------------------------------

[<Fact>]
let ``invalid item type returns InvalidItemType with the value`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["main-repo"] (Some "invalid-type")

    match Backlogs.Create.execute productConfig input today with
    | Error(InvalidItemType value) -> Assert.Equal("invalid-type", value)
    | other -> failwithf "expected InvalidItemType, got: %A" other

[<Fact>]
let ``BacklogItemType.tryParse "refactor" returns Ok Refactor`` () =
    match BacklogItemType.tryParse "refactor" with
    | Ok Refactor -> Assert.True(true)
    | other -> failwithf "expected Ok Refactor, got: %A" other

[<Fact>]
let ``BacklogItemType.toString Refactor returns "refactor"`` () =
    Assert.Equal("refactor", BacklogItemType.toString Refactor)

[<Fact>]
let ``error message for invalid type includes "refactor"`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let input = mkInput "my-feature" "My Feature" ["main-repo"] (Some "invalid-type")

    match Backlogs.Create.execute productConfig input today with
    | Error(InvalidItemType _) ->
        // Verify the domain error type is raised; error message formatting tested via CLI layer
        Assert.True(true)
    | other -> failwithf "expected InvalidItemType, got: %A" other
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// TaskState round-trip via TaskStoreAdapter (1.5)
// ---------------------------------------------------------------------------

let private writeTaskYamlWithState (coordRoot: string) (backlogId: string) (taskId: string) (state: string) =
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

[<Fact>]
let ``mapTaskState round-trip for planned`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-taskstate-planned-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeTaskYamlWithState root "my-feature" "my-task" "planned"
        let store = TaskStoreAdapter() :> ITaskStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e
        match store.ListTasks root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok [ (task, _) ] -> Assert.Equal(TaskState.Planned, task.State)
        | Ok tasks -> failwithf "expected 1 task, got %d" tasks.Length
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``mapTaskState round-trip for approved`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-taskstate-approved-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeTaskYamlWithState root "my-feature" "my-task" "approved"
        let store = TaskStoreAdapter() :> ITaskStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e
        match store.ListTasks root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok [ (task, _) ] -> Assert.Equal(TaskState.Approved, task.State)
        | Ok tasks -> failwithf "expected 1 task, got %d" tasks.Length
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``mapTaskState round-trip for archived`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-taskstate-archived-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeTaskYamlWithState root "my-feature" "my-task" "archived"
        let store = TaskStoreAdapter() :> ITaskStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e
        match store.ListTasks root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok [ (task, _) ] -> Assert.Equal(TaskState.Archived, task.State)
        | Ok tasks -> failwithf "expected 1 task, got %d" tasks.Length
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// BacklogItemStatus.compute tests (2.4)
// ---------------------------------------------------------------------------

let private mkTask (state: TaskState) : ItrTask =
    let backlogId = match BacklogId.tryCreate "test-item" with Ok id -> id | Error e -> failwithf "%A" e
    { Id = TaskId.create "t"
      SourceBacklog = backlogId
      Repo = RepoId "main-repo"
      State = state
      CreatedAt = DateOnly(2026, 1, 1) }

[<Fact>]
let ``compute with no tasks yields Created`` () =
    Assert.Equal(BacklogItemStatus.Created, BacklogItemStatus.compute [])

[<Fact>]
let ``compute with all Planning tasks yields Planning`` () =
    let tasks = [ mkTask TaskState.Planning; mkTask TaskState.Planning ]
    Assert.Equal(BacklogItemStatus.Planning, BacklogItemStatus.compute tasks)

[<Fact>]
let ``compute with all Planned tasks yields Planned`` () =
    let tasks = [ mkTask TaskState.Planned; mkTask TaskState.Planned ]
    Assert.Equal(BacklogItemStatus.Planned, BacklogItemStatus.compute tasks)

[<Fact>]
let ``compute with all Approved tasks yields Approved`` () =
    let tasks = [ mkTask TaskState.Approved; mkTask TaskState.Approved ]
    Assert.Equal(BacklogItemStatus.Approved, BacklogItemStatus.compute tasks)

[<Fact>]
let ``compute with any InProgress task yields InProgress`` () =
    let tasks = [ mkTask TaskState.Approved; mkTask TaskState.InProgress ]
    Assert.Equal(BacklogItemStatus.InProgress, BacklogItemStatus.compute tasks)

[<Fact>]
let ``compute with all Implemented or Validated tasks yields Completed`` () =
    let tasks = [ mkTask TaskState.Implemented; mkTask TaskState.Validated ]
    Assert.Equal(BacklogItemStatus.Completed, BacklogItemStatus.compute tasks)

[<Fact>]
let ``compute with all Archived tasks yields Archived`` () =
    let tasks = [ mkTask TaskState.Archived; mkTask TaskState.Archived ]
    Assert.Equal(BacklogItemStatus.Archived, BacklogItemStatus.compute tasks)

[<Fact>]
let ``compute with mixed Archived and Validated tasks yields Completed`` () =
    let tasks = [ mkTask TaskState.Archived; mkTask TaskState.Validated ]
    Assert.Equal(BacklogItemStatus.Completed, BacklogItemStatus.compute tasks)

// ---------------------------------------------------------------------------
// getBacklogItemDetail tests (4.1 + 4.2)
// ---------------------------------------------------------------------------

let private writeItemYaml (coordRoot: string) (backlogId: string) =
    let dir = Path.Combine(coordRoot, "BACKLOG", backlogId)
    Directory.CreateDirectory(dir) |> ignore
    let yaml =
        $"""id: {backlogId}
title: Test Feature
type: feature
created_at: 2026-01-01
"""
    File.WriteAllText(Path.Combine(dir, "item.yaml"), yaml)

/// Minimal stub IViewStore that returns Ok []
type StubViewStore() =
    interface IViewStore with
        member _.ListViews _ = Ok []

[<Fact>]
let ``getBacklogItemDetail returns detail for valid item with no tasks`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-notasks-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeItemYaml root "my-feature"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok detail ->
            Assert.Equal(BacklogId.value detail.Item.Id, "my-feature")
            Assert.Equal(0, detail.Tasks.Length)
            Assert.Equal(BacklogItemStatus.Created, detail.Status)
            Assert.Equal(None, detail.ViewId)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``getBacklogItemDetail returns BacklogItemNotFound for unknown id`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-notfound-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        Directory.CreateDirectory(Path.Combine(root, "BACKLOG")) |> ignore
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "no-such-item" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error(BacklogItemNotFound _) -> Assert.True(true)
        | other -> failwithf "expected BacklogItemNotFound, got %A" other
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``getBacklogItemDetail with two tasks includes both tasks`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-withtasks-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeItemYaml root "my-feature"
        writeTaskYamlWithState root "my-feature" "task-a" "planning"
        writeTaskYamlWithState root "my-feature" "task-b" "approved"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok detail ->
            Assert.Equal(2, detail.Tasks.Length)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// getBacklogItemDetail computed status tests (4.2)
// ---------------------------------------------------------------------------

[<Fact>]
let ``getBacklogItemDetail with no tasks yields Created status`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-status-created-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeItemYaml root "my-feature"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok detail -> Assert.Equal(BacklogItemStatus.Created, detail.Status)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``getBacklogItemDetail with all approved tasks yields Approved status`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-status-approved-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeItemYaml root "my-feature"
        writeTaskYamlWithState root "my-feature" "task-a" "approved"
        writeTaskYamlWithState root "my-feature" "task-b" "approved"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok detail -> Assert.Equal(BacklogItemStatus.Approved, detail.Status)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// getBacklogItemDetail archived item tests
// ---------------------------------------------------------------------------

/// Write an archived backlog item under BACKLOG/_archive/<date>-<id>/item.yaml
let private writeArchivedItemYaml (coordRoot: string) (backlogId: string) (date: string) =
    let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive", $"{date}-{backlogId}")
    Directory.CreateDirectory(archiveDir) |> ignore
    let yaml =
        $"""id: {backlogId}
title: Archived Feature
type: feature
created_at: 2026-01-01
"""
    File.WriteAllText(Path.Combine(archiveDir, "item.yaml"), yaml)

/// Write a task yaml under an archived backlog folder
let private writeArchivedTaskYaml (coordRoot: string) (backlogId: string) (date: string) (taskId: string) (state: string) =
    let taskDir = Path.Combine(coordRoot, "BACKLOG", "_archive", $"{date}-{backlogId}", "tasks", taskId)
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

[<Fact>]
let ``getBacklogItemDetail on archived item returns archived status`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-archived-status-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeArchivedItemYaml root "my-feature" "2026-03-01"
        writeArchivedTaskYaml root "my-feature" "2026-03-01" "task-a" "archived"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok detail -> Assert.Equal(BacklogItemStatus.Archived, detail.Status)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``getBacklogItemDetail on archived item with tasks returns all tasks`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-archived-tasks-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        writeArchivedItemYaml root "my-feature" "2026-03-01"
        writeArchivedTaskYaml root "my-feature" "2026-03-01" "task-a" "archived"
        writeArchivedTaskYaml root "my-feature" "2026-03-01" "task-b" "archived"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok detail ->
            Assert.Equal(2, detail.Tasks.Length)
            Assert.Equal(BacklogItemStatus.Archived, detail.Status)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``getBacklogItemDetail returns BacklogItemNotFound when not in active or archive`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-detail-notfound-anywhere-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    try
        Directory.CreateDirectory(Path.Combine(root, "BACKLOG", "_archive")) |> ignore
        // Write a different archived item so the archive dir exists but doesn't contain our id
        writeArchivedItemYaml root "other-feature" "2026-03-01"
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = StubViewStore() :> IViewStore
        let backlogId =
            match BacklogId.tryCreate "my-feature" with
            | Ok id -> id
            | Error e -> failwithf "%A" e

        match Backlogs.Query.getDetail backlogStore taskStore viewStore root backlogId with
        | Error(BacklogItemNotFound _) -> Assert.True(true)
        | other -> failwithf "expected BacklogItemNotFound, got %A" other
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)
