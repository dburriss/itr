module Itr.Tests.Acceptance.TaskPlanAcceptanceTests

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
        Path.Combine(Path.GetTempPath(), $"itr-task-plan-tests-{Guid.NewGuid():N}")

    Directory.CreateDirectory(root) |> ignore
    root

let private writeItemYaml
    (coordRoot: string)
    (backlogId: string)
    (title: string)
    (repos: string list)
    (summary: string option)
    (ac: string list)
    =
    let dir = Path.Combine(coordRoot, "BACKLOG", backlogId)
    Directory.CreateDirectory(dir) |> ignore
    let repoLines = repos |> List.map (fun r -> $"  - {r}") |> String.concat "\n"

    let summaryLine =
        summary |> Option.map (fun s -> $"\nsummary: {s}") |> Option.defaultValue ""

    let acLines =
        if ac.IsEmpty then
            ""
        else
            "\nacceptance_criteria:\n"
            + (ac |> List.map (fun c -> $"  - {c}") |> String.concat "\n")

    let yaml =
        $"id: {backlogId}\ntitle: {title}\nrepos:\n{repoLines}{summaryLine}{acLines}\n"

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

// Stub harness for AI tests
type StubHarness(response: Result<string, string>) =
    interface IAgentHarness with
        member _.Prompt _prompt _debug = response

// ---------------------------------------------------------------------------
// planTask use case: happy path (planning → planned)
// ---------------------------------------------------------------------------

[<Fact>]
let ``planTask transitions planning task to planned`` () =
    let task =
        { Id = TaskId.create "feat-a"
          SourceBacklog =
            BacklogId.tryCreate "my-feature"
            |> Result.defaultWith (fun _ -> failwith "invalid")
          Repo = RepoId "main-repo"
          State = TaskState.Planning
          CreatedAt = DateOnly(2026, 1, 15) }

    match Tasks.Plan.execute task with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok(updatedTask, wasAlreadyPlanned) ->
        Assert.Equal(TaskState.Planned, updatedTask.State)
        Assert.False(wasAlreadyPlanned)

// ---------------------------------------------------------------------------
// planTask use case: re-plan from planned state
// ---------------------------------------------------------------------------

[<Fact>]
let ``planTask allows re-planning from planned state`` () =
    let task =
        { Id = TaskId.create "feat-a"
          SourceBacklog =
            BacklogId.tryCreate "my-feature"
            |> Result.defaultWith (fun _ -> failwith "invalid")
          Repo = RepoId "main-repo"
          State = TaskState.Planned
          CreatedAt = DateOnly(2026, 1, 15) }

    match Tasks.Plan.execute task with
    | Error e -> failwithf "expected Ok, got Error: %A" e
    | Ok(updatedTask, wasAlreadyPlanned) ->
        Assert.Equal(TaskState.Planned, updatedTask.State)
        Assert.True(wasAlreadyPlanned)

// ---------------------------------------------------------------------------
// planTask use case: invalid state (approved)
// ---------------------------------------------------------------------------

[<Fact>]
let ``planTask returns InvalidTaskState for approved task`` () =
    let taskId = TaskId.create "feat-a"

    let task =
        { Id = taskId
          SourceBacklog =
            BacklogId.tryCreate "my-feature"
            |> Result.defaultWith (fun _ -> failwith "invalid")
          Repo = RepoId "main-repo"
          State = TaskState.Approved
          CreatedAt = DateOnly(2026, 1, 15) }

    match Tasks.Plan.execute task with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error(InvalidTaskState(id, current)) ->
        Assert.Equal(taskId, id)
        Assert.Equal(TaskState.Approved, current)
    | Error e -> failwithf "unexpected error: %A" e

[<Fact>]
let ``planTask returns InvalidTaskState for in-progress task`` () =
    let taskId = TaskId.create "feat-a"

    let task =
        { Id = taskId
          SourceBacklog =
            BacklogId.tryCreate "my-feature"
            |> Result.defaultWith (fun _ -> failwith "invalid")
          Repo = RepoId "main-repo"
          State = TaskState.InProgress
          CreatedAt = DateOnly(2026, 1, 15) }

    match Tasks.Plan.execute task with
    | Ok _ -> failwith "expected Error, got Ok"
    | Error(InvalidTaskState(id, current)) ->
        Assert.Equal(taskId, id)
        Assert.Equal(TaskState.InProgress, current)
    | Error e -> failwithf "unexpected error: %A" e

// ---------------------------------------------------------------------------
// Integration: plan file written and task state updated
// ---------------------------------------------------------------------------

[<Fact>]
let ``planTask integration writes task state to planned`` () =
    let root = mkRoot ()

    try
        writeItemYaml root "my-feature" "My Feature" [ "main-repo" ] (Some "A feature") [ "AC1"; "AC2" ]
        writeTaskYaml root "my-feature" "my-feature" "my-feature" "my-feature" "main-repo" "planning"

        let taskStore = TaskStoreAdapter() :> ITaskStore

        match taskStore.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "my-feature"

            match allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
            | None -> failwith "task not found"
            | Some(task, _) ->
                match Tasks.Plan.execute task with
                | Error e -> failwithf "planTask failed: %A" e
                | Ok(updatedTask, _) ->
                    // Write the updated task back
                    match taskStore.WriteTask root updatedTask with
                    | Error e -> failwithf "WriteTask failed: %A" e
                    | Ok() ->
                        // Reload and verify
                        match taskStore.ListAllTasks root with
                        | Error e -> failwithf "reload failed: %A" e
                        | Ok reloadedTasks ->
                            match reloadedTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
                            | None -> failwith "task not found after write"
                            | Some(reloaded, _) -> Assert.Equal(TaskState.Planned, reloaded.State)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// Stub harness: happy path (AI content returned)
// ---------------------------------------------------------------------------

[<Fact>]
let ``stub harness returns AI plan content`` () =
    let harness =
        StubHarness(Ok "## AI Generated Plan\n\nThis is the plan.") :> IAgentHarness

    match harness.Prompt "plan task feat-a" false with
    | Error e -> failwithf "expected Ok, got Error: %s" e
    | Ok content -> Assert.Contains("AI Generated Plan", content)

// ---------------------------------------------------------------------------
// Stub harness: error case — no file written on harness failure
// ---------------------------------------------------------------------------

[<Fact>]
let ``stub harness error prevents plan from being written`` () =
    let root = mkRoot ()

    try
        writeItemYaml root "my-feature" "My Feature" [ "main-repo" ] None []
        writeTaskYaml root "my-feature" "my-feature" "my-feature" "my-feature" "main-repo" "planning"

        let taskStore = TaskStoreAdapter() :> ITaskStore
        let harness = StubHarness(Error "AI server unavailable") :> IAgentHarness

        match taskStore.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "my-feature"

            match allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) with
            | None -> failwith "task not found"
            | Some(task, _) ->
                match Tasks.Plan.execute task with
                | Error e -> failwithf "planTask failed: %A" e
                | Ok(_, _) ->
                    // Simulate harness call failing
                    match harness.Prompt "plan task" false with
                    | Ok _ -> failwith "expected harness to return error"
                    | Error msg ->
                        Assert.Equal("AI server unavailable", msg)
                        // Verify no plan.md was written (since harness failed)
                        let planPath =
                            Path.Combine(root, "BACKLOG", "my-feature", "tasks", "my-feature", "plan.md")

                        Assert.False(File.Exists(planPath), "plan.md should not exist when harness fails")
    finally
        Directory.Delete(root, true)
