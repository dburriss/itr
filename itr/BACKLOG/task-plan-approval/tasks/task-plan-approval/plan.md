I now have a comprehensive understanding of the codebase. Here is the completed plan:

# Approve a task plan

**Task ID:** task-plan-approval
**Backlog Item:** task-plan-approval
**Repo:** itr

## Description

Allow users to explicitly approve a task plan so planning has a clear handoff before implementation begins.


## Scope

**Included:**
- New `approveTask` pure function in `TaskUsecase.fs` that validates the `Planned -> Approved` state transition
- New `MissingPlanArtifact` case in `BacklogError` to reject approval when `plan.md` does not exist
- New `Approve` CLI subcommand under `TaskArgs` (`itr task approve <task_id>`)
- New `handleTaskApprove` handler in `Program.fs` following the same pattern as `handleTaskPlan`
- Generalized `InvalidTaskState` error message in `formatBacklogError` so it is not plan-specific
- Communication tests for the pure `approveTask` function
- Acceptance tests for the full approve flow with filesystem

**Excluded:**
- No approval metadata beyond what is stored in `TaskState` (no approver name, timestamp, or comments)
- No undo/revoke approval command
- No batch approve (multiple tasks at once)
- No interactive confirmation prompt
- No changes to `TaskSummary`, `TaskDetail`, or output formatting (these already derive `PlanApproved` from state)

## Steps

1. Add `MissingPlanArtifact of TaskId` case to `BacklogError` in `src/domain/Domain.fs`
2. Add `MissingPlanArtifact` formatting to `formatBacklogError` in `src/cli/Program.fs` (e.g. `"Cannot approve task '<id>': plan artifact does not exist"`)
3. Generalize the `InvalidTaskState` error message in `formatBacklogError` from the plan-specific `"Cannot plan task..."` wording to a generic `"Invalid state transition for task '<id>': current state is '<state>'"` message
4. Add `approveTask` pure function to `src/features/Task/TaskUsecase.fs` that accepts `(task: ItrTask) (planExists: bool)` and returns `Result<ItrTask * bool, BacklogError>`:
   - If `planExists` is false, return `Error (MissingPlanArtifact task.Id)`
   - If state is `Planned`, return `Ok ({ task with State = Approved }, false)`
   - If state is `Approved`, return `Ok (task, true)` (idempotent re-approval)
   - Otherwise, return `Error (InvalidTaskState (task.Id, task.State))`
5. Add `TaskApproveArgs` Argu type in `src/cli/Program.fs` with a mandatory `Task_Id` positional argument
6. Add `Approve of ParseResults<TaskApproveArgs>` case to `TaskArgs` in `src/cli/Program.fs`
7. Add `handleTaskApprove` handler function in `src/cli/Program.fs` that:
   - Resolves the task from `ITaskStore.ListAllTasks`
   - Checks `plan.md` existence via `IFileSystem.FileExists`
   - Calls `Task.approveTask task planExists`
   - Writes the updated task via `ITaskStore.WriteTask`
   - Prints confirmation message
8. Wire `TaskArgs.Approve` into the `dispatch` function in `src/cli/Program.fs`, following the same portfolio/product resolution pattern as `Plan`
9. Add communication tests in `tests/communication/TaskApproveDomainTests.fs`:
   - `Planned` task with plan -> succeeds, state becomes `Approved`
   - `Approved` task re-approved -> succeeds idempotently, `wasAlreadyApproved = true`
   - `Planning` task -> `InvalidTaskState` error
   - `InProgress` task -> `InvalidTaskState` error
   - `Planned` task without plan -> `MissingPlanArtifact` error
10. Register `TaskApproveDomainTests.fs` in `tests/communication/Itr.Tests.Communication.fsproj`
11. Add acceptance tests in `tests/acceptance/TaskApproveAcceptanceTests.fs`:
    - Happy path: write task YAML in `planned` state + `plan.md`, call approve via adapter, verify task YAML reads `approved`
    - Missing plan: write task YAML in `planned` state without `plan.md`, verify `MissingPlanArtifact` error
    - Wrong state: write task YAML in `planning` state + `plan.md`, verify `InvalidTaskState` error
12. Register `TaskApproveAcceptanceTests.fs` in `tests/acceptance/Itr.Tests.Acceptance.fsproj`
13. Run `dotnet build` and `dotnet test` to verify all tests pass

## Dependencies

- task-plan

## Acceptance Criteria

- A command marks an existing task plan as approved
- Approval metadata is recorded in task coordination data
- Approval is rejected when a task does not yet have a plan artifact
- Approved status is visible in task list and task detail output

## Impact

**Files changed:**
- `src/domain/Domain.fs` — add `MissingPlanArtifact` case to `BacklogError` DU
- `src/features/Task/TaskUsecase.fs` — add `approveTask` pure function
- `src/cli/Program.fs` — add `TaskApproveArgs` type, `Approve` case on `TaskArgs`, `handleTaskApprove` handler, dispatch wiring, `MissingPlanArtifact` error formatting, generalized `InvalidTaskState` message
- `tests/communication/TaskApproveDomainTests.fs` — new file for pure function tests
- `tests/communication/Itr.Tests.Communication.fsproj` — register new test file
- `tests/acceptance/TaskApproveAcceptanceTests.fs` — new file for filesystem integration tests
- `tests/acceptance/Itr.Tests.Acceptance.fsproj` — register new test file

**Interfaces affected:**
- `BacklogError` DU gains a new case; any exhaustive `match` on `BacklogError` in downstream code will need updating (only `formatBacklogError` in `Program.fs`)
- No changes to `ITaskStore`, `IBacklogStore`, or `IFileSystem` interfaces

**Data migrations:**
- None. The `TaskState.Approved` value and its YAML serialization (`"approved"`) already exist. No schema changes to `task.yaml`.

**Behavioral change:**
- The `InvalidTaskState` error message produced by `task plan` on an invalid state will change from `"Cannot plan task '<id>': current state is '<state>' (only planning or planned states are allowed)"` to the generic `"Invalid state transition for task '<id>': current state is '<state>'"`. This affects error output only; no programmatic contracts change.

## Risks

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| `InvalidTaskState` message change breaks user scripts parsing stderr | Low | The message is human-readable, not a structured API. Document the change. Exit codes remain unchanged. |
| Exhaustive match on `BacklogError` missed in a location beyond `formatBacklogError` | Low | Compiler will flag incomplete match warnings in F#. Run `dotnet build` to verify. |
| `plan.md` existence check races with concurrent file deletion | Very Low | Single-user CLI tool; no concurrency expected. Acceptable for current scope. |
| Idempotent re-approval (Approved -> Approved) may mask accidental double-runs | Low | Return `wasAlreadyApproved = true` and print an informational message so the user knows it was a no-op. |

## Open Questions

- Should `approveTask` also accept re-approval from states beyond `Approved` (e.g. `InProgress` -> still return `Approved`)? Current design rejects this as `InvalidTaskState`, treating approval as a one-time gate before implementation starts.
- Should the generalized `InvalidTaskState` message include which transitions are valid for the attempted operation, or is the current state alone sufficient context?