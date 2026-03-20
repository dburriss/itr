## Context

The coordination directory currently uses two top-level trees: `BACKLOG/items/` for backlog item YAML files and a parallel `TASKS/<backlog-id>/` tree for task files. The relationship between a backlog item and its tasks is implicit — enforced only by the matching `<backlog-id>` directory name, not by filesystem co-location.

Path construction lives in two adapters in `src/adapters/YamlAdapter.fs` (`BacklogStoreAdapter` and `TaskStoreAdapter`) and the corresponding interfaces in `src/domain/Interfaces.fs`. No archive operation exists at the backlog-item level today; task archiving moves files into `TASKS/archive/`.

The `itr/` directory (the live coordination root) contains 22 active backlog item YAMLs and three already-archived items (each with task files).

## Goals / Non-Goals

**Goals:**
- Co-locate task files under their parent backlog item folder on disk
- Enable atomic archiving of a backlog item together with all its tasks
- Migrate existing `BACKLOG/items/` and `TASKS/` files to the new layout without data loss
- Keep the path change transparent to callers — only `YamlAdapter.fs` and `Interfaces.fs` need updating
- Update `.opencode/command/plan.md` and two docs files to reference new paths

**Non-Goals:**
- Changes to the domain model (types, `ItrTask`, `BacklogItem`) — only paths change
- UI or command-line interface changes beyond what is forced by the new archive operation
- Backwards compatibility shim — old paths are removed after migration

## Decisions

### 1. New directory layout

Tasks live at `BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml` (active) and `BACKLOG/<backlog-id>/tasks/<date>-<task-id>/task.yaml` (completed). Backlog items live at `BACKLOG/<backlog-id>/item.yaml`. Archived backlog items move atomically to `BACKLOG/archive/<date>-<backlog-id>/`.

**Rationale**: The date-prefix convention on completed task folders and archived backlog folders mirrors the openspec archive naming convention already in use and makes ordering by date trivial with `ls`.

**Alternative considered**: Keep `TASKS/` but add a symlink or manifest — rejected because it adds complexity without fixing the discoverability problem.

### 2. `ArchiveBacklogItem` added to `IBacklogStore`

The new operation is attached to `IBacklogStore` rather than `ITaskStore` because it operates at the backlog-item level (moves the whole `BACKLOG/<id>/` folder). Signature: `ArchiveBacklogItem: coordRoot: string -> backlogId: BacklogId -> date: string -> Result<unit, TakeError>`.

**Trigger condition**: caller is responsible for checking that no active (undated) task folders remain before calling; the adapter moves unconditionally.

**Rationale**: Keeping trigger logic in the use-case layer (not the adapter) is consistent with existing patterns.

### 3. Task write must create intermediate directories

`Directory.CreateDirectory` on the full `tasks/<task-id>/` path before writing `task.yaml`. This is safe to call even if the path already exists (idempotent on .NET).

### 4. Glob pattern change for `ListTasks`

Old: `Directory.GetFiles(dir, "*-task.yaml")` scanning flat `TASKS/<id>/`.  
New: `Directory.GetDirectories(dir, "*")` on `BACKLOG/<id>/tasks/`, then read `task.yaml` from each subdirectory (both active `<task-id>/` and completed `<date>-<task-id>/` folders).

**Rationale**: The subdirectory-per-task structure means files are no longer flat — a glob for `task.yaml` must descend one level.

### 5. Migration is a one-shot script / manual operation

The three archived items and 22 active items are migrated by file-system operations before code changes go live. No runtime migration code is added to the adapter. After migration, `BACKLOG/items/` and `TASKS/` are deleted.

**Rationale**: Keeping migration out of production code avoids a temporary compatibility path that would need to be removed later.

## Risks / Trade-offs

- **Migration interrupted mid-way** → Run as a shell script; verify new paths exist before deleting old dirs. If interrupted, re-run is safe because `Directory.Move` / `File.Copy` are idempotent when targeted paths don't exist yet.
- **`task-archive` change in-flight** → The new `ArchiveBacklogItem` operation in `IBacklogStore` is implemented here. The `task-archive` backlog item should adopt this interface rather than inventing its own; coordinate before merging.
- **Secondary repo has no `item.yaml`** → `LoadBacklogItem` returns `Error` if `item.yaml` is absent; callers in secondary repos must not call `LoadBacklogItem`. This is already the pattern — secondary repos only use `ITaskStore`.
- **Test fixtures reference old paths** → All `BACKLOG/items/` and `TASKS/` path strings in `tests/` must be updated. Risk is low given the path strings are explicit and grep-discoverable.

## Migration Plan

1. Run migration shell commands (in order):
   a. For each of the 3 archived items: create `BACKLOG/archive/<date>-<id>/tasks/<date>-<id>/`, copy `item.yaml` and task files, rename files to `task.yaml` / `plan.md`.
   b. For each of the 22 active items: create `BACKLOG/<id>/`, move `BACKLOG/items/<id>.yaml` → `BACKLOG/<id>/item.yaml`.
   c. For any active tasks under `TASKS/<id>/`: create `BACKLOG/<id>/tasks/<task-id>/`, move task and plan files.
   d. Verify all paths exist.
   e. Delete `BACKLOG/items/` and `TASKS/`.
2. Update `src/domain/Interfaces.fs` and `src/adapters/YamlAdapter.fs`.
3. Update `src/cli/Program.fs` wiring if needed.
4. Update tests.
5. Update `.opencode/command/plan.md`, `docs/config-files.md`, `docs/lifecycles.md`.
6. Run `dotnet build && dotnet test`.

**Rollback**: Before step (e), old directories still exist. If verification fails, restore from old dirs.

## Open Questions

- None — design is settled per the plan document.
