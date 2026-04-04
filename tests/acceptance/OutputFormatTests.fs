module Itr.Tests.Acceptance.OutputFormatTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter
open Itr.Features

// ---------------------------------------------------------------------------
// Helpers (shared with other acceptance tests)
// ---------------------------------------------------------------------------

let private mkRoot () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-output-format-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    root

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
    let yaml = $"id: {backlogId}\ntitle: Item {backlogId}\nrepos:\n{repoLines}\n"
    File.WriteAllText(Path.Combine(dir, "item.yaml"), yaml)

let private writeItemYamlFull (coordRoot: string) (backlogId: string) (repos: string list) (priority: string option) =
    let dir = Path.Combine(coordRoot, "BACKLOG", backlogId)
    Directory.CreateDirectory(dir) |> ignore
    let repoLines = repos |> List.map (fun r -> $"  - {r}") |> String.concat "\n"
    let priorityLine =
        match priority with
        | Some p -> $"priority: {p}\n"
        | None -> ""
    let yaml = $"id: {backlogId}\ntitle: Item {backlogId}\nrepos:\n{repoLines}\n{priorityLine}"
    File.WriteAllText(Path.Combine(dir, "item.yaml"), yaml)

let private taskStateToString (state: TaskState) =
    match state with
    | TaskState.Planning -> "planning"
    | TaskState.Planned -> "planned"
    | TaskState.Approved -> "approved"
    | TaskState.InProgress -> "in_progress"
    | TaskState.Implemented -> "implemented"
    | TaskState.Validated -> "validated"
    | TaskState.Archived -> "archived"

let private backlogStatusToString (s: BacklogItemStatus) =
    match s with
    | BacklogItemStatus.Created -> "created"
    | BacklogItemStatus.Planning -> "planning"
    | BacklogItemStatus.Planned -> "planned"
    | BacklogItemStatus.Approved -> "approved"
    | BacklogItemStatus.InProgress -> "in-progress"
    | BacklogItemStatus.Completed -> "completed"
    | BacklogItemStatus.Archived -> "archived"

// ---------------------------------------------------------------------------
// 8.1 task list text output — id, backlog, repo, state, planApproved
// ---------------------------------------------------------------------------

