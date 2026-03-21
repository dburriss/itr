module Itr.Tests.Acceptance.YamlAdapterTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private mkRoot () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-adapter-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    root

let private mkBacklogId s =
    match BacklogId.tryCreate s with
    | Ok id -> id
    | Error e -> failwithf "invalid backlog id %s: %A" s e

let private mkTaskId s = TaskId.create s

let private writeTaskYaml (coordRoot: string) (backlogId: string) (taskFolderName: string) (taskId: string) =
    let taskDir = Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskFolderName)
    Directory.CreateDirectory(taskDir) |> ignore

    let yaml =
        $"""id: {taskId}
source:
  backlog: {backlogId}
repo: main-repo
state: planning
created_at: 2026-01-01
"""

    File.WriteAllText(Path.Combine(taskDir, "task.yaml"), yaml)

// ---------------------------------------------------------------------------
// 4.3 ListTasks: active and completed subdirectory formats
// ---------------------------------------------------------------------------

[<Fact>]
let ``ListTasks returns tasks from both active and dated subdirectories`` () =
    let root = mkRoot ()

    try
        let bid = "my-feature"
        // active task folder (undated)
        writeTaskYaml root bid "my-feature" "my-feature"
        // completed task folder (date-prefixed)
        writeTaskYaml root bid "2026-01-15-my-feature" "my-feature"

        let store = TaskStoreAdapter() :> ITaskStore
        let backlogId = mkBacklogId bid

        match store.ListTasks root backlogId with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok tasks -> Assert.Equal(2, tasks.Length)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``ListTasks returns empty list when tasks directory does not exist`` () =
    let root = mkRoot ()

    try
        // Create backlog folder but no tasks subdirectory
        Directory.CreateDirectory(Path.Combine(root, "BACKLOG", "my-feature")) |> ignore

        let store = TaskStoreAdapter() :> ITaskStore
        let backlogId = mkBacklogId "my-feature"

        match store.ListTasks root backlogId with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok tasks -> Assert.Empty(tasks)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.4 WriteTask: creates intermediate tasks/<task-id>/ directory
// ---------------------------------------------------------------------------

[<Fact>]
let ``WriteTask creates intermediate task subfolder before writing`` () =
    let root = mkRoot ()

    try
        // Create backlog item folder (tasks/ does not yet exist)
        Directory.CreateDirectory(Path.Combine(root, "BACKLOG", "my-feature")) |> ignore

        let store = TaskStoreAdapter() :> ITaskStore
        let backlogId = mkBacklogId "my-feature"

        let task =
            { Id = mkTaskId "my-feature"
              SourceBacklog = backlogId
              Repo = RepoId "main-repo"
              State = Planning
              CreatedAt = DateOnly(2026, 1, 1) }

        match store.WriteTask root task with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok() ->
            let expectedPath =
                Path.Combine(root, "BACKLOG", "my-feature", "tasks", "my-feature", "task.yaml")

            Assert.True(File.Exists(expectedPath), $"Expected task file at: {expectedPath}")
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.5 ArchiveBacklogItem: moves folder to archive/<date>-<id>/
// ---------------------------------------------------------------------------

[<Fact>]
let ``ArchiveBacklogItem moves BACKLOG folder to archive subfolder with date prefix`` () =
    let root = mkRoot ()

    try
        let bid = "my-feature"
        // Create backlog item with a completed task
        writeTaskYaml root bid "2026-01-15-my-feature" "my-feature"

        File.WriteAllText(
            Path.Combine(root, "BACKLOG", bid, "item.yaml"),
            $"id: {bid}\ntitle: My Feature\nrepos:\n  - main-repo\n"
        )

        let store = BacklogStoreAdapter() :> IBacklogStore
        let backlogId = mkBacklogId bid

        match store.ArchiveBacklogItem root backlogId "2026-03-20" with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok() ->
            let archivedPath = Path.Combine(root, "BACKLOG", "archive", "2026-03-20-my-feature")
            Assert.True(Directory.Exists(archivedPath), $"Expected archive directory at: {archivedPath}")
            let originalPath = Path.Combine(root, "BACKLOG", bid)
            Assert.False(Directory.Exists(originalPath), $"Original directory should be gone: {originalPath}")
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.6 ArchiveBacklogItem: fails when active (undated) task folder exists
// ---------------------------------------------------------------------------

[<Fact>]
let ``ArchiveBacklogItem returns error when backlog item directory does not exist`` () =
    let root = mkRoot ()

    try
        let store = BacklogStoreAdapter() :> IBacklogStore
        let backlogId = mkBacklogId "nonexistent"

        match store.ArchiveBacklogItem root backlogId "2026-03-20" with
        | Error(BacklogItemNotFound id) -> Assert.Equal(backlogId, id)
        | other -> failwithf "expected BacklogItemNotFound, got %A" other
    finally
        Directory.Delete(root, true)
