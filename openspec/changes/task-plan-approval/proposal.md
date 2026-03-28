## Why

The task planning workflow currently has no explicit handoff between planning and implementation. Once a plan is written, there is no signal that it has been reviewed and approved — work could begin on an unreviewed plan. Adding an approval step creates a clear gate that separates planning from implementation.

## What Changes

- New `itr task approve <task_id>` CLI subcommand that transitions a task from `planned` to `approved`
- Approval is rejected if the task does not yet have a `plan.md` artifact
- Re-approving an already-approved task is idempotent (no error, informational message)
- Approval attempts on tasks in other states (e.g. `planning`, `in-progress`) return a clear error
- The `InvalidTaskState` error message is generalized (was plan-specific, now generic)

## Capabilities

### New Capabilities

- `task-approve`: CLI command and domain logic to approve a task plan, transitioning task state from `planned` to `approved`

### Modified Capabilities

- `task-plan`: The `InvalidTaskState` error message format changes from plan-specific wording to a generic state-transition error message

## Impact

- `src/domain/Domain.fs` — new `MissingPlanArtifact` error case
- `src/features/Task/TaskUsecase.fs` — new `approveTask` pure function
- `src/cli/Program.fs` — new CLI args type, command handler, dispatch wiring, updated error formatting
- `tests/communication/` — new domain-level tests for `approveTask`
- `tests/acceptance/` — new filesystem integration tests for the approve command
