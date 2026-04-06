## Why

Users need a way to discover what named backlog views exist for a product before working with them. Without a list command, users must manually inspect the filesystem to know what views are available for delivery planning.

## What Changes

- New CLI command `itr view list` that lists all view YAML files from the current product's `BACKLOG/_views/` directory
- Displays view ID, description, total item count, and archived item count per view
- Supports `--output table|json|text` format flag (consistent with existing list commands)
- Accepts optional `--product <id>` flag; falls back to product resolution from working directory

## Capabilities

### New Capabilities

- `backlog-view-list`: CLI command to list all named backlog views for a product, showing ID, description, item count, and archived item count

### Modified Capabilities

<!-- None - this is a purely additive change -->

## Impact

- `src/cli/Program.fs` - New `ViewListArgs`, `ViewArgs` DU types, `handleViewList` handler, wired into `CliArgs` and `dispatch`
- `tests/cli.tests/` - New test cases for the `view list` command
- No interface changes needed: `IViewStore.ListViews` and `IBacklogStore.ListArchivedBacklogItems` already exist
