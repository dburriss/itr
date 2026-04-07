## Context

The `task list` command (`handleTaskList` in `src/cli/Program.fs`) currently hardcodes an implicit exclusion of archived tasks when no `--state` filter is provided (lines 562–566). There is no way to control sort order. The `filterTasks` function in `src/features/Task/TaskUsecase.fs` accepts optional backlog, repo, and state filters but has no exclude or ordering support.

## Goals / Non-Goals

**Goals:**
- Expose `--exclude <state>` on `task list` to let callers opt out of specific states
- Expose `--order-by <field>` on `task list` supporting `created` (asc by `CreatedAt`) and `state` (desc by priority order)
- Remove the hardcoded implicit archived exclusion; default shows all tasks
- Extend `filterTasks` with an `exclude` parameter so exclusion logic lives in the usecase, not the handler

**Non-Goals:**
- Changing any other command (backlog-list, backlog-add, etc.)
- Modifying task state transitions or domain types
- Changes to storage/YAML layer

## Decisions

### 1. Add `exclude` parameter to `filterTasks`
**Decision**: Add `(exclude: TaskState list)` as a new parameter to `filterTasks`.

**Rationale**: Keeps all filter logic in one place (the usecase), consistent with how `state`, `backlog`, and `repo` filters already work. The handler stays thin.

**Alternative considered**: Keep exclusion only in the handler. Rejected because it scatters filter logic and makes `filterTasks` an incomplete abstraction.

### 2. Remove hardcoded archived exclusion from handler
**Decision**: Remove lines 562–566 that pre-filter `allTasks` before `listTasks`/`filterTasks`. Default behavior becomes show-all.

**Rationale**: Exclusion is now explicit via `--exclude`; the implicit default was a hidden behavior that surprised users.

### 3. Default ordering: `created_at` ascending
**Decision**: When `--order-by` is absent, sort by `CreatedAt` oldest-first. This was an open question now resolved in the plan.

**Rationale**: Predictable, stable ordering; matches creation sequence.

### 4. `--order-by state` uses priority order (not alphabetical)
**Decision**: Mirror the `priorityOrder` pattern used in backlog usecase. Order: archived < validated < implemented < in_progress < approved < planned < planning (highest priority states first).

**Rationale**: Consistent with existing backlog-list ordering; task priority meaning of "state" is well established in the codebase.

### 5. Ordering applied after filtering, in the handler
**Decision**: Perform the sort in `handleTaskList` after calling `filterTasks`, rather than inside `filterTasks`.

**Rationale**: Ordering is a presentation concern, not a filter concern. Keeps `filterTasks` focused on predicate logic.

## Risks / Trade-offs

- **BREAKING default behavior change** → Users who rely on archived tasks being hidden must add `--exclude archived`. Mitigation: document clearly in help text.
- **`--state X --exclude X` yields empty results** → This is allowed (explicit user intent). No error is emitted.
- **`TaskListArgs` type change** → Adding `Exclude` and `Order_By` to the DU is backward-compatible at the CLI level (optional args).
