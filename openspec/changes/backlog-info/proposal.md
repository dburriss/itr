## Why

The `backlog list` command provides a high-level overview of all items, but there is no way to inspect the details of a single backlog item (summary, acceptance criteria, dependencies, repos, tasks) without reading raw YAML files. A `backlog info` command gives users a focused, readable view of everything associated with one item.

## What Changes

- Add `itr backlog info <id>` CLI subcommand
- Displays all fields from `item.yaml`: id, title, type, priority, status, summary, acceptance criteria, dependencies, repos, created date
- Lists associated tasks with their state and repo
- Supports `--output json` for structured output
- Returns a non-zero exit code when the item is not found

## Capabilities

### New Capabilities
- `backlog-info`: Show detailed information about a single backlog item including its fields, computed status, tasks, and metadata

### Modified Capabilities
<!-- none -->

## Impact

- `src/cli/Program.fs`: New `InfoArgs` DU, new `BacklogArgs` case, `handleBacklogInfo` handler, dispatch wiring
- `src/features/Backlog/BacklogUsecase.fs`: New `getBacklogItemDetail` function that loads item + tasks and returns a composed detail record
- `src/domain/Domain.fs`: New `BacklogItemDetail` type (item + status + tasks + view id)
- No breaking changes to existing commands
