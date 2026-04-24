module Itr.Tests.Acceptance.TestDoubles

open System.Collections.Generic
open Itr.Domain

// ---------------------------------------------------------------------------
// In-memory filesystem double
// ---------------------------------------------------------------------------

/// In-memory file system for acceptance tests.
/// Stores file content by absolute path. Does not model directories.
type InMemoryFileSystem() =
    let files = Dictionary<string, string>()

    member _.Write (path: string) (content: string) = files.[path] <- content

    member _.Read(path: string) =
        if files.ContainsKey(path) then
            Ok files.[path]
        else
            Error(FileNotFound path)

    member _.Exists(path: string) = files.ContainsKey(path)

    member _.DirectoryExists(path: string) =
        files.Keys |> Seq.exists (fun k -> k.StartsWith(path))

    member _.AllFiles() =
        files |> Seq.map (fun kv -> kv.Key, kv.Value) |> Seq.toList

    interface IFileSystem with
        member this.ReadFile path = this.Read path

        member this.WriteFile path content =
            this.Write path content
            Ok()

        member this.FileExists path = this.Exists path
        member this.DirectoryExists path = this.DirectoryExists path

// ---------------------------------------------------------------------------
// In-memory task store double
// ---------------------------------------------------------------------------

/// In-memory task store for acceptance tests.
type InMemoryTaskStore() =
    let tasks = Dictionary<string, ItrTask * string>() // key = taskId value

    member _.Add (coordRoot: string) (task: ItrTask) =
        let path = ItrTask.taskFile coordRoot task.SourceBacklog task.Id
        tasks.[TaskId.value task.Id] <- (task, path)

    interface ITaskStore with
        member _.ListTasks coordRoot backlogId =
            let matching =
                tasks.Values
                |> Seq.filter (fun (t, _) -> t.SourceBacklog = backlogId)
                |> Seq.toList

            Ok matching

        member _.ListArchivedTasks _coordRoot _backlogId = Ok []

        member this.WriteTask coordRoot task =
            this.Add coordRoot task
            Ok()

        member _.ArchiveTask _coordRoot _backlogId _taskId _date = Ok()

        member _.ListAllTasks _coordRoot = Ok(tasks.Values |> Seq.toList)

// ---------------------------------------------------------------------------
// In-memory backlog store double
// ---------------------------------------------------------------------------

/// In-memory backlog store for acceptance tests.
type InMemoryBacklogStore() =
    let items = Dictionary<string, BacklogItem * string>() // key = BacklogId value

    member _.Add (coordRoot: string) (item: BacklogItem) =
        let path = BacklogItem.itemFile coordRoot item.Id
        items.[BacklogId.value item.Id] <- (item, path)

    member _.Get(backlogId: BacklogId) =
        let key = BacklogId.value backlogId
        if items.ContainsKey(key) then Some items.[key] else None

    interface IBacklogStore with
        member this.LoadBacklogItem _coordRoot backlogId =
            let key = BacklogId.value backlogId

            if items.ContainsKey(key) then
                Ok items.[key]
            else
                Error(BacklogItemNotFound backlogId)

        member _.LoadArchivedBacklogItem _coordRoot _backlogId = Ok None

        member _.ArchiveBacklogItem _coordRoot _backlogId _date = Ok()

        member _.BacklogItemExists _coordRoot backlogId =
            items.ContainsKey(BacklogId.value backlogId)

        member this.WriteBacklogItem coordRoot item =
            this.Add coordRoot item
            Ok()

        member _.ListBacklogItems _coordRoot = Ok(items.Values |> Seq.toList)

        member _.ListArchivedBacklogItems _coordRoot = Ok []

// ---------------------------------------------------------------------------
// In-memory view store double
// ---------------------------------------------------------------------------

/// In-memory view store for acceptance tests.
type InMemoryViewStore() =
    let views = System.Collections.Generic.List<BacklogView>()

    member _.Add(view: BacklogView) = views.Add(view)

    interface IViewStore with
        member _.ListViews _coordRoot = Ok(views |> Seq.toList)

// ---------------------------------------------------------------------------
// In-memory portfolio config double
// ---------------------------------------------------------------------------

/// In-memory portfolio config for acceptance tests.
type InMemoryPortfolioConfig(configPath: string) =
    let mutable portfolio: Portfolio option = None

    member _.SetPortfolio(p: Portfolio) = portfolio <- Some p

    interface IPortfolioConfig with
        member _.ConfigPath() = configPath

        member _.LoadConfig _path =
            match portfolio with
            | Some p -> Ok p
            | None ->
                Ok
                    { DefaultProfile = None
                      Profiles = Map.empty }

        member _.SaveConfig _path p =
            portfolio <- Some p
            Ok()

// ---------------------------------------------------------------------------
// In-memory product config double
// ---------------------------------------------------------------------------

/// In-memory product config for acceptance tests.
type InMemoryProductConfig() =
    let products = Dictionary<string, ProductDefinition>()

    member _.Register (root: string) (def: ProductDefinition) = products.[root] <- def

    interface IProductConfig with
        member _.LoadProductConfig productRoot =
            if products.ContainsKey(productRoot) then
                Ok products.[productRoot]
            else
                Error(ProductConfigError(productRoot, "product.yaml not found"))

// ---------------------------------------------------------------------------
// In-memory environment double
// ---------------------------------------------------------------------------

/// Stub environment for acceptance tests.
type StubEnvironment(homeDir: string, ?envVars: Map<string, string>) =
    let vars = defaultArg envVars Map.empty

    interface IEnvironment with
        member _.GetEnvVar name = vars |> Map.tryFind name
        member _.HomeDirectory() = homeDir

// ---------------------------------------------------------------------------
// Spy agent harness double
// ---------------------------------------------------------------------------

/// Spy agent harness — records prompts, returns a configured response.
type SpyAgentHarness(?response: string) =
    let prompts = System.Collections.Generic.List<string>()
    let configured = defaultArg response "# Plan\n\nGenerated plan.\n"

    member _.RecordedPrompts = prompts |> Seq.toList

    member _.LastPrompt =
        if prompts.Count > 0 then
            Some prompts.[prompts.Count - 1]
        else
            None

    interface IAgentHarness with
        member _.Prompt prompt _debug =
            prompts.Add(prompt)
            Ok configured