[<Fact>]
let ``task list text output contains tab-separated fields`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks

            // Simulate text output: <id>\t<backlog>\t<repo>\t<state>\t<planApproved>
            let lines =
                summaries
                |> List.map (fun s ->
                    let id = TaskId.value s.Task.Id
                    let backlog = BacklogId.value s.Task.SourceBacklog
                    let repo = RepoId.value s.Task.Repo
                    let state = taskStateToString s.Task.State
                    let planApproved = if s.PlanApproved then "yes" else "no"
                    sprintf "%s\t%s\t%s\t%s\t%s" id backlog repo state planApproved)

            Assert.Equal(1, lines.Length)
            let line = lines.[0]
            // Must contain tabs
            Assert.Contains("\t", line)
            // Must have exactly 4 tabs (5 fields)
            let parts = line.Split('\t')
            Assert.Equal(5, parts.Length)
            Assert.Equal("feat-a", parts.[0]) // id
            Assert.Equal("feat-a", parts.[1]) // backlog
            Assert.Equal("repo-1", parts.[2]) // repo
            Assert.Equal("planning", parts.[3]) // state
            Assert.Equal("no", parts.[4])      // planApproved
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 8.2 backlog list text output — id, type, priority, status, view, tasks, created, title
// ---------------------------------------------------------------------------

[<Fact>]
let ``backlog list text output contains tab-separated fields`` () =
    let root = mkRoot ()
    try
        writeItemYamlFull root "feat-a" [ "repo-1" ] (Some "high")

        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = ViewStoreAdapter() :> IViewStore

        match Backlog.loadSnapshot backlogStore taskStore viewStore root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok snapshot ->
            let items = Backlog.listBacklogItems { ViewId = None; Status = None; ItemType = None } snapshot

            // Simulate text output: <id>\t<type>\t<priority>\t<status>\t<view>\t<tasks>\t<created>\t<title>
            let lines =
                items
                |> List.map (fun s ->
                    let id = BacklogId.value s.Item.Id
                    let itemType = BacklogItemType.toString s.Item.Type
                    let priority = s.Item.Priority |> Option.defaultValue "-"
                    let status = backlogStatusToString s.Status
                    let viewId = s.ViewId |> Option.defaultValue "-"
                    let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")
                    let title = s.Item.Title
                    sprintf "%s\t%s\t%s\t%s\t%s\t%d\t%s\t%s" id itemType priority status viewId s.TaskCount createdAt title)

            Assert.Equal(1, lines.Length)
            let line = lines.[0]
            Assert.Contains("\t", line)
            let parts = line.Split('\t')
            Assert.Equal(8, parts.Length)
            Assert.Equal("feat-a", parts.[0])    // id
            Assert.Equal("high", parts.[2])      // priority (index 2, before status)
            Assert.Equal("Item feat-a", parts.[7]) // title at end
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 8.3 task info text output — id, backlog, repo, state, plan exists, plan approved, created, siblings
// ---------------------------------------------------------------------------

[<Fact>]
let ``task info text output contains key-value lines`` () =
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
            match Task.getTaskDetail taskId allTasksList taskYamlPath with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                let task = detail.Task
                let createdAt = task.CreatedAt.ToString("yyyy-MM-dd")
                let planExistsStr = if detail.PlanExists then "yes" else "no"
                let planApprovedStr = if detail.PlanApproved then "yes" else "no"
                let siblingsStr =
                    if detail.Siblings.IsEmpty then "-"
                    else detail.Siblings |> List.map (fun s -> TaskId.value s.Id) |> String.concat ","

                // Simulate text output: key\tvalue lines
                let lines =
                    [ sprintf "id\t%s" (TaskId.value task.Id)
                      sprintf "backlog\t%s" (BacklogId.value task.SourceBacklog)
                      sprintf "repo\t%s" (RepoId.value task.Repo)
                      sprintf "state\t%s" (taskStateToString task.State)
                      sprintf "plan exists\t%s" planExistsStr
                      sprintf "plan approved\t%s" planApprovedStr
                      sprintf "created\t%s" createdAt
                      sprintf "siblings\t%s" siblingsStr ]

                Assert.Equal(8, lines.Length)
                // Each line must have exactly one tab
                for line in lines do
                    Assert.Contains("\t", line)

                let toMap = lines |> List.map (fun l -> let p = l.Split('\t') in (p.[0], p.[1])) |> Map.ofList
                Assert.Equal("feat-a", toMap.["id"])
                Assert.Equal("feat-a", toMap.["backlog"])
                Assert.Equal("repo-1", toMap.["repo"])
                Assert.Equal("approved", toMap.["state"])
                Assert.Equal("no", toMap.["plan exists"])
                Assert.Equal("yes", toMap.["plan approved"])
                Assert.Equal("-", toMap.["siblings"])
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 8.4 backlog info text output — id, title, type, priority, status, view, summary,
//     acceptance criteria, dependencies, repos, created, taskCount, tasks
// ---------------------------------------------------------------------------

[<Fact>]
let ``backlog info text output contains key-value lines`` () =
    let root = mkRoot ()
    try
        writeItemYamlFull root "feat-a" [ "repo-1" ] (Some "medium")

        let backlogStore = BacklogStoreAdapter() :> IBacklogStore
        let taskStore = TaskStoreAdapter() :> ITaskStore
        let viewStore = ViewStoreAdapter() :> IViewStore

        match BacklogId.tryCreate "feat-a" with
        | Error _ -> failwith "bad id"
        | Ok backlogId ->
            match Backlog.getBacklogItemDetail backlogStore taskStore viewStore root backlogId with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok detail ->
                let item = detail.Item
                let id = BacklogId.value item.Id
                let itemType = BacklogItemType.toString item.Type
                let status = backlogStatusToString detail.Status
                let priorityStr = item.Priority |> Option.defaultValue "-"
                let viewStr = detail.ViewId |> Option.defaultValue "-"
                let repos = item.Repos |> List.map RepoId.value
                let deps_ = item.Dependencies |> List.map BacklogId.value
                let reposStr = if repos.IsEmpty then "-" else String.concat "," repos
                let depsStr = if deps_.IsEmpty then "-" else String.concat "," deps_
                let summary = item.Summary |> Option.defaultValue ""
                let ac = item.AcceptanceCriteria
                let summaryStr = summary.Replace('\n', ' ').Replace('\r', ' ')
                let acStr =
                    if ac.IsEmpty then "-"
                    else ac |> List.map (fun s -> s.Replace('\n', ' ').Replace('\r', ' ')) |> String.concat ","
                let tasksStr =
                    if detail.Tasks.IsEmpty then "-"
                    else detail.Tasks |> List.map (fun t -> TaskId.value t.Id) |> String.concat ","
                let createdAt = item.CreatedAt.ToString("yyyy-MM-dd")

                // Simulate text output (matches handler order)
                let lines =
                    [ sprintf "id\t%s" id
                      sprintf "title\t%s" item.Title
                      sprintf "type\t%s" itemType
                      sprintf "priority\t%s" priorityStr
                      sprintf "status\t%s" status
                      sprintf "view\t%s" viewStr
                      sprintf "summary\t%s" summaryStr
                      sprintf "acceptance criteria\t%s" acStr
                      sprintf "dependencies\t%s" depsStr
                      sprintf "repos\t%s" reposStr
                      sprintf "created\t%s" createdAt
                      sprintf "taskCount\t%d" detail.Tasks.Length
                      sprintf "tasks\t%s" tasksStr ]

                Assert.Equal(13, lines.Length)

                for line in lines do
                    Assert.Contains("\t", line)

                let toMap = lines |> List.map (fun l -> let p = l.Split('\t') in (p.[0], p.[1])) |> Map.ofList
                Assert.Equal("feat-a", toMap.["id"])
                Assert.Equal("Item feat-a", toMap.["title"])
                Assert.Equal("medium", toMap.["priority"])
                Assert.Equal("repo-1", toMap.["repos"])
                Assert.Equal("-", toMap.["dependencies"])
                Assert.Equal("-", toMap.["tasks"])
                // 'created' key (not 'createdAt')
                Assert.True(toMap.ContainsKey("created"), "should have 'created' key (not 'createdAt')")
                Assert.False(toMap.ContainsKey("createdAt"), "should not have 'createdAt' key")
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 8.5 text output has no ANSI sequences
// ---------------------------------------------------------------------------

[<Fact>]
let ``text output has no ANSI sequences`` () =
    let root = mkRoot ()
    try
        writeItemYaml root "feat-a" [ "repo-1" ]
        writeTaskYaml root "feat-a" "feat-a" "feat-a" "feat-a" "repo-1" "planning"

        let store = TaskStoreAdapter() :> ITaskStore
        match store.ListAllTasks root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok allTasks ->
            let summaries = Task.listTasks allTasks

            // Build text output lines exactly as the CLI does (id, backlog, repo, state, planApproved)
            let lines =
                summaries
                |> List.map (fun s ->
                    let id = TaskId.value s.Task.Id
                    let backlog = BacklogId.value s.Task.SourceBacklog
                    let repo = RepoId.value s.Task.Repo
                    let state = taskStateToString s.Task.State
                    let planApproved = if s.PlanApproved then "yes" else "no"
                    sprintf "%s\t%s\t%s\t%s\t%s" id backlog repo state planApproved)

            let output = String.concat "\n" lines

            // ESC character (ANSI escape prefix) must not appear
            let hasAnsi = output |> Seq.exists (fun c -> int c = 0x1B)
            Assert.False(hasAnsi, $"Output should not contain ANSI escape sequences, but got: {output}")
    finally
        Directory.Delete(root, true)
