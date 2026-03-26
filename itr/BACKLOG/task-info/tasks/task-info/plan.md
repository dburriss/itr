# Plan: task-info

**Status:** Draft

---

## Description

Add a `task info <id>` CLI subcommand that displays detailed information for a single task: its source backlog item, repo, lifecycle state, creation timestamp, whether a `plan.md` exists, whether the plan is approved (state ≥ `approved`), and sibling tasks that share the same source backlog item.

---

## Scope

### 1. Feature: `getTaskDetail` in `TaskUsecase.fs`

Add a pure function to `src/features/Task/TaskUsecase.fs`:

```fsharp
type TaskDetail =
    { Task: ItrTask
      PlanExists: bool
      PlanApproved: bool
      SiblingTasks: ItrTask list }

let getTaskDetail
    (taskId: TaskId)
    (allTasks: ItrTask list)
    (planExists: bool)
    : Result<TaskDetail, BacklogError>
```

Steps:
1. Find the task by id in `allTasks`; return `TaskNotFound` (new error case) if absent.
2. Compute `PlanApproved` using the same rule as `TaskSummary` (state ∈ `Approved | InProgress | Implemented | Validated | Archived`).
3. Collect `SiblingTasks` = all tasks whose `SourceBacklog = task.SourceBacklog` excluding the found task itself.
4. Return `TaskDetail`.

`planExists` is passed in from the entry point (it is an IO concern, not a domain concern).

### 2. Domain: new error case `TaskNotFound`

Add to `BacklogError` in `src/domain/Domain.fs`:

```fsharp
| TaskNotFound of TaskId
```

Update `formatBacklogError` in `src/cli/Program.fs` to handle the new case:

```fsharp
| TaskNotFound id -> $"Task not found: {TaskId.value id}"
```

### 3. CLI: `task info` subcommand

**Argu DU** (add to `src/cli/Program.fs`):

```fsharp
[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskInfoArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string
    | Output of output: string
    interface IArgParserTemplate with ...
```

Extend `TaskArgs`:

```fsharp
type TaskArgs =
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<TaskListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<TaskInfoArgs>
    interface IArgParserTemplate with ...
```

**Handler (`handleTaskInfo`):**

1. Parse `task-id`; validate with `TaskId.tryCreate`.
2. Resolve product/coord root via existing portfolio helpers.
3. Call `taskStore.ListAllTasks coordRoot` to get all tasks.
4. Determine `planExists`: check `IFileSystem.FileExists` for `<coordRoot>/BACKLOG/<backlogId>/tasks/<taskId>/plan.md`.
   - The backlog id is derived after finding the task (step 5), so: first find task in `allTasks` by id to get `SourceBacklog`, then check file path.
   - Alternative: call `getTaskDetail` first with `planExists = false`, then re-call with correct value. Better: compute task-level plan path after `ListAllTasks` but before `getTaskDetail`; look up task in list inline to get `SourceBacklog`, then check path, then call `getTaskDetail`.
5. Call `Task.getTaskDetail taskId allTasks planExists`.
6. Emit output.

**Human output** (Spectre.Console `Table`):

```
 Field         Value
 ─────────────────────────────────────
 id            task-info
 backlog       task-info
 repo          itr
 state         planning
 plan exists   no
 plan approved no
 created       2026-03-26

 Siblings
 ─────────────────────────────────
 (none)
```

Or if siblings exist, a second table with columns `Id | Repo | State`.

**JSON output:**

```json
{
  "id": "task-info",
  "backlog": "task-info",
  "repo": "itr",
  "state": "planning",
  "planExists": false,
  "planApproved": false,
  "createdAt": "2026-03-26",
  "siblings": []
}
```

---

## Dependencies / Prerequisites

- `task-add` (`backlog take`) — fully implemented; `ItrTask`, `ITaskStore`, and `ITaskStore.ListAllTasks` are in place.
- `task-list` — implemented; `TaskSummary`, `listTasks`, `filterTasks` are available. The handler follows the same product-resolution pattern.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/domain/Domain.fs` | Add `TaskNotFound of TaskId` to `BacklogError` |
| `src/features/Task/TaskUsecase.fs` | Add `TaskDetail` type and `getTaskDetail` function |
| `src/cli/Program.fs` | Add `TaskInfoArgs`, extend `TaskArgs` with `Info`, add `handleTaskInfo`, wire dispatch, add `TaskNotFound` to `formatBacklogError` |

No existing commands are affected. The `ITaskStore` interface does not change.

---

## Acceptance Criteria

- [ ] `itr task info <id>` shows task id, source backlog item, repo, state, plan existence, plan approval, and creation date.
- [ ] `plan exists` is `yes` when `<coordRoot>/BACKLOG/<backlogId>/tasks/<taskId>/plan.md` exists.
- [ ] `plan approved` is `yes` when state is `approved`, `in_progress`, `implemented`, `validated`, or `archived`.
- [ ] Siblings section lists other tasks sharing the same source backlog item (excluding the queried task itself).
- [ ] When no siblings exist, the section indicates none.
- [ ] `itr task info <id> --output json` emits valid JSON with all required fields and a `siblings` array.
- [ ] `itr task info unknown-id` exits with `Task not found: unknown-id`.
- [ ] `itr task list` and `itr backlog` commands are unaffected.

---

## Testing Strategy

### Acceptance tests (`tests/acceptance/`)

1. `task info shows full detail` — write a task via `backlog take`; call `task info <id>`; verify all fields present and correct.
2. `task info plan exists when plan.md present` — write a `plan.md` file in the task directory; verify `plan exists: yes`.
3. `task info shows siblings` — take two repos from a multi-repo backlog item; call `task info` for one; verify the other appears as a sibling.
4. `task info json output is valid` — parse JSON; assert all required fields.
5. `task info returns error for unknown id` — verify exit code and error message.

### Communication tests (`tests/communication/`)

`getTaskDetail`:
- Task found → correct `TaskDetail` returned.
- Task not found → `TaskNotFound`.
- Sibling detection: tasks with same `SourceBacklog` are siblings; task itself is excluded.
- `PlanApproved = false` for `Planning` state.
- `PlanApproved = true` for `Approved` state.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| `BacklogError.TaskNotFound` addition is a breaking change in exhaustive pattern matches | Compiler flags all match sites; add the case to `formatBacklogError` and any test matches |
| Plan file path depends on finding the task first (chicken-and-egg with `getTaskDetail`) | Resolve inline: scan `allTasks` list to get `SourceBacklog` before calling `getTaskDetail`, then construct plan path |
| `ListAllTasks` includes archived tasks; querying an archived task's info should still work | `ListAllTasks` already scans both active and archive dirs — no special handling needed |

---

## Open Questions

- None. Requirements are clear from `item.yaml` and existing codebase patterns.
