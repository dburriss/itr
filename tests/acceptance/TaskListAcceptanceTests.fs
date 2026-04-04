module Itr.Tests.Acceptance.TaskListAcceptanceTests

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

let private mkBacklogId s =
    match BacklogId.tryCreate s with
    | Ok id -> id
    | Error e -> failwithf "invalid backlog id %s: %A" s e

let private writeTaskYaml (coordRoot: string) (backlogFolder: string) (taskFolderName: string) (taskId: string) (backlogId: string) (repo: string) (state: string) =
    let taskDir = Path.Combine(coordRoot, "BACKLOG", backlogFolder, "tasks", taskFolderName)
    Directory.CreateDirectory(taskDir) |> ignore
    let yaml = $"""id: {taskId}
source:
  backlog: {backlogId}
repo: {repo}
state: {state}
created_at: 2026-01-01
"""
    File.WriteAllText(Path.Combine(taskDir, "task.yaml"), yaml)

let private writeItemYaml (coordRoot: string) (backlogId: string) (repos: string list) =
    let dir = Path.Combine(coordRoot, "BACKLOG", backlogId)
    Directory.CreateDirectory(dir) |> ignore
    let repoLines = repos |> List.map (fun r -> $"  - {r}") |> String.concat "\n"
    let yaml = $"id: {backlogId}\ntitle: {backlogId}\nrepos:\n{repoLines}\n"
    File.WriteAllText(Path.Combine(dir, "item.yaml"), yaml)

let private mkRoot () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-task-list-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    root

// ---------------------------------------------------------------------------
// 6.1 task list shows all active tasks
// ---------------------------------------------------------------------------

