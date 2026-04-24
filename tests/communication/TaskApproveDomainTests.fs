module Itr.Tests.Communication.TaskApproveDomainTests

open System
open Xunit
open Itr.Domain
open Itr.Domain.Tasks

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private today = DateOnly(2026, 3, 15)

let private mkTask id backlogId state =
    { Id = TaskId.create id
      SourceBacklog =
        match BacklogId.tryCreate backlogId with
        | Ok bid -> bid
        | Error e -> failwithf "invalid backlog id %s: %A" backlogId e
      Repo = RepoId "main-repo"
      State = state
      CreatedAt = today }

// ---------------------------------------------------------------------------
// approveTask tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``approveTask transitions Planned task with plan to Approved`` () =
    let task = mkTask "feat-a" "my-feature" TaskState.Planned

    match Tasks.Approve.execute { Task = task; PlanExists = true } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok(updatedTask, wasAlreadyApproved) ->
        Assert.Equal(TaskState.Approved, updatedTask.State)
        Assert.False(wasAlreadyApproved)

[<Fact>]
let ``approveTask re-approving Approved task is idempotent`` () =
    let task = mkTask "feat-a" "my-feature" TaskState.Approved

    match Tasks.Approve.execute { Task = task; PlanExists = true } with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok(updatedTask, wasAlreadyApproved) ->
        Assert.Equal(TaskState.Approved, updatedTask.State)
        Assert.True(wasAlreadyApproved)

[<Fact>]
let ``approveTask returns InvalidTaskState for Planning task`` () =
    let taskId = TaskId.create "feat-a"
    let task = mkTask "feat-a" "my-feature" TaskState.Planning

    match Tasks.Approve.execute { Task = task; PlanExists = true } with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error(InvalidTaskState(id, current)) ->
        Assert.Equal(taskId, id)
        Assert.Equal(TaskState.Planning, current)
    | Error e -> failwithf "unexpected error: %A" e

[<Fact>]
let ``approveTask returns InvalidTaskState for InProgress task`` () =
    let taskId = TaskId.create "feat-a"
    let task = mkTask "feat-a" "my-feature" TaskState.InProgress

    match Tasks.Approve.execute { Task = task; PlanExists = true } with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error(InvalidTaskState(id, current)) ->
        Assert.Equal(taskId, id)
        Assert.Equal(TaskState.InProgress, current)
    | Error e -> failwithf "unexpected error: %A" e

[<Fact>]
let ``approveTask returns MissingPlanArtifact for Planned task without plan`` () =
    let taskId = TaskId.create "feat-a"
    let task = mkTask "feat-a" "my-feature" TaskState.Planned

    match Tasks.Approve.execute { Task = task; PlanExists = false } with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error(MissingPlanArtifact id) -> Assert.Equal(taskId, id)
    | Error e -> failwithf "unexpected error: %A" e
