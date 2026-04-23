module Itr.Tests.Acceptance.TaskApproveInMemoryTests

open System
open Xunit
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Tests.Acceptance.TestDoubles
open Itr.Tests.Acceptance.TestBuilders

// ---------------------------------------------------------------------------
// Acceptance tests using in-memory doubles and A./Given. builders
// ---------------------------------------------------------------------------

let private coordRoot = "/coord"

[<Fact>]
let ``approve planned task with plan succeeds and transitions to approved`` () =
    let taskId = A.taskId "my-feature"
    let backlogId = A.backlogId "my-feature"
    let task = A.taskInState taskId backlogId TaskState.Planned

    let fs = TestDoubles.InMemoryFileSystem()
    Given.planFile fs coordRoot backlogId taskId "# Plan\n\nContent.\n" |> ignore

    let planPath = ItrTask.planFile coordRoot backlogId taskId
    let planExists = fs.Exists planPath

    let input: Tasks.Approve.Input = { Task = task; PlanExists = planExists }

    match Tasks.Approve.execute input with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok (updatedTask, wasAlreadyApproved) ->
        Assert.False(wasAlreadyApproved)
        Assert.Equal(TaskState.Approved, updatedTask.State)

[<Fact>]
let ``approve planned task without plan returns MissingPlanArtifact`` () =
    let taskId = A.taskId "my-feature"
    let backlogId = A.backlogId "my-feature"
    let task = A.taskInState taskId backlogId TaskState.Planned

    let fs = TestDoubles.InMemoryFileSystem()
    // No plan file written
    let planPath = ItrTask.planFile coordRoot backlogId taskId
    let planExists = fs.Exists planPath

    let input: Tasks.Approve.Input = { Task = task; PlanExists = planExists }

    match Tasks.Approve.execute input with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error (MissingPlanArtifact id) -> Assert.Equal(taskId, id)
    | Error e -> failwithf "unexpected error: %A" e

[<Fact>]
let ``approve task in planning state returns InvalidTaskState`` () =
    let taskId = A.taskId "my-feature"
    let backlogId = A.backlogId "my-feature"
    let task = A.taskInState taskId backlogId TaskState.Planning

    let fs = TestDoubles.InMemoryFileSystem()
    Given.planFile fs coordRoot backlogId taskId "# Plan\n" |> ignore

    let planPath = ItrTask.planFile coordRoot backlogId taskId
    let planExists = fs.Exists planPath

    let input: Tasks.Approve.Input = { Task = task; PlanExists = planExists }

    match Tasks.Approve.execute input with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error (InvalidTaskState(id, current)) ->
        Assert.Equal(taskId, id)
        Assert.Equal(TaskState.Planning, current)
    | Error e -> failwithf "unexpected error: %A" e

[<Fact>]
let ``approve already approved task returns wasAlreadyApproved true`` () =
    let taskId = A.taskId "my-feature"
    let backlogId = A.backlogId "my-feature"
    let task = A.taskInState taskId backlogId TaskState.Approved

    let fs = TestDoubles.InMemoryFileSystem()
    Given.planFile fs coordRoot backlogId taskId "# Plan\n" |> ignore

    let planPath = ItrTask.planFile coordRoot backlogId taskId
    let planExists = fs.Exists planPath

    let input: Tasks.Approve.Input = { Task = task; PlanExists = planExists }

    match Tasks.Approve.execute input with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok (_, wasAlreadyApproved) ->
        Assert.True(wasAlreadyApproved)
