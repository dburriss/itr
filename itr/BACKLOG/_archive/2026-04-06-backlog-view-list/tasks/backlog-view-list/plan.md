# List backlog views for a product

**Task ID:** backlog-view-list
**Backlog Item:** backlog-view-list
**Repo:** itr

## Description

Allow users to list all named backlog views defined for a product, giving a quick overview of the views available for delivery planning.
Outcome: A new CLI command `itr view list` that displays the ID and description of each view defined in the product's `BACKLOG/_views/` directory, with support for table, JSON, and text output formats.

## Scope

**Included:**
- A new CLI command to list all view YAML files in the current product's `BACKLOG/_views/` directory
- Display view ID, description, total item count, and archived item count for each view
- Support for table, JSON, and text output formats (using existing output infrastructure)
- Integration with existing product/profile resolution

**Explicitly excluded:**
- Creating new views (handled by backlog-view-create)
- Updating or deleting views
- Filtering or searching views
- View membership management

## Steps

1. **Add leaf args type** - Create `ViewListArgs` DU in Program.fs with an optional `Product` flag (`--product <id>`), following the pattern of `ProductListArgs`, `TaskListArgs`
2. **Add top-level `ViewArgs` type** - Create `ViewArgs` DU with a `List` case, decorated with `[<CliPrefix(CliPrefix.DoubleDash)>]`, following the pattern of `ProductArgs`, `TaskArgs`
3. **Add `View` to `CliArgs`** - Add `| [<CliPrefix(CliPrefix.None)>] View of ParseResults<ViewArgs>` to `CliArgs`, alongside `Backlog`, `Product`, `Task`
4. **Implement handler function** - Create `handleViewList` function that:
   - Resolves the product: use `--product <id>` if provided, otherwise resolve from the working directory; error if neither succeeds
   - Calls `viewStore.ListViews(coordRoot)` to get all views
   - Calls `backlogStore.ListArchivedBacklogItems(coordRoot)` to get archived item IDs
   - For each view, derives `total = view.Items.Length` and `archived = view.Items |> intersect archivedIds |> count`
   - Formats output based on output flag (table/json/text)
   - Renders description as blank when absent
5. **Add output formatting** - Use existing output utilities to render views in table (with columns: ID, Description, Items, Archived), JSON, or text format
6. **Add error handling** - Handle empty view list gracefully (show friendly message)
7. **Wire up routing** - Add a `View` arm in `dispatch` that routes to `handleViewList`
8. **Write tests** - Add unit tests for the handler function covering:
   - Normal operation with multiple views
   - Empty view list
   - Different output formats

## Impact

**Files changed:**
- `src/cli/Program.fs` - Add `ViewListArgs`, `ViewArgs`, `handleViewList`, wire up `CliArgs` and `dispatch`
- `tests/cli.tests/Program.Tests.fs` (or similar) - Add tests for the new command

**Interfaces affected:**
- No new interfaces needed - `IViewStore.ListViews` and `IBacklogStore.ListArchivedBacklogItems` already exist
- Uses existing `IProductConfig` for product resolution

**Data migrations:** None - read-only operation on existing YAML files

## Risks

1. **Low risk** - Follows established patterns from existing list commands (ProductList, TaskList)
2. **Edge case: no views exist** - Will return empty list; need to display friendly message instead of empty table
3. **Output formatting** - Ensure description field handles None values gracefully (view YAML may not have description)

## Decisions

1. Views without a description field show blank in table output.
2. `ViewListArgs` accepts an optional `--product <id>` flag. If omitted, the current product is resolved from the working directory. If resolution fails (not in a product directory), the command errors with a clear message.
3. Item counts are derived by calling `IBacklogStore.ListArchivedBacklogItems` and intersecting the result with `view.Items`; no new interface methods needed.