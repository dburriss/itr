module Itr.Tests.Communication.TaskDomainTests

open System
open Xunit
open Itr.Domain
open Itr.Features

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private today = DateOnly(2026, 3, 15)

let private mkBacklogId s =
    match BacklogId.tryCreate s with
    | Ok id -> id
    | Error e -> failwithf "invalid test backlog id %s: %A" s e

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

let private mkBacklogItem id title repos =
    { Id = mkBacklogId id
      Title = title
      Repos = repos |> List.map RepoId
      Type = Feature
      Priority = None
      Summary = None
      AcceptanceCriteria = []
      Dependencies = []
      CreatedAt = DateOnly.MinValue }

let private mkInput backlogId overrideTaskId =
    { Task.TakeInput.BacklogId = mkBacklogId backlogId
      Task.TakeInput.TaskIdOverride = overrideTaskId }

// ---------------------------------------------------------------------------
// TaskId derivation tests (6.1)
// ---------------------------------------------------------------------------

[<Fact>]
let ``single-repo item with no existing tasks uses backlog id as task id`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let backlogItem = mkBacklogItem "my-feature" "My Feature" [ "main-repo" ]
    let input = mkInput "my-feature" None

    match Task.takeBacklogItem productConfig backlogItem [] input today with
    | Ok [ task ] ->
        Assert.Equal("my-feature", TaskId.value task.Id)
        Assert.Equal(RepoId "main-repo", task.Repo)
        Assert.Equal(Planning, task.State)
        Assert.Equal(today, task.CreatedAt)
    | other -> failwithf "expected single task, got %A" other

[<Fact>]
let ``single-repo item re-taken uses repo-prefixed id`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let backlogItem = mkBacklogItem "my-feature" "My Feature" [ "main-repo" ]
    let input = mkInput "my-feature" None

    let existingTask =
        { Id = TaskId.create "my-feature"
          SourceBacklog = mkBacklogId "my-feature"
          Repo = RepoId "main-repo"
          State = Planning
          CreatedAt = today }

    match Task.takeBacklogItem productConfig backlogItem [ existingTask ] input today with
    | Ok [ task ] -> Assert.Equal("main-repo-my-feature", TaskId.value task.Id)
    | other -> failwithf "expected single task, got %A" other

[<Fact>]
let ``multi-repo item uses repo-prefixed ids for each repo`` () =
    let productConfig = mkProductConfig [ "api", "."; "web", "." ]
    let backlogItem = mkBacklogItem "cross-feature" "Cross Feature" [ "api"; "web" ]
    let input = mkInput "cross-feature" None

    match Task.takeBacklogItem productConfig backlogItem [] input today with
    | Ok tasks ->
        Assert.Equal(2, tasks.Length)
        let ids = tasks |> List.map (fun t -> TaskId.value t.Id) |> Set.ofList
        Assert.Contains("api-cross-feature", ids)
        Assert.Contains("web-cross-feature", ids)
    | other -> failwithf "expected two tasks, got %A" other

[<Fact>]
let ``re-take with collision on repo-prefixed id uses numeric suffix`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let backlogItem = mkBacklogItem "feat" "Feat" [ "main-repo" ]
    let input = mkInput "feat" None

    let existing1 =
        { Id = TaskId.create "feat"
          SourceBacklog = mkBacklogId "feat"
          Repo = RepoId "main-repo"
          State = Planning
          CreatedAt = today }

    let existing2 =
        { Id = TaskId.create "main-repo-feat"
          SourceBacklog = mkBacklogId "feat"
          Repo = RepoId "main-repo"
          State = Planning
          CreatedAt = today }

    match Task.takeBacklogItem productConfig backlogItem [ existing1; existing2 ] input today with
    | Ok [ task ] -> Assert.Equal("main-repo-feat-2", TaskId.value task.Id)
    | other -> failwithf "expected single task, got %A" other

// ---------------------------------------------------------------------------
// --task-id override tests (6.2)
// ---------------------------------------------------------------------------

[<Fact>]
let ``task-id override is used when provided for single-repo item`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let backlogItem = mkBacklogItem "my-feature" "My Feature" [ "main-repo" ]
    let input = mkInput "my-feature" (Some "custom-task")

    match Task.takeBacklogItem productConfig backlogItem [] input today with
    | Ok [ task ] -> Assert.Equal("custom-task", TaskId.value task.Id)
    | other -> failwithf "expected single task, got %A" other

[<Fact>]
let ``task-id override fails with TaskIdConflict when id already exists`` () =
    let productConfig = mkProductConfig [ "main-repo", "." ]
    let backlogItem = mkBacklogItem "my-feature" "My Feature" [ "main-repo" ]
    let input = mkInput "my-feature" (Some "existing-task")

    let existingTask =
        { Id = TaskId.create "existing-task"
          SourceBacklog = mkBacklogId "my-feature"
          Repo = RepoId "main-repo"
          State = Planning
          CreatedAt = today }

    match Task.takeBacklogItem productConfig backlogItem [ existingTask ] input today with
    | Error(TaskIdConflict id) -> Assert.Equal("existing-task", TaskId.value id)
    | other -> failwithf "expected TaskIdConflict, got %A" other

// ---------------------------------------------------------------------------
// TaskIdOverrideRequiresSingleRepo (6.3)
// ---------------------------------------------------------------------------

[<Fact>]
let ``task-id override on multi-repo item returns TaskIdOverrideRequiresSingleRepo`` () =
    let productConfig = mkProductConfig [ "api", "."; "web", "." ]
    let backlogItem = mkBacklogItem "cross-feature" "Cross Feature" [ "api"; "web" ]
    let input = mkInput "cross-feature" (Some "my-override")

    match Task.takeBacklogItem productConfig backlogItem [] input today with
    | Error TaskIdOverrideRequiresSingleRepo -> Assert.True(true)
    | other -> failwithf "expected TaskIdOverrideRequiresSingleRepo, got %A" other

// ---------------------------------------------------------------------------
// Repo validation (6.4)
// ---------------------------------------------------------------------------

[<Fact>]
let ``repo not in product config returns RepoNotInProduct`` () =
    let productConfig = mkProductConfig [ "known-repo", "." ]
    let backlogItem = mkBacklogItem "my-feature" "My Feature" [ "unknown-repo" ]
    let input = mkInput "my-feature" None

    match Task.takeBacklogItem productConfig backlogItem [] input today with
    | Error(RepoNotInProduct(RepoId id)) -> Assert.Equal("unknown-repo", id)
    | other -> failwithf "expected RepoNotInProduct, got %A" other
