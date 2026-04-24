module Itr.Tests.Acceptance.TaskApproveAcceptanceTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter
open Itr.Domain.Portfolios
open Itr.Domain.Tasks
open Itr.Domain.Backlogs

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private mkRoot () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-task-approve-tests-{Guid.NewGuid():N}")

    Directory.CreateDirectory(root) |> ignore
    root

let private writeItemYaml (coordRoot: string) (backlogId: string) =
    let dir = Path.Combine(coordRoot, "BACKLOG", backlogId)
    Directory.CreateDirectory(dir) |> ignore
    let yaml = $"id: {backlogId}\ntitle: Test Feature\nrepos:\n  - main-repo\n"
    File.WriteAllText(Path.Combine(dir, "item.yaml"), yaml)

let private writeTaskYaml
    (coordRoot: string)
    (backlogFolder: string)
    (taskFolderName: string)
    (taskId: string)
    (backlogId: string)
    (repo: string)
    (state: string)
    =
    let taskDir =
        Path.Combine(coordRoot, "BACKLOG", backlogFolder, "tasks", taskFolderName)

    Directory.CreateDirectory(taskDir) |> ignore

    let yaml =
        $"""id: {taskId}
source:
  backlog: {backlogId}
repo: {repo}
state: {state}
created_at: 2026-01-15
"""

    File.WriteAllText(Path.Combine(taskDir, "task.yaml"), yaml)

let private writePlanMd (coordRoot: string) (backlogId: string) (taskId: string) =
    let planDir = Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskId)
    Directory.CreateDirectory(planDir) |> ignore
    File.WriteAllText(Path.Combine(planDir, "plan.md"), "# Plan\n\nThis is the plan.\n")

// ---------------------------------------------------------------------------
// Happy path: planned task with plan.md → approved YAML
// ---------------------------------------------------------------------------

[<Fact>]
let ``approveTask integration writes task state to approved`` () =
    let root = mkRoot ()

    try
        writeItemYaml root "my-feature"
        writeTaskYaml root "my-feature" "my-feature" "my-feature" "my-feature" "main-repo" "planned"
        writePlanMd root "my-feature" "my-feature"

        let taskStore = TaskStoreAdapter() :> ITaskStore

        match taskStore.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "my-feature"

            match allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
            | None -> failwith "task not found"
            | Some(task, _) ->
                let planPath =
                    Path.Combine(root, "BACKLOG", "my-feature", "tasks", "my-feature", "plan.md")

                let planExists = File.Exists(planPath)

                match Tasks.Approve.execute { Task = task; PlanExists = planExists } with
                | Error e -> failwithf "approveTask failed: %A" e
                | Ok(updatedTask, _) ->
                    match taskStore.WriteTask root updatedTask with
                    | Error e -> failwithf "WriteTask failed: %A" e
                    | Ok() ->
                        match taskStore.ListAllTasks root with
                        | Error e -> failwithf "reload failed: %A" e
                        | Ok reloadedTasks ->
                            match reloadedTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
                            | None -> failwith "task not found after write"
                            | Some(reloaded, _) -> Assert.Equal(TaskState.Approved, reloaded.State)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// Missing plan: planned task without plan.md → MissingPlanArtifact
// ---------------------------------------------------------------------------

[<Fact>]
let ``approveTask returns MissingPlanArtifact when plan md does not exist`` () =
    let root = mkRoot ()

    try
        writeItemYaml root "my-feature"
        writeTaskYaml root "my-feature" "my-feature" "my-feature" "my-feature" "main-repo" "planned"
        // No plan.md written

        let taskStore = TaskStoreAdapter() :> ITaskStore

        match taskStore.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "my-feature"

            match allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
            | None -> failwith "task not found"
            | Some(task, _) ->
                let planPath =
                    Path.Combine(root, "BACKLOG", "my-feature", "tasks", "my-feature", "plan.md")

                let planExists = File.Exists(planPath)

                match Tasks.Approve.execute { Task = task; PlanExists = planExists } with
                | Ok _ -> failwith "expected Error, got Ok"
                | Error(MissingPlanArtifact id) -> Assert.Equal(taskId, id)
                | Error e -> failwithf "unexpected error: %A" e
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// Wrong state: planning task with plan.md → InvalidTaskState
// ---------------------------------------------------------------------------

[<Fact>]
let ``approveTask returns InvalidTaskState when task is in planning state`` () =
    let root = mkRoot ()

    try
        writeItemYaml root "my-feature"
        writeTaskYaml root "my-feature" "my-feature" "my-feature" "my-feature" "main-repo" "planning"
        writePlanMd root "my-feature" "my-feature"

        let taskStore = TaskStoreAdapter() :> ITaskStore

        match taskStore.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "my-feature"

            match allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
            | None -> failwith "task not found"
            | Some(task, _) ->
                let planPath =
                    Path.Combine(root, "BACKLOG", "my-feature", "tasks", "my-feature", "plan.md")

                let planExists = File.Exists(planPath)

                match Tasks.Approve.execute { Task = task; PlanExists = planExists } with
                | Ok _ -> failwith "expected Error, got Ok"
                | Error(InvalidTaskState(id, current)) ->
                    Assert.Equal(taskId, id)
                    Assert.Equal(TaskState.Planning, current)
                | Error e -> failwithf "unexpected error: %A" e
    finally
        Directory.Delete(root, true)
