## Why

Users need a quick way to inspect a single task in detail — seeing its source backlog item, repo, lifecycle state, plan status, and related sibling tasks — without scanning through the full `task list` output. This is a natural complement to `task list` and completes the task management surface.

## What Changes

- New `itr task info <id>` subcommand that displays detailed information for a single task.
- New `TaskNotFound` error case in `BacklogError`.
- New `TaskDetail` type and `getTaskDetail` function in `TaskUsecase.fs`.
- New `TaskInfoArgs` DU and `handleTaskInfo` handler in `Program.fs`.

## Capabilities

### New Capabilities

- `task-info`: Display detailed information for a single task including id, source backlog item, repo, state, plan existence, plan approval status, creation date, and sibling tasks that share the same backlog item. Supports `--output json` for machine-readable output.

### Modified Capabilities

<!-- No existing specs have requirement-level changes. -->

## Impact

- `src/domain/Domain.fs`: new `BacklogError` case.
- `src/features/Task/TaskUsecase.fs`: new type and function.
- `src/cli/Program.fs`: new CLI args, handler, and error formatting.
- No existing commands are affected; `ITaskStore` interface is unchanged.
