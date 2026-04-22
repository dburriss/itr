module Itr.Tests.Acceptance.ViewListAcceptanceTests

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
    let root = Path.Combine(Path.GetTempPath(), $"itr-view-list-tests-{Guid.NewGuid():N}")
    Directory.CreateDirectory(root) |> ignore
    root

let private writeViewYaml (coordRoot: string) (viewId: string) (description: string option) (items: string list) =
    let viewsDir = Path.Combine(coordRoot, "BACKLOG", "_views")
    Directory.CreateDirectory(viewsDir) |> ignore
    let descLine =
        match description with
        | Some d -> $"description: {d}\n"
        | None -> ""
    let itemLines =
        if items.IsEmpty then ""
        else
            let lines = items |> List.map (fun i -> $"  - {i}") |> String.concat "\n"
            $"items:\n{lines}\n"
    let yaml = $"id: {viewId}\n{descLine}{itemLines}"
    File.WriteAllText(Path.Combine(viewsDir, $"{viewId}.yaml"), yaml)

let private writeArchivedItemYaml (coordRoot: string) (backlogId: string) =
    let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive", $"2026-01-01-{backlogId}")
    Directory.CreateDirectory(archiveDir) |> ignore
    let yaml = $"id: {backlogId}\ntitle: Archived {backlogId}\nrepos:\n  - repo-1\n"
    File.WriteAllText(Path.Combine(archiveDir, "item.yaml"), yaml)

// ---------------------------------------------------------------------------
// 4.1 Normal operation: multiple views in table format
// ---------------------------------------------------------------------------

[<Fact>]
let ``ListViews returns multiple views with correct item counts`` () =
    let root = mkRoot ()
    try
        writeViewYaml root "sprint-1" (Some "Sprint 1 scope") ["feat-a"; "feat-b"; "feat-c"]
        writeViewYaml root "sprint-2" (Some "Sprint 2 scope") ["feat-d"]

        let viewStore = ViewStoreAdapter() :> IViewStore
        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            Assert.Equal(2, views.Length)
            let v1 = views |> List.find (fun v -> v.Id = "sprint-1")
            Assert.Equal(Some "Sprint 1 scope", v1.Description)
            Assert.Equal(3, v1.Items.Length)
            let v2 = views |> List.find (fun v -> v.Id = "sprint-2")
            Assert.Equal(1, v2.Items.Length)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.2 Empty view list: no views defined
// ---------------------------------------------------------------------------

[<Fact>]
let ``ListViews returns empty list when no views directory exists`` () =
    let root = mkRoot ()
    try
        let viewStore = ViewStoreAdapter() :> IViewStore
        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            Assert.Empty(views)
    finally
        Directory.Delete(root, true)

[<Fact>]
let ``ListViews returns empty list when views directory is empty`` () =
    let root = mkRoot ()
    try
        let viewsDir = Path.Combine(root, "BACKLOG", "_views")
        Directory.CreateDirectory(viewsDir) |> ignore

        let viewStore = ViewStoreAdapter() :> IViewStore
        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            Assert.Empty(views)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.3 JSON output format
// ---------------------------------------------------------------------------

[<Fact>]
let ``view list JSON output fields are correct`` () =
    let root = mkRoot ()
    try
        writeViewYaml root "my-view" (Some "A test view") ["item-1"; "item-2"]
        writeArchivedItemYaml root "item-1"

        let viewStore = ViewStoreAdapter() :> IViewStore
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore

        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            match backlogStore.ListArchivedBacklogItems root with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok archivedItems ->
                let archivedIds =
                    archivedItems |> List.map (fun (item, _) -> BacklogId.value item.Id) |> Set.ofList

                let view = views |> List.find (fun v -> v.Id = "my-view")
                let total = view.Items.Length
                let archived =
                    view.Items |> List.filter (fun id -> archivedIds.Contains(id)) |> List.length

                // Simulate what the JSON handler would produce
                let description = view.Description |> Option.defaultValue ""
                let line =
                    sprintf "  { \"id\": \"%s\", \"description\": \"%s\", \"items\": %d, \"archived\": %d }"
                        view.Id description total archived

                Assert.Equal("my-view", view.Id)
                Assert.Equal(2, total)
                Assert.Equal(1, archived)
                Assert.Contains("\"id\": \"my-view\"", line)
                Assert.Contains("\"items\": 2", line)
                Assert.Contains("\"archived\": 1", line)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.4 Text output format
// ---------------------------------------------------------------------------

[<Fact>]
let ``view list text output is tab-separated with correct fields`` () =
    let root = mkRoot ()
    try
        writeViewYaml root "v1" (Some "View One") ["a"; "b"]

        let viewStore = ViewStoreAdapter() :> IViewStore
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore

        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            match backlogStore.ListArchivedBacklogItems root with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok archivedItems ->
                let archivedIds =
                    archivedItems |> List.map (fun (item, _) -> BacklogId.value item.Id) |> Set.ofList

                let view = views.[0]
                let description = view.Description |> Option.defaultValue ""
                let total = view.Items.Length
                let archived =
                    view.Items |> List.filter (fun id -> archivedIds.Contains(id)) |> List.length

                // Simulate text output
                let line = sprintf "%s\t%s\t%d\t%d" view.Id description total archived
                let parts = line.Split('\t')

                Assert.Equal(4, parts.Length)
                Assert.Equal("v1", parts.[0])
                Assert.Equal("View One", parts.[1])
                Assert.Equal("2", parts.[2])
                Assert.Equal("0", parts.[3])
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.5 View with no description renders as empty string
// ---------------------------------------------------------------------------

[<Fact>]
let ``view with no description field renders as empty string`` () =
    let root = mkRoot ()
    try
        writeViewYaml root "nodesc" None ["item-x"]

        let viewStore = ViewStoreAdapter() :> IViewStore
        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            Assert.Equal(1, views.Length)
            let view = views.[0]
            Assert.Equal("nodesc", view.Id)
            Assert.Equal(None, view.Description)

            // When rendered, description should be empty string (not null/None)
            let description = view.Description |> Option.defaultValue ""
            Assert.Equal("", description)
    finally
        Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 4.6 Archived item count computation
// ---------------------------------------------------------------------------

[<Fact>]
let ``archived item count reflects only items that appear in archived backlog`` () =
    let root = mkRoot ()
    try
        // View with 4 items; 2 are archived
        writeViewYaml root "mixed-view" (Some "Mixed") ["active-1"; "active-2"; "archived-a"; "archived-b"]
        writeArchivedItemYaml root "archived-a"
        writeArchivedItemYaml root "archived-b"

        let viewStore = ViewStoreAdapter() :> IViewStore
        let backlogStore = BacklogStoreAdapter() :> IBacklogStore

        match viewStore.ListViews root with
        | Error e -> failwithf "expected Ok, got Error: %A" e
        | Ok views ->
            match backlogStore.ListArchivedBacklogItems root with
            | Error e -> failwithf "expected Ok, got Error: %A" e
            | Ok archivedItems ->
                let archivedIds =
                    archivedItems |> List.map (fun (item, _) -> BacklogId.value item.Id) |> Set.ofList

                let view = views |> List.find (fun v -> v.Id = "mixed-view")
                let total = view.Items.Length
                let archived =
                    view.Items
                    |> List.filter (fun id -> archivedIds.Contains(id))
                    |> List.length

                Assert.Equal(4, total)
                Assert.Equal(2, archived)
    finally
        Directory.Delete(root, true)
