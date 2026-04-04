## 1. Domain - Path Construction Modules

- [ ] 1.1 Add `BacklogItem` module to `src/domain/Domain.fs` with `itemDir` and `itemFile` functions (after `BacklogItem` type)
- [ ] 1.2 Add `ItrTask` module to `src/domain/Domain.fs` with `taskDir`, `taskFile`, and `planFile` functions (after `ItrTask` type)

## 2. Domain - Update Summary and Detail Types

- [ ] 2.1 Add `Path: string` field to `BacklogItemSummary` record in `src/domain/Domain.fs`
- [ ] 2.2 Add `Path: string` field to `BacklogItemDetail` record in `src/domain/Domain.fs`

## 3. Interface - Update IBacklogStore Read Methods

- [ ] 3.1 Change `LoadBacklogItem` return type to `Result<BacklogItem * string, BacklogError>` in `src/domain/Interfaces.fs`
- [ ] 3.2 Change `LoadArchivedBacklogItem` return type to `Result<(BacklogItem * string) option, BacklogError>` in `src/domain/Interfaces.fs`
- [ ] 3.3 Change `ListBacklogItems` return type to `Result<(BacklogItem * string) list, BacklogError>` in `src/domain/Interfaces.fs`
- [ ] 3.4 Change `ListArchivedBacklogItems` return type to `Result<(BacklogItem * string) list, BacklogError>` in `src/domain/Interfaces.fs`

## 4. Adapter - Update YamlAdapter.fs

- [ ] 4.1 Replace inline `Path.Combine` in `BacklogItemExists` with `BacklogItem.itemFile coordRoot backlogId`
- [ ] 4.2 Replace inline `Path.Combine` in `WriteBacklogItem` with `BacklogItem.itemFile coordRoot backlogId`
- [ ] 4.3 Replace inline `Path.Combine` in `ArchiveBacklogItem` source dir with `BacklogItem.itemDir coordRoot backlogId`
- [ ] 4.4 Update `LoadBacklogItem` to use `BacklogItem.itemFile` and return `Ok(item, path)`
- [ ] 4.5 Update `ListBacklogItems` to propagate `(item, path)` tuples from `LoadBacklogItem` calls
- [ ] 4.6 Update `LoadArchivedBacklogItem` to return `Ok(Some(item, path))` (path from scan loop)
- [ ] 4.7 Update `ListArchivedBacklogItems` to return `(item * string) list` (path from scan)
- [ ] 4.8 Replace inline `Path.Combine` in `ListTasks` task.yaml path with `ItrTask.taskFile coordRoot backlogId taskId`
- [ ] 4.9 Replace inline `Path.Combine` in `WriteTask` with `ItrTask.taskFile coordRoot backlogId taskId`
- [ ] 4.10 Replace inline `Path.Combine` in `ArchiveTask` dir with `ItrTask.taskDir coordRoot backlogId taskId`

## 5. CLI - Update AppDeps Delegators

- [ ] 5.1 Update `LoadBacklogItem` delegator in `AppDeps` (`src/cli/Program.fs`) to forward new return type
- [ ] 5.2 Update `LoadArchivedBacklogItem` delegator in `AppDeps` to forward new return type
- [ ] 5.3 Update `ListBacklogItems` delegator in `AppDeps` to forward new return type
- [ ] 5.4 Update `ListArchivedBacklogItems` delegator in `AppDeps` to forward new return type

## 6. CLI - Update Call Sites That Ignore Path

- [ ] 6.1 Update task update flow in `Program.fs` (~line 738) to destructure `let (backlogItem, _) = ...`
- [ ] 6.2 Update backlog info / task list handler in `Program.fs` (~line 1416) to destructure `let (backlogItem, _) = ...`
- [ ] 6.3 Update `InteractivePrompts.fs` (~line 165) dependency multi-select to use `|> Result.map (List.map fst)`

## 7. CLI - Replace Inline Path Construction in Program.fs

- [ ] 7.1 Replace inline plan path in `handleTaskInfo` with `ItrTask.planFile coordRoot backlogId taskId`
- [ ] 7.2 Replace inline plan path in `handleTaskPlan` with `ItrTask.planFile coordRoot backlogId taskId`
- [ ] 7.3 Replace inline plan path in `handleTaskApprove` with `ItrTask.planFile coordRoot backlogId taskId`
- [ ] 7.4 Replace inline path in `handleBacklogAdd` (display path ~lines 1545, 1588) with `BacklogItem.itemFile coordRoot backlogId`
- [ ] 7.5 Replace inline path in `handleBacklogTake` (~line 1439) with `ItrTask.taskFile coordRoot backlogId taskId`

## 8. Usecase - Populate Path Field

- [ ] 8.1 Update `loadSnapshot` in `BacklogUsecase.fs`: destructure `(item, path)` from `ListBacklogItems` and `ListArchivedBacklogItems`; populate `Path` on `BacklogItemSummary`
- [ ] 8.2 Update `getBacklogItemDetail` in `BacklogUsecase.fs`: destructure `(item, path)` from `LoadBacklogItem` and `LoadArchivedBacklogItem`; populate `Path` on `BacklogItemDetail`

## 9. CLI - Update Output Formats

- [ ] 9.1 Update `backlog list` text output to emit 9 tab-separated columns: `id\ttype\tpriority\tstatus\tview\ttaskCount\tcreatedAt\ttitle\tpath`
- [ ] 9.2 Update `backlog list` JSON output to include `path` field
- [ ] 9.3 Update `backlog list` table output to include `Path` as last column
- [ ] 9.4 Update `backlog info` text output to add `path\t<path>` key-value line
- [ ] 9.5 Update `backlog info` JSON output to include `path` field
- [ ] 9.6 Update `backlog info` table output to add `Path` row

## 10. Tests and Acceptance

- [ ] 10.1 Update `tests/acceptance/TaskAcceptanceTests.fs` (~line 94) `LoadBacklogItem` call to destructure and ignore path
- [ ] 10.2 Run `dotnet build` and fix any compilation errors
- [ ] 10.3 Run `dotnet test` and verify no regressions
