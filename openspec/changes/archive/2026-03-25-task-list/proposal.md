## Why

Users have no way to see all tasks across a product in one view — tasks are currently only accessible per-backlog-item, making it impossible to get a product-wide picture of work in progress, plan approval status, or filter by repo. This capability is needed now to support planning and visibility workflows.

## What Changes

- New `itr task list` CLI subcommand for product-wide task listing
- New `ITaskStore.ListAllTasks` interface method that scans all backlog items (active + archived)
- New `TaskSummary` type and `listTasks`/`filterTasks` pure functions in the task domain
- Human-readable table output and structured JSON output (`--output json`)
- Filtering by `--backlog`, `--repo`, and `--state`; default view excludes archived tasks

## Capabilities

### New Capabilities
- `task-list`: List all tasks across a product with filtering and output format options

### Modified Capabilities
<!-- None — no existing spec requirements are changing. -->

## Impact

- `src/domain/Interfaces.fs`: add `ListAllTasks` to `ITaskStore`
- `src/adapters/YamlAdapter.fs`: implement `ListAllTasks` in `TaskStoreAdapter`
- `src/features/Task/TaskUsecase.fs`: add `TaskSummary`, `listTasks`, `filterTasks`
- `src/cli/Program.fs`: add `TaskListArgs`, `TaskArgs`, `handleTaskList`, wire into `CliArgs` dispatch, implement `ListAllTasks` in `AppDeps`
- New acceptance tests in `tests/acceptance/`
- New communication tests in `tests/communication/`
