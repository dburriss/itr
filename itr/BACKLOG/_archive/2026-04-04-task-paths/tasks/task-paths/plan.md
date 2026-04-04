

# Include absolute paths in task commands

**Task ID:** task-paths
**Backlog Item:** task-paths
**Repo:** itr

## Description

Include `task.yaml` and `plan.md` absolute paths in `task list` and `task info` output. Follows the same adapter-layer path construction pattern established by `backlog-item-path`.

## Scope

**Included:**
- Change `ITaskStore.ListTasks` to return `(ItrTask * string) list` (path = absolute `task.yaml` path)
- Update `YamlAdapter.fs` `ListTasks` to return the task alongside its `task.yaml` path (already scanned locally)
- Update `AppDeps` `ListTasks` delegator in `Program.fs` to forward the new return type
- Add `TaskYamlPath: string` and `PlanMdPath: string option` to `TaskSummary` in `TaskUsecase.fs`
- Add `TaskYamlPath: string` and `PlanMdPath: string option` to `TaskDetail` in `TaskUsecase.fs`
- Update `listTasks` to accept `(ItrTask * string) list`; derive `PlanMdPath` from `task.yaml` directory; check file existence
- Update `getTaskDetail` to accept and populate both path fields
- Update `handleTaskList` output (text, JSON, table): drop `planApproved`, add `taskYamlPath` and `planMdPath`
- Update `handleTaskInfo` output (text, JSON, table): add `taskYamlPath` and `planMdPath`
- Update other `ListTasks` call sites that don't need the path to discard it with `List.map fst`
- `planMdPath` is empty string in text output when `plan.md` does not exist

**Explicitly excluded:**
- No changes to `ItrTask` type
- No changes to task create/write/archive commands
- No changes to file system structure
- No data migrations required

## Steps

1. **`Interfaces.fs`** — change `ListTasks` return type:
   `Result<ItrTask list, BacklogError>` → `Result<(ItrTask * string) list, BacklogError>`

2. **`YamlAdapter.fs` — `ListTasks`** — the inner `let taskFile = Path.Combine(subdir, "task.yaml")` is already a local variable; return `(task, taskFile)` tuples instead of just `task`

3. **`Program.fs` — `AppDeps` delegator** — update the `ListTasks` pass-through to forward the new `(ItrTask * string) list` return type unchanged

4. **`TaskUsecase.fs` — types** — add fields to `TaskSummary` and `TaskDetail`:
   - `TaskYamlPath: string`
   - `PlanMdPath: string option`

5. **`TaskUsecase.fs` — `listTasks`** — change signature to accept `(ItrTask * string) list`; for each task:
   - `taskYamlPath` = the path string from the tuple
   - `planMdPath` = `System.IO.Path.Combine(System.IO.Path.GetDirectoryName(taskYamlPath), "plan.md")`; wrapped in `Some` only if the file exists, otherwise `None`

6. **`TaskUsecase.fs` — `getTaskDetail`** — add `taskYamlPath: string` parameter; derive `planMdPath` the same way; populate both fields on `TaskDetail`

7. **`Program.fs` — `handleTaskList`** — update call to `listTasks` (now receives tuples); update all three output formats:
   - Text: 6 columns — `taskId\tbacklogId\trepoId\tstate\ttaskYamlPath\tplanMdPath` (`planMdPath` is empty string when `None`)
   - JSON: replace `planApproved` with `taskYamlPath` and `planMdPath` fields (`planMdPath` is `null` or `""` when absent)
   - Table: replace `Plan Approved` column with `Task YAML` and `Plan MD` columns

8. **`Program.fs` — `handleTaskInfo`** — pass `taskYamlPath` to `getTaskDetail`; update all three output formats to display `taskYamlPath` and `planMdPath`

9. **Other `ListTasks` call sites** — any callers that don't need paths (e.g. task update flow, backlog info task listing) discard the path: `|> Result.map (List.map fst)`

10. **Build and test** — `dotnet build` then `dotnet test`

## Dependencies

- None (`ItrTask.taskFile` and `ItrTask.planFile` helpers already exist in `Domain.fs` but are not needed here — paths come directly from the adapter scan)

## Acceptance Criteria

- `ITaskStore.ListTasks` returns `(ItrTask * string) list`
- Paths are absolute and point to the correct `task.yaml` and `plan.md` files
- `planMdPath` is empty string (text output) / absent field or empty (JSON/table) when `plan.md` does not exist
- `task list --output text` emits exactly 6 tab-separated columns: `taskId\tbacklogId\trepoId\tstate\ttaskYamlPath\tplanMdPath`
- `task list --output json` includes `taskYamlPath` and `planMdPath` fields
- `task list --output table` includes `Task YAML` and `Plan MD` columns
- `task info` includes both paths in all three output formats
- All existing tests pass

## Impact

**Files changed:**
- `src/domain/Interfaces.fs` — update `ListTasks` return type on `ITaskStore`
- `src/adapters/YamlAdapter.fs` — return `(task, taskFile)` tuples from `ListTasks`
- `src/features/Task/TaskUsecase.fs` — add path fields to `TaskSummary` and `TaskDetail`; update `listTasks` and `getTaskDetail`
- `src/cli/Program.fs` — update `AppDeps` delegator; update `handleTaskList` and `handleTaskInfo` output; update non-path call sites

**Interfaces affected:**
- `ITaskStore.ListTasks` — return type changes
- `TaskSummary` type — two new path fields
- `TaskDetail` type — two new path fields
- CLI text output columns for `task list` (6 columns, `planApproved` removed)
- CLI output for `task info`

**Data migrations:** None required

## Risks

1. **`ITaskStore` interface change touches multiple call sites** — mitigated by the compiler catching all missed sites at build time
2. **Missing plan.md** — mitigated by checking file existence; empty string in text output, `None`/empty in other formats
3. **Output format compatibility** — mitigated by updating all three formats (text, JSON, table) in both handlers
