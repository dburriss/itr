module Itr.Tests.Acceptance.TaskInfoAcceptanceTests

open System
open System.IO
open System.Text.Json
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
            let allTasksList = allTasks |> List.map fst
            let taskYamlPath = allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) |> Option.map snd |> Option.defaultValue ""
            match Tasks.Query.getDetail { TaskId = taskId; AllTasks = allTasksList; TaskYamlPath = taskYamlPath } with
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
            let allTasksList = allTasks |> List.map fst
            let taskYamlPath = allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) |> Option.map snd |> Option.defaultValue ""
            match Tasks.Query.getDetail { TaskId = taskId; AllTasks = allTasksList; TaskYamlPath = taskYamlPath } with
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
            let allTasksList = allTasks |> List.map fst
            let taskYamlPath = allTasks |> List.tryFind (fun (t, _) -> t.Id = taskId) |> Option.map snd |> Option.defaultValue ""
            match Tasks.Query.getDetail { TaskId = taskId; AllTasks = allTasksList; TaskYamlPath = taskYamlPath } with
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
            let allTasksList = allTasks |> List.map fst
            match Tasks.Query.getDetail { TaskId = missingId; AllTasks = allTasksList; TaskYamlPath = "" } with
            | Ok _ -> failwith "expected Error, got Ok"
            | Error(TaskNotFound id) ->
                Assert.Equal("unknown-id", TaskId.value id)
            | Error e -> failwithf "unexpected error: %A" e
    finally
        Directory.Delete(root, true)
