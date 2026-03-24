# Plan: task-list

**Status:** Draft

---

## Description

Add a `task list` top-level CLI subcommand that displays tasks for a product. Output includes task id, source backlog item, repo, lifecycle state, and plan approval status. Supports both human-readable and structured JSON output, with optional filtering by backlog item, repo, or state.

By default, only non-archived tasks are shown. Archived tasks (including those belonging to archived backlog items) are accessible via `--state archived`.

"Plan approval status" is derived from `TaskState`: a task is plan-approved when its state is `Approved` or beyond (`InProgress`, `Implemented`, `Validated`, `Archived`).

---

## Scope

### 1. Domain: `TaskSummary` and `listTasks`

Add a pure function in `src/features/Task/TaskUsecase.fs`:

```fsharp
type TaskSummary =
    { Task: ItrTask
      PlanApproved: bool }

let listTasks (tasks: ItrTask list) : TaskSummary list
```

`PlanApproved` is `true` when `task.State` is one of `Approved | InProgress | Implemented | Validated | Archived`.

Filtering is also pure — accept optional filter parameters:

```fsharp
let filterTasks
    (backlogId: BacklogId option)
    (repo: RepoId option)
    (state: TaskState option)
    (summaries: TaskSummary list)
    : TaskSummary list
```

### 2. Interface: `ITaskStore.ListAllTasks`

The existing `ITaskStore.ListTasks` takes a `backlogId` parameter and only lists tasks under one backlog item. A product-wide listing requires iterating all backlog items. Add a new method to `ITaskStore` in `src/domain/Interfaces.fs`:

```fsharp
abstract ListAllTasks: coordRoot: string -> Result<ItrTask list, BacklogError>
```

The adapter scans both active and archived locations, reusing the existing `mapTaskDto` logic from `ListTasks`.

Wire into `AppDeps` in `src/cli/Program.fs`.

### 3. Adapter: implement `ListAllTasks`

In `src/adapters/YamlAdapter.fs`, add `ListAllTasks` to `TaskStoreAdapter`:

- Enumerate subdirs of `<coordRoot>/BACKLOG/` (skip names starting with `_`); for each collect task yamls from `tasks/*/task.yaml`.
- Enumerate subdirs of `<coordRoot>/BACKLOG/_archive/`; for each collect task yamls from `tasks/*/task.yaml`.
- Concatenate both result lists and return. Return `Ok []` gracefully when either directory is absent.

### 4. CLI: `task list` subcommand

Add to `src/cli/Program.fs`:

**Argu DUs:**

```fsharp
type TaskListArgs =
    | [<AltCommandLine("--backlog")>] Backlog_Id of backlog_id: string
    | [<AltCommandLine("--repo")>]    Repo_Id    of repo_id: string
    | State of state: string
    | [<AltCommandLine("-o")>]        Output     of output: string
    interface IArgParserTemplate with ...

type TaskArgs =
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<TaskListArgs>
    interface IArgParserTemplate with ...
```

Add `| [<CliPrefix(CliPrefix.None)>] Task of ParseResults<TaskArgs>` to the top-level `CliArgs` union.

**Handler (`handleTaskList`):**

1. Parse optional `--backlog`, `--repo`, `--state` filters (validate each if provided).
2. Resolve product/coord root via the shared portfolio/profile helpers.
3. Call `taskStore.ListAllTasks coordRoot`.
4. If no `--state` filter was provided, implicitly exclude tasks with state `Archived`.
5. Call `Task.listTasks tasks` then `Task.filterTasks`.
6. Emit output.

**Human output** (Spectre.Console `Table`):

```
 Id               Backlog          Repo    State       Plan Approved
 ─────────────────────────────────────────────────────────────────────
 my-feature       my-feature       itr     planning    no
 itr-other-feat   other-feature    itr     approved    yes
```

- `State` column shows the lowercase string form of `TaskState`.
- `Plan Approved` column shows `yes` / `no`.
- When no tasks exist (or filters yield no results): emit `No tasks found.`

