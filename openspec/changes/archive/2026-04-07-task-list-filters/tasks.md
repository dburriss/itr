## 1. CLI Arguments

- [x] 1.1 Add `Exclude` optional argument to `TaskListArgs` DU in `src/cli/Program.fs` with usage string listing valid states
- [x] 1.2 Add `Order_By` optional argument to `TaskListArgs` DU in `src/cli/Program.fs` with usage string (`created` | `state`)

## 2. TaskUsecase

- [x] 2.1 Add `exclude: TaskState list` parameter to `filterTasks` in `src/features/Task/TaskUsecase.fs`
- [x] 2.2 Add filter logic in `filterTasks` to remove tasks whose state is in the `exclude` list

## 3. Handler Updates

- [x] 3.1 Remove implicit archived exclusion (lines 562–566) from `handleTaskList` in `src/cli/Program.fs`
- [x] 3.2 Parse `--exclude` argument in `handleTaskList`, validate state string, pass exclude list to `filterTasks`
- [x] 3.3 Parse `--order-by` argument in `handleTaskList`, validate value (`created` | `state`), return error on unknown value
- [x] 3.4 Apply ordering to filtered results before output: `created` → sort by `CreatedAt` ascending; `state` → sort by priority order descending (default is `created` ascending)

## 4. Acceptance Tests

- [x] 4.1 Add test: default `task list` now shows archived tasks
- [x] 4.2 Add test: `--exclude archived` hides archived tasks
- [x] 4.3 Add test: `--order-by created` returns tasks sorted oldest-first
- [x] 4.4 Add test: `--order-by state` returns tasks in priority order (planning before archived)
- [x] 4.5 Add test: `--exclude unknownstate` exits non-zero with error message
- [x] 4.6 Add test: `--order-by unknown` exits non-zero with error message

## 5. Verification

- [x] 5.1 Run `dotnet build` — no errors
- [x] 5.2 Run `dotnet test` — all tests pass
