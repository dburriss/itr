## 1. Domain

- [ ] 1.1 Add `TaskNotFound of TaskId` case to `BacklogError` in `src/domain/Domain.fs`

## 2. Use Case

- [ ] 2.1 Add `TaskDetail` record type to `src/features/Task/TaskUsecase.fs`
- [ ] 2.2 Implement `getTaskDetail` function in `src/features/Task/TaskUsecase.fs`

## 3. CLI

- [ ] 3.1 Add `TaskInfoArgs` DU to `src/cli/Program.fs`
- [ ] 3.2 Extend `TaskArgs` with `Info of ParseResults<TaskInfoArgs>` case in `src/cli/Program.fs`
- [ ] 3.3 Implement `handleTaskInfo` handler in `src/cli/Program.fs`
- [ ] 3.4 Wire `Info` dispatch in the task command handler in `src/cli/Program.fs`
- [ ] 3.5 Add `TaskNotFound` case to `formatBacklogError` in `src/cli/Program.fs`

## 4. Tests

- [ ] 4.1 Add communication tests for `getTaskDetail` in `tests/communication/`
- [ ] 4.2 Add acceptance test: `task info shows full detail`
- [ ] 4.3 Add acceptance test: `task info plan exists when plan.md present`
- [ ] 4.4 Add acceptance test: `task info shows siblings`
- [ ] 4.5 Add acceptance test: `task info json output is valid`
- [ ] 4.6 Add acceptance test: `task info returns error for unknown id`

## 5. Verify

- [ ] 5.1 Run `dotnet build` and fix any compiler errors
- [ ] 5.2 Run `dotnet test` and ensure all tests pass