**JSON output:**

```json
{
  "tasks": [
    {
      "id": "...",
      "backlog": "...",
      "repo": "...",
      "state": "...",
      "planApproved": true
    }
  ]
}
```

---

## Dependencies / Prerequisites

- `task-promotion` (backlog item declared as dependency in `item.yaml`) — `backlog take` is fully implemented; `ItrTask` and `ITaskStore` are in place. This task extends the existing task infrastructure.
- `backlog-list` — `loadSnapshot` and product resolution patterns are already established. The handler follows the same composition pattern.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/domain/Interfaces.fs` | Add `ListAllTasks` to `ITaskStore` |
| `src/adapters/YamlAdapter.fs` | Implement `ListAllTasks` in `TaskStoreAdapter` |
| `src/features/Task/TaskUsecase.fs` | Add `TaskSummary` type, `listTasks`, `filterTasks` |
| `src/cli/Program.fs` | Add `TaskListArgs`, `TaskArgs`, `handleTaskList`, wire `Task` into `CliArgs` dispatch, implement `ListAllTasks` in `AppDeps` |

No existing code is modified beyond the interface and `AppDeps` additions. All existing commands are unaffected.

---

## Acceptance Criteria

- [ ] `itr task list` (no filters) shows only non-archived tasks by default.
- [ ] `itr task list --backlog <id>` filters to tasks from that backlog item only.
- [ ] `itr task list --repo <id>` filters to tasks for that repo only.
- [ ] `itr task list --state <state>` filters to tasks in that lifecycle state.
- [ ] `itr task list --state archived` returns archived tasks, including tasks belonging to archived backlog items.
- [ ] Filters can be combined.
- [ ] When no tasks match, output is `No tasks found.`
- [ ] `itr task list --output json` emits valid JSON with the `tasks` array.
- [ ] `planApproved` is `true` for states `approved`, `in_progress`, `implemented`, `validated`, `archived`.
- [ ] Existing `backlog` commands are unaffected.

---

## Testing Strategy

### Acceptance tests (`tests/acceptance/`)

1. `task list shows all active tasks` — write two tasks under different backlog items; verify both appear and no archived tasks are included.
2. `task list filters by backlog id` — write tasks under two backlog items; filter by one; verify only matching tasks returned.
3. `task list filters by repo` — two tasks for different repos; filter by one repo; verify single result.
4. `task list filters by state` — tasks in different states; filter by `planning`; verify only planning tasks returned.
5. `task list --state archived includes tasks from archived backlog items` — archive a backlog item; verify its tasks appear with `--state archived` but not in the unfiltered list.
6. `task list json output is valid` — parse JSON; assert `tasks` array, each object has expected fields.
7. `task list no tasks returns empty message` — product with no tasks; verify `No tasks found.` output.

### Communication tests (`tests/communication/`)

`Task.listTasks` / `Task.filterTasks`:
- Empty task list → empty summaries.
- `planning` state → `PlanApproved = false`.
- `approved` state → `PlanApproved = true`.
- `in_progress` state → `PlanApproved = true`.
- Filter by backlog id matches correctly.
- Filter by repo matches correctly.
- Filter by state matches correctly.
- Combined filters applied as AND.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| `ListAllTasks` scans entire backlog dir; performance on large repos | Acceptable for MVP; all existing commands load similar volumes |
| Tasks in `_archive/` must be reachable for `--state archived` | `ListAllTasks` scans both active and `_archive` dirs; implicit filter excludes archived from default output |
| State string parsing for `--state` filter | Map lowercase strings to `TaskState` in the CLI handler; return a clear parse error on unknown values |
| Argu top-level `CliArgs` changes may shift existing parse paths | Argu DU ordering does not affect parsing; add `Task` case alongside existing cases |

---

## Open Questions

- None. Requirements are clear from `item.yaml` and existing codebase patterns.
