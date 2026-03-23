## Why

The backlog has no way to be queried from the CLI — users cannot see what items exist, their current status, or filter by type or view. This also exposes a silent bug where `state: planned` in task YAML files is misread as `Planning`, corrupting computed statuses.

## What Changes

- Add `itr backlog list` CLI subcommand with optional `--view`, `--status`, `--type`, and `--output json` flags.
- Fix **BREAKING** `TaskState` DU: add `Planned` and `Approved` cases; fix `mapTaskState` to no longer silently coerce unknown strings to `Planning`.
- Add `BacklogItemStatus` domain type with computed status logic driven by task states.
- Add `BacklogItemSummary` and `BacklogSnapshot` read models for snapshot-based querying.
- Add `IBacklogStore.ListBacklogItems` to enumerate all items under a coordination root.
- Add `IViewStore` interface and `ViewStoreAdapter` to read `_views/*.yaml` files.
- Add `BacklogUsecase.loadSnapshot` and `BacklogUsecase.listBacklogItems` use-case functions.
- Wire `IViewStore` into `AppDeps` composition root.

## Capabilities

### New Capabilities
- `backlog-list`: CLI command to list backlog items with filtering by view, type, status; supports table and JSON output modes. Includes computed `BacklogItemStatus` from task states.

### Modified Capabilities
- `backlog-take`: `TaskState` gains `Planned` and `Approved` cases — exhaustive matches in existing code must be updated. Bug fix only; no behavioral requirement changes.

## Impact

- `src/domain/Domain.fs` — new types and DU cases
- `src/domain/Interfaces.fs` — new interface members
- `src/adapters/YamlAdapter.fs` — new adapter implementations and bug fix
- `src/features/Backlog/BacklogUsecase.fs` — new use-case functions
- `src/cli/Program.fs` — new subcommand wiring
- All exhaustive `TaskState` matches must handle `Planned` and `Approved`
