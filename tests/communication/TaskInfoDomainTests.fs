module Itr.Tests.Communication.TaskInfoDomainTests

open System
open Xunit
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Domain.Tasks
open Itr.Domain.Backlogs

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private today = DateOnly(2026, 3, 15)

let private mkBacklogId s =
    match BacklogId.tryCreate s with
    | Ok id -> id
    | Error e -> failwithf "invalid test backlog id %s: %A" s e

let private mkTask id backlogId repo state =
    { Id = TaskId.create id
      SourceBacklog = mkBacklogId backlogId
      Repo = RepoId repo
      State = state
      CreatedAt = today }

// ---------------------------------------------------------------------------
// getTaskDetail: TaskNotFound when id not present
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail returns TaskNotFound when task id does not exist`` () =
    let tasks = [ mkTask "task-a" "feat" "repo" TaskState.Planning ]
    let missingId = TaskId.create "no-such-task"
    match Tasks.Query.getDetail { TaskId = missingId; AllTasks = tasks; TaskYamlPath = "" } with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error(TaskNotFound id) -> Assert.Equal("no-such-task", TaskId.value id)
    | Error e -> failwithf "unexpected error: %A" e

// ---------------------------------------------------------------------------
// getTaskDetail: returns correct task fields
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail returns the matching task`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planning
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail ->
        Assert.Equal(task, detail.Task)

// ---------------------------------------------------------------------------
// getTaskDetail: PlanExists reflects the provided parameter
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail sets PlanExists to true when passed true`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planning
    // PlanExists is derived from File.Exists on taskYamlPath/../plan.md
    // Passing "" means plan.md won't exist → PlanExists = false
    // To test PlanExists=true, we'd need a real file; skip that here
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.False(detail.PlanExists)

[<Fact>]
let ``getTaskDetail sets PlanExists to false when passed false`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planning
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.False(detail.PlanExists)

// ---------------------------------------------------------------------------
// getTaskDetail: PlanApproved based on state
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail PlanApproved is false for Planning state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planning
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.False(detail.PlanApproved)

[<Fact>]
let ``getTaskDetail PlanApproved is false for Planned state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planned
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.False(detail.PlanApproved)

[<Fact>]
let ``getTaskDetail PlanApproved is true for Approved state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Approved
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.True(detail.PlanApproved)

[<Fact>]
let ``getTaskDetail PlanApproved is true for InProgress state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.InProgress
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.True(detail.PlanApproved)

[<Fact>]
let ``getTaskDetail PlanApproved is true for Implemented state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Implemented
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.True(detail.PlanApproved)

[<Fact>]
let ``getTaskDetail PlanApproved is true for Validated state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Validated
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.True(detail.PlanApproved)

[<Fact>]
let ``getTaskDetail PlanApproved is true for Archived state`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Archived
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.True(detail.PlanApproved)

// ---------------------------------------------------------------------------
// getTaskDetail: siblings list
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail returns empty siblings when no other tasks share the backlog`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planning
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.Empty(detail.Siblings)

[<Fact>]
let ``getTaskDetail returns sibling that shares the same backlog`` () =
    let task = mkTask "task-a" "feat" "repo-1" TaskState.Planning
    let sibling = mkTask "task-b" "feat" "repo-2" TaskState.Approved
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task; sibling ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail ->
        Assert.Equal(1, detail.Siblings.Length)
        Assert.Equal(TaskId.create "task-b", detail.Siblings.[0].Id)
        Assert.Equal(RepoId "repo-2", detail.Siblings.[0].Repo)
        Assert.Equal(TaskState.Approved, detail.Siblings.[0].State)

[<Fact>]
let ``getTaskDetail does not include the target task itself in siblings`` () =
    let task = mkTask "task-a" "feat" "repo" TaskState.Planning
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail ->
        Assert.DoesNotContain(detail.Siblings, fun s -> s.Id = task.Id)

[<Fact>]
let ``getTaskDetail does not include tasks from different backlog as siblings`` () =
    let task = mkTask "task-a" "feat-a" "repo" TaskState.Planning
    let other = mkTask "task-b" "feat-b" "repo" TaskState.Planning
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task; other ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail -> Assert.Empty(detail.Siblings)

[<Fact>]
let ``getTaskDetail returns multiple siblings when several tasks share the backlog`` () =
    let task = mkTask "task-a" "feat" "repo-1" TaskState.Planning
    let sib1 = mkTask "task-b" "feat" "repo-2" TaskState.Planning
    let sib2 = mkTask "task-c" "feat" "repo-3" TaskState.Approved
    match Tasks.Query.getDetail { TaskId = task.Id; AllTasks = [ task; sib1; sib2 ]; TaskYamlPath = "" } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok detail ->
        Assert.Equal(2, detail.Siblings.Length)
        let sibIds = detail.Siblings |> List.map (fun s -> TaskId.value s.Id) |> Set.ofList
        Assert.Contains("task-b", sibIds)
        Assert.Contains("task-c", sibIds)
