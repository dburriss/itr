module Itr.Tests.Acceptance.TestBuilders

open System
open Itr.Domain

// ---------------------------------------------------------------------------
// A.<Thing> — default test data builders
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module A =
    let backlogId str = BacklogId.tryCreate str |> Result.defaultWith (fun _ -> failwithf "invalid backlog id: %s" str)
    let repoId str = RepoId str
    let taskId str = TaskId.create str
    let today () = DateOnly.FromDateTime(DateTime.UtcNow)

    let backlogItem (id: BacklogId) : BacklogItem =
        { Id = id
          Title = "Test Feature"
          Repos = [ RepoId "main-repo" ]
          Type = Feature
          Priority = None
          Summary = None
          AcceptanceCriteria = []
          Dependencies = []
          CreatedAt = today() }

    let task (id: TaskId) (backlogId: BacklogId) : ItrTask =
        { Id = id
          SourceBacklog = backlogId
          Repo = RepoId "main-repo"
          State = TaskState.Planning
          CreatedAt = today() }

    let taskInState (id: TaskId) (backlogId: BacklogId) (state: TaskState) : ItrTask =
        { task id backlogId with State = state }

    let productDefinition (id: string) : ProductDefinition =
        { Id = ProductId.tryCreate id |> Result.defaultWith (fun _ -> failwithf "invalid product id: %s" id)
          Description = None
          Repos = Map.ofList [ "main-repo", { Path = "./"; Url = None } ]
          Coordination = { Mode = "standalone"; Path = Some ".itr"; Repo = None }
          Docs = Map.empty
          CoordRoot = { Mode = Standalone; AbsolutePath = ".itr" } }

// ---------------------------------------------------------------------------
// Given.<Thing> — dependency setup helpers
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module Given =
    open TestDoubles

    /// Convenience: return a coordRoot string for a temp test root.
    let coordRoot (root: string) = root

    /// Seed an in-memory task store with a list of tasks.
    let tasksInStore (store: InMemoryTaskStore) (coordRoot: string) (tasks: ItrTask list) =
        tasks |> List.iter (store.Add coordRoot)
        store

    /// Seed an in-memory backlog store with a list of items.
    let itemsInStore (store: InMemoryBacklogStore) (coordRoot: string) (items: BacklogItem list) =
        items |> List.iter (store.Add coordRoot)
        store

    /// Seed the in-memory filesystem with a plan file for a given task.
    let planFile (fs: InMemoryFileSystem) (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) (content: string) =
        let path = ItrTask.planFile coordRoot backlogId taskId
        fs.Write path content
        fs
