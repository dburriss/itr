## 1. Domain

- [x] 1.1 Add `TaskNotFound of TaskId` case to `BacklogError` in `src/domain/Domain.fs`

## 2. Use Case

- [x] 2.1 Add `TaskDetail` record type to `src/features/Task/TaskUsecase.fs`
- [x] 2.2 Implement `getTaskDetail` function in `src/features/Task/TaskUsecase.fs`

## 3. CLI

- [x] 3.1 Add `TaskInfoArgs` DU to `src/cli/Program.fs`
- [x] 3.2 Extend `TaskArgs` with `Info of ParseResults<TaskInfoArgs>` case in `src/cli/Program.fs`
- [x] 3.3 Implement `handleTaskInfo` handler in `src/cli/Program.fs`
- [x] 3.4 Wire `Info` dispatch in the task command handler in `src/cli/Program.fs`
- [x] 3.5 Add `TaskNotFound` case to `formatBacklogError` in `src/cli/Program.fs`

## 4. Tests

- [x] 4.1 Add communication tests for `getTaskDetail` in `tests/communication/`
- [x] 4.2 Add acceptance test: `task info shows full detail`
- [x] 4.3 Add acceptance test: `task info plan exists when plan.md present`
- [x] 4.4 Add acceptance test: `task info shows siblings`
- [x] 4.5 Add acceptance test: `task info json output is valid`
- [x] 4.6 Add acceptance test: `task info returns error for unknown id`

## 5. Verify

- [x] 5.1 Run `dotnet build` and fix any compiler errors
- [x] 5.2 Run `dotnet test` and ensure all tests pass
