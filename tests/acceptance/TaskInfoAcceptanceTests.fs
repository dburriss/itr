module Itr.Tests.Acceptance.TaskInfoAcceptanceTests

open System
open System.IO
open System.Text.Json
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter
open Itr.Features

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private mkRoot () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-task-info-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    root

let private writeTaskYaml
    (coordRoot: string)
    (backlogFolder: string)
    (taskFolderName: string)
    (taskId: string)
    (backlogId: string)
    (repo: string)
    (state: string) =
    let taskDir = Path.Combine(coordRoot, "BACKLOG", backlogFolder, "tasks", taskFolderName)
    Directory.CreateDirectory(taskDir) |> ignore
    let yaml = $"""id: {taskId}
source:
  backlog: {backlogId}
repo: {repo}
state: {state}
created_at: 2026-01-15
"""
    File.WriteAllText(Path.Combine(taskDir, "task.yaml"), yaml)

let private writeItemYaml (coordRoot: string) (backlogId: string) (repos: string list) =
    let dir = Path.Combine(coordRoot, "BACKLOG", backlogId)
    Directory.CreateDirectory(dir) |> ignore
    let repoLines = repos |> List.map (fun r -> $"  - {r}") |> String.concat "\n"
    let yaml = $"id: {backlogId}\ntitle: {backlogId}\nrepos:\n{repoLines}\n"
    File.WriteAllText(Path.Combine(dir, "item.yaml"), yaml)

let private mkBacklogId s =
    match BacklogId.tryCreate s with
    | Ok id -> id
    | Error e -> failwithf "invalid backlog id %s: %A" s e

// ---------------------------------------------------------------------------
// 4.2 task info shows full detail
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail returns full detail record for a known task`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "approved"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "feat-a"
            match Task.getTaskDetail taskId allTasks false with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                Assert.Equal("feat-a", TaskId.value detail.Task.Id)
                Assert.Equal("feat-a", BacklogId.value detail.Task.SourceBacklog)
                Assert.Equal(RepoId "repo-1", detail.Task.Repo)
                Assert.Equal(TaskState.Approved, detail.Task.State)
                Assert.Equal(DateOnly(2026, 1, 15), detail.Task.CreatedAt)
                Assert.True(detail.PlanApproved)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.3 task info plan exists when plan.md present
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail PlanExists true when plan md file is present`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        // Create plan.md in the task directory
        let planPath = Path.Combine(root, "BACKLOG", "feat-a", "tasks", "feat-a", "plan.md")
        File.WriteAllText(planPath, "# Plan")

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let planExists = File.Exists(planPath)
            let taskId = TaskId.create "feat-a"
            match Task.getTaskDetail taskId allTasks planExists with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                Assert.True(detail.PlanExists)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``getTaskDetail PlanExists false when plan md file is absent`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        let planPath = Path.Combine(root, "BACKLOG", "feat-a", "tasks", "feat-a", "plan.md")
        // Do NOT create the plan file

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let planExists = File.Exists(planPath)
            let taskId = TaskId.create "feat-a"
            match Task.getTaskDetail taskId allTasks planExists with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                Assert.False(detail.PlanExists)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.4 task info shows siblings
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail shows siblings that share the same backlog item`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1"; "repo-2" ]
        writeTaskYaml root "feat-a" "feat-a-repo1" "feat-a-repo1" "feat-a" "repo-1" "planning"
        writeTaskYaml root "feat-a" "feat-a-repo2" "feat-a-repo2" "feat-a" "repo-2" "approved"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "feat-a-repo1"
            match Task.getTaskDetail taskId allTasks false with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                Assert.Equal(1, detail.Siblings.Length)
                Assert.Equal(TaskId.create "feat-a-repo2", detail.Siblings.[0].Id)
                Assert.Equal(RepoId "repo-2", detail.Siblings.[0].Repo)
                Assert.Equal(TaskState.Approved, detail.Siblings.[0].State)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``getTaskDetail shows no siblings when task is the only one for its backlog`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        writeItemYaml root "feat-b" [ "repo-1" ]
        writeTaskYaml root "feat-b" "feat-b" "feat-b" "feat-b" "repo-1" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "feat-a"
            match Task.getTaskDetail taskId allTasks false with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                Assert.Empty(detail.Siblings)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.5 task info json output is valid
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail returns data suitable for valid JSON output`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1"; "repo-2" ]
        writeTaskYaml root "feat-a" "feat-a-r1" "feat-a-r1" "feat-a" "repo-1" "approved"
        writeTaskYaml root "feat-a" "feat-a-r2" "feat-a-r2" "feat-a" "repo-2" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "feat-a-r1"
            match Task.getTaskDetail taskId allTasks false with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                // Verify all JSON fields are accessible (id, backlog, repo, state,
                // planExists, planApproved, createdAt, siblings)
                Assert.Equal("feat-a-r1", TaskId.value detail.Task.Id)
                Assert.Equal("feat-a", BacklogId.value detail.Task.SourceBacklog)
                Assert.Equal(RepoId "repo-1", detail.Task.Repo)
                Assert.Equal(TaskState.Approved, detail.Task.State)
                Assert.False(detail.PlanExists)
                Assert.True(detail.PlanApproved)
                Assert.Equal(DateOnly(2026, 1, 15), detail.Task.CreatedAt)
                Assert.Equal(1, detail.Siblings.Length)

                // Verify siblings have id, repo, state
                let sib = detail.Siblings.[0]
                Assert.Equal(TaskId.create "feat-a-r2", sib.Id)
                Assert.Equal(RepoId "repo-2", sib.Repo)
                Assert.Equal(TaskState.Planning, sib.State)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``getTaskDetail siblings is empty list when no siblings exist`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let taskId = TaskId.create "feat-a"
            match Task.getTaskDetail taskId allTasks false with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                Assert.Empty(detail.Siblings)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.6 task info returns error for unknown id
// ---------------------------------------------------------------------------

[<Fact>]
let ``getTaskDetail returns TaskNotFound error for unknown task id`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let missingId = TaskId.create "unknown-id"
            match Task.getTaskDetail missingId allTasks false with
            | Ok _ -> failwith "expected Error, got Ok"
            | Error(TaskNotFound id) ->
                Assert.Equal("unknown-id", TaskId.value id)
            | Error e -> failwithf "unexpected error: %A" e
    finally
        Directory.Delete(root, true)
