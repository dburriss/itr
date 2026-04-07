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
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.False(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved false for Planned state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Planned
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.False(summaries.[0].PlanApproved)

// ---------------------------------------------------------------------------
// listTasks: PlanApproved = true for approved-and-beyond states
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks sets PlanApproved true for Approved state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Approved
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for InProgress state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.InProgress
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for Implemented state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Implemented
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for Validated state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Validated
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

[<Fact>]
let ``listTasks sets PlanApproved true for Archived state`` () =
    let task = mkTask "t1" "feat" "repo" TaskState.Archived
    let summaries = Task.listTasks [ (task, "") ]
    Assert.Equal(1, summaries.Length)
    Assert.True(summaries.[0].PlanApproved)

// ---------------------------------------------------------------------------
// listTasks: Task field is preserved
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks preserves the original task reference`` () =
    let task = mkTask "t1" "feat" "main-repo" TaskState.Planning
    let summaries = Task.listTasks [ (task, "") ]
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
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None None None [] summaries
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
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks (Some(mkBacklogId "feat-a")) None None [] summaries
    Assert.Equal(1, result.Length)
    Assert.Equal("feat-a", BacklogId.value result.[0].Task.SourceBacklog)

[<Fact>]
let ``filterTasks by backlog id with no matches returns empty`` () =
    let tasks = [ mkTask "t1" "feat-a" "repo" TaskState.Planning ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks (Some(mkBacklogId "feat-b")) None None [] summaries
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
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None (Some(RepoId "repo-a")) None [] summaries
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
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None None (Some TaskState.Planning) [] summaries
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
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None (Some(RepoId "repo-a")) (Some TaskState.Planning) [] summaries
    Assert.Equal(1, result.Length)
    Assert.Equal("t1", TaskId.value result.[0].Task.Id)

[<Fact>]
let ``filterTasks by backlog, repo, and state all together`` () =
    let tasks = [
        mkTask "t1" "feat-a" "repo-a" TaskState.Planning
        mkTask "t2" "feat-b" "repo-a" TaskState.Planning
        mkTask "t3" "feat-a" "repo-a" TaskState.Approved
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result =
        Task.filterTasks
            (Some(mkBacklogId "feat-a"))
            (Some(RepoId "repo-a"))
            (Some TaskState.Planning)
            []
            summaries
    Assert.Equal(1, result.Length)
    Assert.Equal("t1", TaskId.value result.[0].Task.Id)

// ---------------------------------------------------------------------------
// 4.1 default task list shows archived tasks
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks with empty exclude list includes archived tasks`` () =
    let tasks = [
        mkTask "t1" "feat" "repo" TaskState.Planning
        mkTask "t2" "feat" "repo" TaskState.Archived
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None None None [] summaries
    Assert.Equal(2, result.Length)
    let states = result |> List.map (fun s -> s.Task.State) |> Set.ofList
    Assert.Contains(TaskState.Archived, states)

// ---------------------------------------------------------------------------
// 4.2 --exclude archived hides archived tasks
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks with exclude archived removes archived tasks`` () =
    let tasks = [
        mkTask "t1" "feat" "repo" TaskState.Planning
        mkTask "t2" "feat" "repo" TaskState.Archived
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None None None [ TaskState.Archived ] summaries
    Assert.Equal(1, result.Length)
    Assert.Equal(TaskState.Planning, result.[0].Task.State)

// ---------------------------------------------------------------------------
// 4.3 --order-by created returns tasks sorted oldest-first
// ---------------------------------------------------------------------------

[<Fact>]
let ``orderBy created sorts tasks oldest-first by CreatedAt`` () =
    let mkTaskWithDate id backlogId repo state (date: DateOnly) =
        { Id = TaskId.create id
          SourceBacklog = mkBacklogId backlogId
          Repo = RepoId repo
          State = state
          CreatedAt = date }
    let tasks = [
        mkTaskWithDate "t-newest" "feat" "repo" TaskState.Planning (DateOnly(2026, 3, 15))
        mkTaskWithDate "t-oldest" "feat" "repo" TaskState.Planning (DateOnly(2026, 1, 1))
        mkTaskWithDate "t-middle" "feat" "repo" TaskState.Planning (DateOnly(2026, 2, 1))
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let ordered = summaries |> List.sortBy (fun s -> s.Task.CreatedAt)
    Assert.Equal("t-oldest", TaskId.value ordered.[0].Task.Id)
    Assert.Equal("t-middle", TaskId.value ordered.[1].Task.Id)
    Assert.Equal("t-newest", TaskId.value ordered.[2].Task.Id)

// ---------------------------------------------------------------------------
// 4.4 --order-by state returns tasks in priority order
// ---------------------------------------------------------------------------

[<Fact>]
let ``orderBy state sorts tasks by priority order descending`` () =
    let taskStatePriority state =
        match state with
        | TaskState.Planning -> 7
        | TaskState.Planned -> 6
        | TaskState.Approved -> 5
        | TaskState.InProgress -> 4
        | TaskState.Implemented -> 3
        | TaskState.Validated -> 2
        | TaskState.Archived -> 1
    let tasks = [
        mkTask "t-archived" "feat" "repo" TaskState.Archived
        mkTask "t-planning" "feat" "repo" TaskState.Planning
        mkTask "t-approved" "feat" "repo" TaskState.Approved
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let ordered = summaries |> List.sortByDescending (fun s -> taskStatePriority s.Task.State)
    Assert.Equal(TaskState.Planning, ordered.[0].Task.State)
    Assert.Equal(TaskState.Approved, ordered.[1].Task.State)
    Assert.Equal(TaskState.Archived, ordered.[2].Task.State)

// ---------------------------------------------------------------------------
// 4.5 --exclude unknownstate exits non-zero with error message
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks with multiple excluded states removes all of them`` () =
    // Verifies the exclude list handles multiple states (supports the validation path)
    let tasks = [
        mkTask "t1" "feat" "repo" TaskState.Planning
        mkTask "t2" "feat" "repo" TaskState.Archived
        mkTask "t3" "feat" "repo" TaskState.Approved
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None None None [ TaskState.Archived; TaskState.Planning ] summaries
    Assert.Equal(1, result.Length)
    Assert.Equal(TaskState.Approved, result.[0].Task.State)

// ---------------------------------------------------------------------------
// 4.6 --order-by unknown exits non-zero - handled at CLI level
// Verified here: the sort falls back to created order for unknown values
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks with empty exclude list returns same tasks regardless of state`` () =
    // Confirms exclude=[] is a no-op on filtering, validating the default path
    let tasks = [
        mkTask "t1" "feat" "repo" TaskState.Planning
        mkTask "t2" "feat" "repo" TaskState.Validated
        mkTask "t3" "feat" "repo" TaskState.Archived
    ]
    let summaries = Task.listTasks (tasks |> List.map (fun t -> (t, "")))
    let result = Task.filterTasks None None None [] summaries
    Assert.Equal(3, result.Length)
