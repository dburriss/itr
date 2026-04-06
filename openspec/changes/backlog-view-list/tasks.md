## 1. Arg Types

- [ ] 1.1 Add `ViewListArgs` DU to `Program.fs` with `Output` and optional `Product` flags, following `TaskListArgs` pattern
- [ ] 1.2 Add `ViewArgs` DU to `Program.fs` with a single `List` case decorated with `[<CliPrefix(CliPrefix.None)>]`
- [ ] 1.3 Add `| [<CliPrefix(CliPrefix.None)>] View of ParseResults<ViewArgs>` to `CliArgs`

## 2. Handler Implementation

- [ ] 2.1 Create `handleViewList` function that resolves product from `--product` flag or working directory
- [ ] 2.2 Call `viewStore.ListViews(coordRoot)` to retrieve all views
- [ ] 2.3 Call `backlogStore.ListArchivedBacklogItems(coordRoot)` and build a `Set<string>` of archived item IDs
- [ ] 2.4 For each view, compute `total = view.Items.Length` and `archived = view.Items |> List.filter (fun id -> archivedIds.Contains(id)) |> List.length`
- [ ] 2.5 Implement table output using Spectre.Console `Table` with columns: Id, Description, Items, Archived
- [ ] 2.6 Implement JSON output as a JSON array with `id`, `description`, `items`, `archived` fields per element
- [ ] 2.7 Implement text output as tab-separated lines: id, description, items count, archived count
- [ ] 2.8 Handle empty view list with a friendly message (all three output formats)

## 3. Routing

- [ ] 3.1 Add `View` arm to `dispatch` that matches on `ViewArgs.List` and calls `handleViewList`

## 4. Tests

- [ ] 4.1 Write test for normal operation: multiple views returned in table format
- [ ] 4.2 Write test for empty view list: friendly message displayed
- [ ] 4.3 Write test for JSON output format
- [ ] 4.4 Write test for text output format
- [ ] 4.5 Write test for view with no description (blank description rendered)
- [ ] 4.6 Write test for archived item count computation
