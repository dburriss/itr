# Start implementing a task

**Task ID:** task-start
**Backlog Item:** task-start
**Repo:** itr

## Description

Allow users to move a task into active implementation once planning is complete and approved. Starting a task triggers the agent to begin work on the scoped repo.

## Scope

**Included:**

- Adding `startTask` pure transition function in `TaskUsecase.fs` that transitions tasks from `Approved` → `InProgress`
- Validating that the openspec change is ready via `IOpenSpecCli.IsChangeReady(taskId)` — shells out to `openspec status --change <task-id> --json` and checks `isComplete: true`
- New `IOpenSpecCli` interface in `Interfaces.fs` with `IsChangeReady: TaskId -> bool`
- New `OpenSpecCliAdapter` implementing `IOpenSpecCli` using process execution
- Adding new `MissingSpecArtifacts of TaskId` error type to `Domain.fs`
- Reusing existing `InvalidTaskState` error type from `Domain.fs`
- Adding CLI command `task start` in `Program.fs` with `TaskStartArgs` type
- Handler function `handleTaskStart` that:
  - Calls `openSpecCli.IsChangeReady(taskId)` and passes `specsReady` bool to `startTask`
  - Sets task state to `InProgress` in task.yaml
  - Constructs prompt as `/opsx-apply <task-id>` (ACP slash command sent as plain text in `session/prompt`)
  - Invokes the agent directly (blocking until completion) via `IAgentHarness.Prompt`
- Extending `IAgentHarness.Prompt` with `taskContext: (BacklogId * TaskId) option` parameter to enable `AcpHarnessAdapter` to write `run.yaml`
- `AcpHarnessAdapter` writes `run.yaml` to the task directory after `session/new` succeeds, containing `started_at` and `session_id`
- State transition recorded in task.yaml (existing coordination data format)

**Excluded:**

- Any changes to `OpenCodeHarnessAdapter` — ACP is the target protocol; the HTTP adapter is legacy
- Renaming `IAgentHarness.Prompt`
- Any changes to task coordination data format or storage location
- Async/background agent invocation
- A prompt template file — the prompt is simply `/opsx-apply <task-id>` constructed inline
- Direct filesystem checks for openspec artifact files — the openspec CLI handles this

## Steps

1. **Add `MissingSpecArtifacts of TaskId`** to `BacklogError` in `src/domain/Domain.fs`

2. **Add `IOpenSpecCli` interface** to `src/domain/Interfaces.fs`:
   - `IsChangeReady: TaskId -> bool` — returns true if `openspec status` reports `isComplete: true` for the given change name

3. **Add `OpenSpecCliAdapter`** in `src/adapters/OpenSpecCliAdapter.fs`:
   - Implements `IOpenSpecCli`
   - Shells out to `openspec status --change <task-id> --json`
   - Parses JSON response and returns `isComplete` field as bool
   - Returns false on any error (non-zero exit, parse failure, change not found)

4. **Update `IAgentHarness.Prompt` signature** in `src/domain/Interfaces.fs`:
   - Add `taskContext: (BacklogId * TaskId) option` parameter between `prompt` and `debug`

5. **Update `AcpHarnessAdapter.Prompt`** in `src/adapters/AcpAdapter.fs`:
   - Accept `taskContext: (BacklogId * TaskId) option`
   - After `session/new` succeeds and `sessionId` is extracted, if `taskContext` is `Some(backlogId, taskId)`, write `run.yaml` to `<coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/run.yaml`:
     ```yaml
     started_at: <ISO8601 datetime>
     session_id: <sessionId>
     ```

6. **Update existing `handleTaskPlan` call site** of `IAgentHarness.Prompt` in `src/cli/Program.fs` to pass `None` for `taskContext`

7. **Add `startTask` transition function** in `src/features/Task/TaskUsecase.fs`:
   - Pattern match on `TaskState.Approved` → `InProgress` (requires `specsReady=true`)
   - Idempotent case: `TaskState.InProgress` → return existing task unchanged
   - Return `InvalidTaskState` error for other states
   - Return `MissingSpecArtifacts` error when `specsReady=false`

8. **Add `TaskStartArgs` CLI type** in `src/cli/Program.fs` following `TaskApproveArgs` pattern (lines 268-275)

9. **Add `Start` case** to the `TaskArgs` discriminated union

10. **Add `handleTaskStart` handler** (modelled on `handleTaskApprove`, lines 907-939):
    - Parse arguments, resolve taskId from backlog
    - Load all tasks from taskStore
    - Find target task
    - Call `openSpecCli.IsChangeReady(taskId)` to get `specsReady`
    - Call `startTask task specsReady` pure function
    - Write updated task via taskStore
    - Construct prompt: `sprintf "/opsx-apply %s" (TaskId.value taskId)`
    - Invoke agent via `harness.Prompt prompt (Some(backlogId, taskId)) debug`
    - Print success/failure message

11. **Wire up command** in main match block, injecting `OpenSpecCliAdapter` into deps

12. **Add acceptance tests** in `tests/acceptance/` following `TaskApproveAcceptanceTests.fs` pattern:
    - Valid transition: `Approved` → `InProgress` (mock `IOpenSpecCli` returning `true`)
    - Invalid transition: other states return `InvalidTaskState` error
    - `IOpenSpecCli` returning `false` returns `MissingSpecArtifacts` error

13. **Run build and tests** (`dotnet build && dotnet test`)

## Impact

**Files Changed:**

- `src/domain/Domain.fs` - Add `MissingSpecArtifacts of TaskId` to `BacklogError` (~1 line)
- `src/domain/Interfaces.fs` - Add `IOpenSpecCli` interface and `taskContext` param to `IAgentHarness.Prompt` (~5 lines)
- `src/adapters/OpenSpecCliAdapter.fs` - New adapter implementing `IOpenSpecCli` (~25 lines)
- `src/adapters/AcpAdapter.fs` - Accept `taskContext`, write `run.yaml` after `session/new` (~15 lines)
- `src/features/Task/TaskUsecase.fs` - Add `startTask` function (~15 lines)
- `src/cli/Program.fs` - Add args type, handler, wire-up, update existing Prompt call site (~60 lines)
- `tests/acceptance/TaskStartAcceptanceTests.fs` - New test file

**Interfaces Affected:**

- `IOpenSpecCli` (new): `IsChangeReady: TaskId -> bool`
- `IAgentHarness.Prompt`: new `taskContext: (BacklogId * TaskId) option` parameter (existing caller `handleTaskPlan` passes `None`)
- CLI: new `task start <task-id>` command
- Domain: new `startTask` usecase function

**Data Written:**

- `task.yaml`: `state` field updated to `in_progress`
- `run.yaml` (new): written to task directory by `AcpHarnessAdapter` with `started_at` and `session_id`

**Data Migrations:**

- None required

## Risks

1. **Similar implementation to task-approve**: Low risk - pattern is well-established; reuse existing code patterns
2. **Validation via openspec CLI**: Low risk - `openspec status --change <task-id> --json` returns `isComplete`; adapter returns false on any error, which surfaces as `MissingSpecArtifacts`. Requires `openspec` to be installed at runtime.
3. **run.yaml not written if OpenCodeHarnessAdapter is used**: By design — `OpenCodeHarnessAdapter` is excluded from this story. No marker file written for legacy HTTP protocol.
4. **Blocking agent call**: Agent runs to completion before the CLI returns. Long-running tasks will hold the terminal. Acceptable given the current synchronous model.

## Open Questions

- None
