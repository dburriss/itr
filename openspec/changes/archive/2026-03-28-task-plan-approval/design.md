## Context

The `itr` CLI manages a backlog of tasks through a state machine. Tasks progress through states: `planning` → `planned` → `approved` → `in_progress` → ... . The `task plan` command generates a `plan.md` and moves a task to `planned`. Currently there is no gate between `planned` and `in_progress` — any task with a plan can immediately start being worked on without explicit sign-off.

The domain layer uses pure functions (in `TaskUsecase.fs`) that take a task value and return a `Result`, keeping all side effects at the CLI boundary (`Program.fs`). Error cases are modelled in the `BacklogError` discriminated union in `Domain.fs`.

## Goals / Non-Goals

**Goals:**
- Provide an explicit `itr task approve <task_id>` command that transitions `planned → approved`
- Reject approval if `plan.md` does not exist (guards against approving a stub/empty plan)
- Make re-approval of an already-approved task idempotent with an informational message
- Generalize the `InvalidTaskState` error message so it is not plan-specific

**Non-Goals:**
- No approval metadata (approver name, timestamp, comments)
- No revoke/undo approval command
- No batch approval
- No interactive confirmation prompt
- No changes to how `TaskSummary` or `TaskDetail` display the `approved` state (already handled)

## Decisions

### Use the same pure-function + adapter pattern as `planTask`

**Decision**: Implement `approveTask : ItrTask -> bool -> Result<ItrTask * bool, BacklogError>` as a pure function in `TaskUsecase.fs`, keeping file-system checks at the handler level in `Program.fs`.

**Rationale**: Consistent with the existing `planTask` design. Pure functions are trivially testable without mocking the filesystem. Side effects (reading/writing files, printing output) stay in the CLI handler.

**Alternative considered**: Passing `IFileSystem` into `approveTask` — rejected because it couples a domain function to infrastructure and makes unit tests heavier.

### Return `wasAlreadyApproved` flag for idempotent case

**Decision**: When a task is already in `Approved` state, return `Ok (task, true)` where the boolean signals the no-op. The handler prints an informational "already approved" message rather than an error.

**Rationale**: Idempotent re-runs should not fail CI/scripting. The flag lets the handler distinguish a real transition from a no-op without using exceptions or a separate error case.

### Add `MissingPlanArtifact` to `BacklogError` DU

**Decision**: Introduce `MissingPlanArtifact of TaskId` as a new case in `BacklogError` rather than reusing `InvalidTaskState`.

**Rationale**: The two error conditions have different meanings and different user-facing messages. A missing artifact is a precondition failure, not a state-machine violation. Separate cases allow precise formatting and future handling.

### Generalize `InvalidTaskState` error message

**Decision**: Change the human-readable message from `"Cannot plan task '<id>': current state is '<state>' (only planning or planned states are allowed)"` to `"Invalid state transition for task '<id>': current state is '<state>'"`.

**Rationale**: The message was previously plan-specific. With approval (and potentially future commands) also producing `InvalidTaskState` errors, the message must be generic. The exit code is unchanged; only the text changes.

## Risks / Trade-offs

- `InvalidTaskState` message change → any user script parsing stderr text will break. Low risk: the message is human-readable, not a structured API. Exit codes are unchanged.
- Exhaustive `match` on `BacklogError` missed → compiler warning in F#. Caught by `dotnet build`.
- `plan.md` existence check races with concurrent file deletion → acceptable for a single-user CLI; no concurrency expected.

## Open Questions

- Should approval from `InProgress` (or later states) be allowed? Current design rejects this as `InvalidTaskState`. Revisit if a "re-approve after revision" workflow is needed.
