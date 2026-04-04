## Why

When working with tasks, users and tooling (e.g. AI agents) need to know the exact filesystem location of `task.yaml` and `plan.md` to open, edit, or reference them directly. Currently `task list` and `task info` output provides no path information, forcing users to manually reconstruct paths from task/backlog IDs.

## What Changes

- `ITaskStore.ListTasks` return type changes from `Result<ItrTask list, BacklogError>` to `Result<(ItrTask * string) list, BacklogError>` where the string is the absolute path to `task.yaml` **BREAKING**
- `TaskSummary` and `TaskDetail` gain two new fields: `TaskYamlPath: string` and `PlanMdPath: string option`
- `task list` text output changes from 5 columns to 6 columns (drops `planApproved`, adds `taskYamlPath` and `planMdPath`) **BREAKING**
- `task list` JSON output replaces `planApproved` field with `taskYamlPath` and `planMdPath` **BREAKING**
- `task list` table output replaces `Plan Approved` column with `Task YAML` and `Plan MD` columns
- `task info` output (all formats) gains `taskYamlPath` and `planMdPath` fields

## Capabilities

### New Capabilities

- `task-list`: Updated output for `task list` command to include absolute paths for `task.yaml` and `plan.md`
- `task-info`: Updated output for `task info` command to include absolute paths for `task.yaml` and `plan.md`

### Modified Capabilities

- `task-list`: Output format changes — drops `planApproved`, adds `taskYamlPath` and `planMdPath` columns/fields
- `task-info`: Output format changes — adds `taskYamlPath` and `planMdPath` to all output formats

## Impact

- `src/domain/Interfaces.fs` — `ITaskStore.ListTasks` return type
- `src/adapters/YamlAdapter.fs` — `ListTasks` implementation
- `src/features/Task/TaskUsecase.fs` — `TaskSummary`, `TaskDetail`, `listTasks`, `getTaskDetail`
- `src/cli/Program.fs` — `AppDeps` delegator, `handleTaskList`, `handleTaskInfo`, other `ListTasks` call sites
- Any callers of `ListTasks` that don't need paths must discard the path with `List.map fst`
