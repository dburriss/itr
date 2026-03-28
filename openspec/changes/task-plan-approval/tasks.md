## 1. Domain Layer

- [x] 1.1 Add `MissingPlanArtifact of TaskId` case to `BacklogError` DU in `src/domain/Domain.fs`
- [x] 1.2 Add `approveTask : ItrTask -> bool -> Result<ItrTask * bool, BacklogError>` pure function to `src/features/Task/TaskUsecase.fs`

## 2. CLI Layer

- [x] 2.1 Add `MissingPlanArtifact` formatting to `formatBacklogError` in `src/cli/Program.fs` (e.g. `"Cannot approve task '<id>': plan artifact does not exist"`)
- [x] 2.2 Generalize the `InvalidTaskState` error message in `formatBacklogError` to `"Invalid state transition for task '<id>': current state is '<state>'"` (removes plan-specific wording)
- [x] 2.3 Add `TaskApproveArgs` Argu type with a mandatory `Task_Id` positional argument in `src/cli/Program.fs`
- [x] 2.4 Add `Approve of ParseResults<TaskApproveArgs>` case to `TaskArgs` DU in `src/cli/Program.fs`
- [x] 2.5 Add `handleTaskApprove` handler in `src/cli/Program.fs` that resolves the task, checks `plan.md` existence, calls `approveTask`, writes the updated task, and prints a confirmation or "already approved" message
- [x] 2.6 Wire `TaskArgs.Approve` into the `dispatch` function in `src/cli/Program.fs` following the same portfolio/product resolution pattern as `Plan`

## 3. Communication Tests

- [x] 3.1 Create `tests/communication/TaskApproveDomainTests.fs` with tests: `Planned` task + plan → `Approved`; `Approved` task re-approved → idempotent (`wasAlreadyApproved = true`); `Planning` task → `InvalidTaskState`; `InProgress` task → `InvalidTaskState`; `Planned` task without plan → `MissingPlanArtifact`
- [x] 3.2 Register `TaskApproveDomainTests.fs` in `tests/communication/Itr.Tests.Communication.fsproj`

## 4. Acceptance Tests

- [x] 4.1 Create `tests/acceptance/TaskApproveAcceptanceTests.fs` with tests: happy path (planned + plan.md → approved YAML); missing plan (planned without plan.md → `MissingPlanArtifact`); wrong state (planning + plan.md → `InvalidTaskState`)
- [x] 4.2 Register `TaskApproveAcceptanceTests.fs` in `tests/acceptance/Itr.Tests.Acceptance.fsproj`

## 5. Verification

- [x] 5.1 Run `dotnet build` and confirm no warnings or errors
- [x] 5.2 Run `dotnet test` and confirm all tests pass