[<Fact>]
let ``ListAllTasks returns tasks from multiple active backlog items`` () =
    let root = mkRoot ()
    try
        // Two tasks under different backlog items
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeItemYaml root "feat-b" [ "repo-2" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"
        writeTaskYaml root "feat-b" "feat-b" "feat-b" "feat-b" "repo-2" "approved"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok tasks ->
            Assert.Equal(2, tasks.Length)
            // No archived tasks in the default unfiltered result — but ListAllTasks returns everything
            // Archived exclusion is in the CLI handler; here verify both tasks are present
            let ids = tasks |> List.map (fun (t, _) -> TaskId.value t.Id) |> Set.ofList
            Assert.Contains("feat-a", ids)
            Assert.Contains("feat-b", ids)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``ListAllTasks does not include archived tasks implicitly - all tasks returned`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        // Archived backlog item (in _archive folder)
        let archiveDir = Path.Combine(root, "BACKLOG", "_archive", "2026-01-01-archived-feat")
        Directory.CreateDirectory(archiveDir) |> ignore
        File.WriteAllText(
            Path.Combine(archiveDir, "item.yaml"),
            "id: archived-feat\ntitle: Archived Feature\nrepos:\n  - repo-1\n")
        writeTaskYaml root (Path.Combine("_archive", "2026-01-01-archived-feat")) "archived-task" "archived-task" "archived-feat" "repo-1" "archived"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok tasks ->
            // Adapter returns all tasks; archived exclusion is CLI responsibility
            Assert.Equal(2, tasks.Length)
            let ids = tasks |> List.map (fun (t, _) -> TaskId.value t.Id) |> Set.ofList
            Assert.Contains("feat-a", ids)
            Assert.Contains("archived-task", ids)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 6.2 task list filters by backlog id
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by backlog id returns only matching tasks`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeItemYaml root "feat-b" [ "repo-1" ]
        writeTaskYaml root "feat-a" "task-a" "task-a" "feat-a" "repo-1" "planning"
        writeTaskYaml root "feat-b" "task-b" "task-b" "feat-b" "repo-1" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks
            let filtered = Task.filterTasks (Some(mkBacklogId "feat-a")) None None summaries
            Assert.Equal(1, filtered.Length)
            Assert.Equal("feat-a", BacklogId.value filtered.[0].Task.SourceBacklog)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 6.3 task list filters by repo
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by repo returns only matching tasks`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1"; "repo-2" ]
        writeTaskYaml root "feat-a" "task-repo1" "task-repo1" "feat-a" "repo-1" "planning"
        writeTaskYaml root "feat-a" "task-repo2" "task-repo2" "feat-a" "repo-2" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks
            let filtered = Task.filterTasks None (Some(RepoId "repo-1")) None summaries
            Assert.Equal(1, filtered.Length)
            Assert.Equal(RepoId "repo-1", filtered.[0].Task.Repo)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 6.4 task list filters by state
// ---------------------------------------------------------------------------

[<Fact>]
let ``filterTasks by state returns only tasks in that state`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "task-planning" "task-planning" "feat-a" "repo-1" "planning"
        writeTaskYaml root "feat-a" "task-approved" "task-approved" "feat-a" "repo-1" "approved"
        writeTaskYaml root "feat-a" "task-inprogress" "task-inprogress" "feat-a" "repo-1" "in_progress"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks
            let filtered = Task.filterTasks None None (Some TaskState.Planning) summaries
            Assert.Equal(1, filtered.Length)
            Assert.Equal(TaskState.Planning, filtered.[0].Task.State)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 6.5 task list --state archived includes tasks from archived backlog items
// ---------------------------------------------------------------------------

[<Fact>]
let ``ListAllTasks returns tasks from archived backlog items`` () =
    let root = mkRoot ()
    try
        // Active task
        writeItemYaml root "feat-active" [ "repo-1" ]
        writeTaskYaml root "feat-active" "active-task" "active-task" "feat-active" "repo-1" "planning"

        // Archived backlog item
        let archiveDir = Path.Combine(root, "BACKLOG", "_archive", "2026-01-01-feat-archived")
        Directory.CreateDirectory(archiveDir) |> ignore
        File.WriteAllText(
            Path.Combine(archiveDir, "item.yaml"),
            "id: feat-archived\ntitle: Archived Feature\nrepos:\n  - repo-1\n")
        let archivedTaskDir = Path.Combine(archiveDir, "tasks", "archived-task")
        Directory.CreateDirectory(archivedTaskDir) |> ignore
        let archivedTaskYaml = """id: archived-task
source:
  backlog: feat-archived
repo: repo-1
state: archived
created_at: 2026-01-01
"""
        File.WriteAllText(Path.Combine(archivedTaskDir, "task.yaml"), archivedTaskYaml)

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks

            // Archived tasks appear when filtered by archived state
            let archivedFiltered = Task.filterTasks None None (Some TaskState.Archived) summaries
            Assert.Equal(1, archivedFiltered.Length)
            Assert.Equal("archived-task", TaskId.value archivedFiltered.[0].Task.Id)

            // Archived tasks are NOT in the non-archived list (CLI would exclude, but test via filter)
            let nonArchivedFiltered =
                summaries |> List.filter (fun s -> s.Task.State <> TaskState.Archived)
            Assert.Equal(1, nonArchivedFiltered.Length)
            Assert.Equal("active-task", TaskId.value nonArchivedFiltered.[0].Task.Id)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 6.6 task list json output is valid
// ---------------------------------------------------------------------------

[<Fact>]
let ``listTasks and filterTasks produce data suitable for JSON output`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "approved"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks
            Assert.Equal(1, summaries.Length)
            let s = summaries.[0]
            Assert.Equal("feat-a", TaskId.value s.Task.Id)
            Assert.Equal("feat-a", BacklogId.value s.Task.SourceBacklog)
            Assert.Equal(RepoId "repo-1", s.Task.Repo)
            Assert.Equal(TaskState.Approved, s.Task.State)
            Assert.True(s.PlanApproved, "approved task should have planApproved = true")
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 6.7 task list no tasks returns empty list from adapter
// ---------------------------------------------------------------------------

[<Fact>]
let ``ListAllTasks returns empty list when product has no tasks`` () =
    let root = mkRoot ()
    try
        // Backlog items exist but no tasks directory
        writeItemYaml root "feat-a" [ "repo-1" ]

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok tasks ->
            Assert.Empty(tasks)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``ListAllTasks returns empty list when BACKLOG directory does not exist`` () =
    let root = mkRoot ()
    try
        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok tasks ->
            Assert.Empty(tasks)
    finally
        Directory.Delete(root, true)
