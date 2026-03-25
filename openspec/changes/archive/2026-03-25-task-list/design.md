## Context

The `itr` tool coordinates work across repositories. Tasks live under `<coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`. Currently `ITaskStore.ListTasks` requires a `backlogId` and can only fetch tasks for one backlog item at a time. There is no product-wide task listing capability.

The codebase follows a clean layered architecture: domain types and interfaces (`src/domain/`), pure use-case functions (`src/features/`), YAML adapters (`src/adapters/`), and CLI wiring (`src/cli/Program.fs`). Existing commands (`backlog list`, `backlog take`) establish the composition patterns to follow.

## Goals / Non-Goals

**Goals:**
- Add a `ListAllTasks` method to `ITaskStore` that scans all backlog items (active and archived)
- Add pure `TaskSummary`, `listTasks`, and `filterTasks` functions to the task domain
- Add `itr task list` CLI subcommand with human-readable table and JSON output
- Support optional filters: `--backlog`, `--repo`, `--state`
- By default, exclude archived tasks; include them only when `--state archived` is explicitly passed

**Non-Goals:**
- Modifying existing `ListTasks` or `ListArchivedTasks` behaviour
- Task mutation operations
- Pagination or streaming for large task volumes

## Decisions

### 1. New `ListAllTasks` adapter method rather than composing existing methods

**Decision**: Add `ListAllTasks: coordRoot: string -> Result<ItrTask list, BacklogError>` to `ITaskStore`.

**Rationale**: The CLI handler should not orchestrate multiple adapter calls and zip results — that logic belongs in the adapter. Keeping the interface clean also makes testing simpler (one mock method, one result). The adapter scans both `<coordRoot>/BACKLOG/` (skip `_` prefixed dirs) and `<coordRoot>/BACKLOG/_archive/`, reusing the existing `mapTaskDto` logic from `ListTasks`.

**Alternative considered**: Have the CLI handler call `ListBacklogItems` + `ListArchivedBacklogItems` and then `ListTasks` per item. Rejected because it adds coordination logic to the CLI layer and doubles the number of disk reads.

### 2. Pure filter functions in `TaskUsecase.fs`

**Decision**: `filterTasks` takes optional parameters and is composable with `listTasks`.

**Rationale**: Mirrors the pattern used in `BacklogUsecase.fs`. Keeps the CLI handler thin (parse args → call domain → emit output). Filters combine as AND, matching user expectation.

### 3. Implicit exclusion of archived tasks

**Decision**: When no `--state` filter is provided, archived tasks are excluded in the CLI handler (not in the adapter or `filterTasks`). The `filterTasks` function is state-filter-agnostic.

**Rationale**: The adapter returns everything so tests can verify raw data. The CLI applies the user-visible default. This separation keeps the domain functions reusable without baking CLI policy into them.

### 4. Argu CLI structure: `Task` → `List` subcommand

**Decision**: Add `TaskArgs` and `TaskListArgs` DUs following the same nesting pattern as `BacklogArgs`.

**Rationale**: Consistent with existing CLI structure. Argu DU ordering does not affect parsing, so inserting `Task` alongside existing cases is safe.

## Risks / Trade-offs

- **Full directory scan on every call** → Acceptable for MVP; volumes are bounded by typical product sizes. Mitigation: same risk exists for `backlog list` today.
- **State string parsing for `--state` filter** → Unknown strings must return a clear error. Mitigation: map lowercase strings to `TaskState` in the handler; emit a descriptive parse error on unknown values.
- **Archived task visibility** → Users may be surprised that `--state archived` is the only way to see archived tasks. Mitigation: this matches the `backlog list` pattern and is documented in help text.
