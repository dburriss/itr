## Why

The `task list` command currently hides archived tasks by default and provides no way to control ordering of results. Users need more control over what they see and in what order—specifically the ability to exclude tasks by state and sort results by creation date or priority state.

## What Changes

- Add `--exclude <state>` flag to `task list` to filter out tasks with the specified state (e.g., `--exclude archived`)
- Add `--order-by <field>` flag to `task list` supporting `created` (ascending by creation date) and `state` (descending by priority order)
- **BREAKING**: Remove implicit exclusion of archived tasks—`task list` now shows all tasks by default (users must pass `--exclude archived` to restore previous behavior)
- Update `TaskUsecase.filterTasks` to accept an exclude list parameter
- Add acceptance tests for the new flags and the changed default behavior

## Capabilities

### New Capabilities

<!-- None: these are requirement-level changes to an existing capability -->

### Modified Capabilities

- `task-list`: Default behavior changes (archived now shown), two new filter/sort flags added

## Impact

- `src/cli/Program.fs` — `TaskListArgs` type and `handleTaskList` handler
- `src/features/Task/TaskUsecase.fs` — `filterTasks` function signature and logic
- `tests/acceptance/TaskListAcceptanceTests.fs` — new test cases for new flags and changed default
