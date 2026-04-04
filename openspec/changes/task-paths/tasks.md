## 1. Interface and Adapter Layer

- [ ] 1.1 Update `ITaskStore.ListTasks` in `src/domain/Interfaces.fs` to return `Result<(ItrTask * string) list, BacklogError>`
- [ ] 1.2 Update `YamlAdapter.fs` `ListTasks` to return `(task, taskFile)` tuples instead of just `task`

## 2. Use-Case Types and Logic

- [ ] 2.1 Add `TaskYamlPath: string` and `PlanMdPath: string option` fields to `TaskSummary` in `src/features/Task/TaskUsecase.fs`
- [ ] 2.2 Add `TaskYamlPath: string` and `PlanMdPath: string option` fields to `TaskDetail` in `src/features/Task/TaskUsecase.fs`
- [ ] 2.3 Update `listTasks` signature to accept `(ItrTask * string) list`; derive `taskYamlPath` from tuple; derive `planMdPath` via `Path.Combine(Path.GetDirectoryName(taskYamlPath), "plan.md")` wrapped in `Some` only if file exists
- [ ] 2.4 Update `getTaskDetail` to accept `taskYamlPath: string` parameter; derive `planMdPath` same way; populate both fields on `TaskDetail`

## 3. Program.fs Wiring

- [ ] 3.1 Update `AppDeps` `ListTasks` delegator in `src/cli/Program.fs` to forward the new `(ItrTask * string) list` return type unchanged
- [ ] 3.2 Update `handleTaskList` call to `listTasks` to pass tuples
- [ ] 3.3 Update `handleTaskInfo` to pass `taskYamlPath` to `getTaskDetail`
- [ ] 3.4 Update other `ListTasks` call sites that don't need paths to discard with `|> Result.map (List.map fst)`

## 4. Output Formats — task list

- [ ] 4.1 Update `handleTaskList` text output: 6 tab-separated columns — `taskId\tbacklogId\trepoId\tstate\ttaskYamlPath\tplanMdPath` (`planMdPath` is `""` when `None`)
- [ ] 4.2 Update `handleTaskList` JSON output: replace `planApproved` field with `taskYamlPath` and `planMdPath` fields
- [ ] 4.3 Update `handleTaskList` table output: replace `Plan Approved` column with `Task YAML` and `Plan MD` columns

## 5. Output Formats — task info

- [ ] 5.1 Update `handleTaskInfo` text output: add `taskYamlPath\t<path>` and `planMdPath\t<path or empty>` lines
- [ ] 5.2 Update `handleTaskInfo` JSON output: add `taskYamlPath` and `planMdPath` fields
- [ ] 5.3 Update `handleTaskInfo` table output: add `Task YAML` and `Plan MD` rows

## 6. Build and Test

- [ ] 6.1 Run `dotnet build` and fix any compilation errors
- [ ] 6.2 Run `dotnet test` and ensure all tests pass
