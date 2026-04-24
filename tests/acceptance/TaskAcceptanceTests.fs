module Itr.Tests.Acceptance.TaskAcceptanceTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter

// ---------------------------------------------------------------------------
// Fixture
// ---------------------------------------------------------------------------

type TaskFixture() =
    // product root = root; coord root = root/.itr
    let root = Path.Combine(Path.GetTempPath(), $"itr-task-tests-{Guid.NewGuid():N}")
    let coordRoot = Path.Combine(root, ".itr")

    do
        Directory.CreateDirectory(coordRoot) |> ignore

        Directory.CreateDirectory(Path.Combine(coordRoot, "BACKLOG", "my-feature"))
        |> ignore

        Directory.CreateDirectory(Path.Combine(coordRoot, "BACKLOG", "cross-feature"))
        |> ignore

        // product.yaml lives at the product root (not coordRoot)
        // Uses multi-repo so both single- and multi-repo tests can load it.
        let productYaml =
            """id: test-product
repos:
  main-repo:
    path: ./
  second-repo:
    path: ../second
coordination:
  mode: standalone
  path: .itr
"""

        File.WriteAllText(Path.Combine(root, "product.yaml"), productYaml)

        // Single-repo backlog item
        let singleRepoItem =
            """id: my-feature
title: My Feature
repos:
  - main-repo
"""

        File.WriteAllText(Path.Combine(coordRoot, "BACKLOG", "my-feature", "item.yaml"), singleRepoItem)

        // Multi-repo backlog item
        let multiRepoItem =
            """id: cross-feature
title: Cross Feature
repos:
  - main-repo
  - second-repo
"""

        File.WriteAllText(Path.Combine(coordRoot, "BACKLOG", "cross-feature", "item.yaml"), multiRepoItem)

    member _.Root = root
    member _.CoordRoot = coordRoot

    interface IDisposable with
        member _.Dispose() =
            if Directory.Exists(root) then
                Directory.Delete(root, true)

let private toProductConfig (def: ProductDefinition) : ProductConfig =
    let repos =
        def.Repos |> Map.toSeq |> Seq.map (fun (k, v) -> RepoId k, v) |> Map.ofSeq

    { Id = def.Id; Repos = repos }

let private runTake productRoot coordRoot backlogIdStr (taskIdOverride: string option) =
    let productStore = ProductConfigAdapter() :> IProductConfig
    let backlogStore = BacklogStoreAdapter() :> IBacklogStore
    let taskStore = TaskStoreAdapter() :> ITaskStore

    match BacklogId.tryCreate backlogIdStr with
    | Error e -> Error(sprintf "invalid backlog id: %A" e)
    | Ok backlogId ->

        let today = DateOnly.FromDateTime(DateTime.UtcNow)

        productStore.LoadProductConfig productRoot
        |> Result.mapError (sprintf "%A")
        |> Result.bind (fun definition ->
            let productConfig = toProductConfig definition

            backlogStore.LoadBacklogItem coordRoot backlogId
            |> Result.mapError (sprintf "%A")
            |> Result.bind (fun (backlogItem, _) ->
                taskStore.ListTasks coordRoot backlogId
                |> Result.mapError (sprintf "%A")
                |> Result.bind (fun existingTaskTuples ->
                    let existingTasks = existingTaskTuples |> List.map fst

                    let input =
                        { Tasks.Take.Input.BacklogId = backlogId
                          Tasks.Take.Input.TaskIdOverride = taskIdOverride }

                    Tasks.Take.execute productConfig backlogItem existingTasks input today
                    |> Result.mapError (sprintf "%A")
                    |> Result.bind (fun newTasks ->
                        let errors =
                            newTasks
                            |> List.map (fun task ->
                                taskStore.WriteTask coordRoot task
                                |> Result.mapError (sprintf "%A")
                                |> Result.map (fun () -> task))
                            |> List.choose (function
                                | Error e -> Some e
                                | Ok _ -> None)

                        match errors with
                        | e :: _ -> Error e
                        | [] -> Ok newTasks))))

// ---------------------------------------------------------------------------
// Acceptance tests (6.5)
// ---------------------------------------------------------------------------

[<Fact>]
let ``take single-repo item creates task file with correct YAML content`` () =
    use fixture = new TaskFixture()
    let coordRoot = fixture.CoordRoot

    match runTake fixture.Root coordRoot "my-feature" None with
    | Error msg -> failwithf "expected success, got error: %s" msg
    | Ok tasks ->
        Assert.Equal(1, tasks.Length)
        let task = tasks.[0]
        Assert.Equal("my-feature", TaskId.value task.Id)
        Assert.Equal(RepoId "main-repo", task.Repo)
        Assert.Equal(TaskState.Planning, task.State)

        // Check file exists on disk
        let taskPath =
            Path.Combine(coordRoot, "BACKLOG", "my-feature", "tasks", "my-feature", "task.yaml")

        Assert.True(File.Exists(taskPath), $"Expected task file at: {taskPath}")

        // Check YAML content
        let yaml = File.ReadAllText(taskPath)
        Assert.Contains("id: my-feature", yaml)
        Assert.Contains("state: planning", yaml)
        Assert.Contains("repo: main-repo", yaml)
        Assert.Contains("backlog: my-feature", yaml)

// ---------------------------------------------------------------------------
// Acceptance tests (6.6)
// ---------------------------------------------------------------------------

[<Fact>]
let ``re-take produces additional task file with repo-prefixed id`` () =
    use fixture = new TaskFixture()
    let coordRoot = fixture.CoordRoot

    // First take
    match runTake fixture.Root coordRoot "my-feature" None with
    | Error msg -> failwithf "first take failed: %s" msg
    | Ok firstTasks ->
        Assert.Equal(1, firstTasks.Length)
        Assert.Equal("my-feature", TaskId.value firstTasks.[0].Id)

    // Second take (re-take)
    match runTake fixture.Root coordRoot "my-feature" None with
    | Error msg -> failwithf "re-take failed: %s" msg
    | Ok secondTasks ->
        Assert.Equal(1, secondTasks.Length)
        Assert.Equal("main-repo-my-feature", TaskId.value secondTasks.[0].Id)

        // Both files should exist
        let first =
            Path.Combine(coordRoot, "BACKLOG", "my-feature", "tasks", "my-feature", "task.yaml")

        let second =
            Path.Combine(coordRoot, "BACKLOG", "my-feature", "tasks", "main-repo-my-feature", "task.yaml")

        Assert.True(File.Exists(first), $"first task file missing: {first}")
        Assert.True(File.Exists(second), $"second task file missing: {second}")
