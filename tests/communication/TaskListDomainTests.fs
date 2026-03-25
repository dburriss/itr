module Itr.Tests.Communication.TaskListDomainTests

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

let private mkTask id backlogId repo state =
    { Id = TaskId.create id
      SourceBacklog = mkBacklogId backlogId
      Repo = RepoId repo
      State = state
      CreatedAt = today }

// ---------------------------------------------------------------------------
// listTasks: empty list
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks returns empty list when given empty list`` () =
    let result = Task.listTasks []
    Assert.Empty(result)

// ---------------------------------------------------------------------------
// listTasks: PlanApproved = false for pre-approval states
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks sets PlanApproved false for Planning state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Planning
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.False(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved false for Planned state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Planned
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.False(summaries.[0].PlanApproved)

// ---------------------------------------------------------------------------
// listTasks: PlanApproved = true for approved-and-beyond states
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks sets PlanApproved true for Approved state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Approved
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for InProgress state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.InProgress
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for Implemented state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Implemented
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for Validated state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Validated
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for Archived state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Archived
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

// ---------------------------------------------------------------------------
// listTasks: Task field is preserved
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks preserves the original task reference`` () =
    let task = mkTask "t1" "feat" "main-repo" TaskState.Planning
    let summaries = Task.listTasks [ task ]
    Assert.Equal(1, summaries.Length)
    Assert.Equal(task, summaries.[0].Task)

// ---------------------------------------------------------------------------
// filterTasks: no filters returns all
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks with no filters returns all summaries`` () =
    let tasks = [
        mkTask "t1" "feat-a" "repo-a" TaskState.Planning
        mkTask "t2" "feat-b" "repo-b" TaskState.Approved
    ]
    let summaries = Task.listTasks tasks
    let result = Task.filterTasks None None None summaries
    Assert.Equal(2, result.Length)

// ---------------------------------------------------------------------------
// filterTasks: filter by backlog id
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by backlog id returns only matching tasks`` () =
    let tasks = [
        mkTask "t1" "feat-a" "repo" TaskState.Planning
        mkTask "t2" "feat-b" "repo" TaskState.Planning
    ]
    let summaries = Task.listTasks tasks
    let result = Task.filterTasks (Some(mkBacklogId "feat-a")) None None summaries
    Assert.Equal(1, result.Length)
    Assert.Equal("feat-a", BacklogId.value result.[0].Task.SourceBacklog)

[<Fact>]
let ``filterTasks by backlog id with no matches returns empty`` () =
    let tasks = [ mkTask "t1" "feat-a" "repo" TaskState.Planning ]
    let summaries = Task.listTasks tasks
    let result = Task.filterTasks (Some(mkBacklogId "feat-b")) None None summaries
    Assert.Empty(result)

// ---------------------------------------------------------------------------
// filterTasks: filter by repo
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by repo returns only matching tasks`` () =
    let tasks = [
        mkTask "t1" "feat-a" "repo-a" TaskState.Planning
        mkTask "t2" "feat-a" "repo-b" TaskState.Planning
    ]
    let summaries = Task.listTasks tasks
    let result = Task.filterTasks None (Some(RepoId "repo-a")) None summaries
    Assert.Equal(1, result.Length)
    Assert.Equal(RepoId "repo-a", result.[0].Task.Repo)

// ---------------------------------------------------------------------------
// filterTasks: filter by state
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by state returns only tasks in that state`` () =
    let tasks = [
        mkTask "t1" "feat" "repo" TaskState.Planning
        mkTask "t2" "feat" "repo" TaskState.Approved
        mkTask "t3" "feat" "repo" TaskState.InProgress
    ]
    let summaries = Task.listTasks tasks
    let result = Task.filterTasks None None (Some TaskState.Planning) summaries
    Assert.Equal(1, result.Length)
    Assert.Equal(TaskState.Planning, result.[0].Task.State)

// ---------------------------------------------------------------------------
// filterTasks: combined filters (AND semantics)
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by repo and state uses AND semantics`` () =
    let tasks = [
        mkTask "t1" "feat-a" "repo-a" TaskState.Planning
        mkTask "t2" "feat-a" "repo-b" TaskState.Planning
        mkTask "t3" "feat-a" "repo-a" TaskState.Approved
    ]
    let summaries = Task.listTasks tasks
    let result = Task.filterTasks None (Some(RepoId "repo-a")) (Some TaskState.Planning) summaries
    Assert.Equal(1, result.Length)
    Assert.Equal("t1", TaskId.value result.[0].Task.Id)

[<Fact>]
let ``filterTasks by backlog, repo, and state all together`` () =
    let tasks = [
        mkTask "t1" "feat-a" "repo-a" TaskState.Planning
        mkTask "t2" "feat-b" "repo-a" TaskState.Planning
        mkTask "t3" "feat-a" "repo-a" TaskState.Approved
    ]
    let summaries = Task.listTasks tasks
    let result =
        Task.filterTasks
            (Some(mkBacklogId "feat-a"))
            (Some(RepoId "repo-a"))
            (Some TaskState.Planning)
            summaries
    Assert.Equal(1, result.Length)
    Assert.Equal("t1", TaskId.value result.[0].Task.Id)
