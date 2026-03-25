## 1. Domain Layer

- [ ] 1.1 Add `TaskSummary` type to `src/features/Task/TaskUsecase.fs` with fields `Task: ItrTask` and `PlanApproved: bool`
- [ ] 1.2 Implement `listTasks (tasks: ItrTask list) : TaskSummary list` — sets `PlanApproved = true` for `Approved | InProgress | Implemented | Validated | Archived`
- [ ] 1.3 Implement `filterTasks (backlogId: BacklogId option) (repo: RepoId option) (state: TaskState option) (summaries: TaskSummary list) : TaskSummary list` with AND semantics
- [ ] 1.4 Add unit tests in `tests/communication/` covering: empty list, each plan-approval state transition, and each filter parameter (including combined filters)

## 2. Interface

- [ ] 2.1 Add `abstract ListAllTasks: coordRoot: string -> Result<ItrTask list, BacklogError>` to `ITaskStore` in `src/domain/Interfaces.fs`

## 3. Adapter

- [ ] 3.1 Implement `ListAllTasks` in `TaskStoreAdapter` in `src/adapters/YamlAdapter.fs`
  - Enumerate subdirs of `<coordRoot>/BACKLOG/` (skip names starting with `_`); collect task yamls from `tasks/*/task.yaml`
  - Enumerate subdirs of `<coordRoot>/BACKLOG/_archive/`; collect task yamls from `tasks/*/task.yaml`
  - Concatenate results; return `Ok []` gracefully when either directory is absent
  - Reuse existing `mapTaskDto` logic

## 4. CLI Wiring

- [ ] 4.1 Add `TaskListArgs` Argu DU to `src/cli/Program.fs` with `--backlog-id` / `--backlog`, `--repo-id` / `--repo`, `--state`, `--output` / `-o`
- [ ] 4.2 Add `TaskArgs` Argu DU with `| [<CliPrefix(CliPrefix.None)>] List of ParseResults<TaskListArgs>`
- [ ] 4.3 Add `| [<CliPrefix(CliPrefix.None)>] Task of ParseResults<TaskArgs>` to top-level `CliArgs` union
- [ ] 4.4 Implement `handleTaskList` handler:
  - Parse and validate optional `--backlog`, `--repo`, `--state` filters
  - Resolve product/coord root via existing portfolio/profile helpers
  - Call `taskStore.ListAllTasks coordRoot`
  - Implicitly exclude `Archived` tasks when no `--state` filter provided
  - Call `Task.listTasks` then `Task.filterTasks`
  - Emit table or JSON output
- [ ] 4.5 Implement `ListAllTasks` in `AppDeps` and wire `Task` case into dispatch in `src/cli/Program.fs`

## 5. Output Formatting

- [ ] 5.1 Implement human-readable Spectre.Console `Table` output with columns: `Id`, `Backlog`, `Repo`, `State`, `Plan Approved` (state lowercase, plan approved as `yes`/`no`)
- [ ] 5.2 Emit `No tasks found.` when result list is empty
- [ ] 5.3 Implement JSON output (`--output json`) matching schema: `{ "tasks": [ { "id", "backlog", "repo", "state", "planApproved" } ] }`

## 6. Acceptance Tests

- [ ] 6.1 `task list shows all active tasks` — two tasks under different backlog items; verify both appear, no archived tasks
- [ ] 6.2 `task list filters by backlog id` — tasks under two backlog items; filter by one; verify single result set
- [ ] 6.3 `task list filters by repo` — two tasks for different repos; filter by one; verify single result
- [ ] 6.4 `task list filters by state` — tasks in different states; filter by `planning`; verify only planning tasks
- [ ] 6.5 `task list --state archived includes tasks from archived backlog items` — archive backlog item; verify its tasks appear with `--state archived` but not in unfiltered list
- [ ] 6.6 `task list json output is valid` — parse JSON; assert `tasks` array with expected fields
- [ ] 6.7 `task list no tasks returns empty message` — product with no tasks; verify `No tasks found.`
