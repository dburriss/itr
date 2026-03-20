# Plan: Reorganise Coordination Directory — `task-under-backlog`

**Status: Draft**

---

## Description

Restructure the coordination directory so tasks live under their parent backlog item
folder rather than in a separate top-level `TASKS/` tree. The change makes the
parent-child relationship between a backlog item and its tasks explicit on disk,
improves discoverability for both humans and agents, and enables atomic archiving of
a backlog item together with all its tasks once work is complete.

---

## New Directory Layout

```
<coordRoot>/
  BACKLOG/
    _views/
      *.yaml                              (unchanged)
    _archive/
      <date>-<backlog-id>/                ← atomically archived backlog item
        item.yaml
        tasks/
          <date>-<task-id>/               ← completed task (date = completion date)
            task.yaml
            plan.md
    <backlog-id>/
      item.yaml                           ← backlog item (was BACKLOG/items/<id>.yaml)
      tasks/                              ← only present once item is taken
        <task-id>/                        ← active task (no date prefix)
          task.yaml                       ← was <task-id>-task.yaml
          plan.md                         ← was <task-id>-plan.md
        <date>-<task-id>/                 ← completed task (date prefix added on completion)
          task.yaml
          plan.md
```

### Rules

| State | Folder appearance |
|---|---|
| Backlog item not yet taken | `<id>/item.yaml` only, no `tasks/` subfolder |
| Active task | `<id>/tasks/<task-id>/` (no date prefix) |
| Completed task | `<id>/tasks/<date>-<task-id>/` (folder renamed on completion) |
| All tasks done — archive | entire `<id>/` moved to `archive/<date>-<id>/` |

### Multi-repo secondary repos

Secondary repos have no `item.yaml` — the backlog item lives only on the primary.
The folder still exists because a task was taken:

```
<coordRoot>/
  BACKLOG/
    <backlog-id>/
      tasks/
        <task-id>/
          task.yaml     ← scoped to this repo; source.backlog field carries the lineage
          plan.md
```

---

## Scope

### 1. Migrate existing files on disk

Three archived tasks exist. Dates are sourced from the matching openspec archive
folder names under `openspec/changes/archive/`.

| Item | Openspec archive date | Action |
|---|---|---|
| `portfolio-layer` | 2026-03-13 | Move `BACKLOG/items/portfolio-layer.yaml` → `BACKLOG/archive/2026-03-13-portfolio-layer/item.yaml`; move `TASKS/archive/portfolio-layer/` → `BACKLOG/archive/2026-03-13-portfolio-layer/tasks/2026-03-13-portfolio-layer/`; rename files to `task.yaml` / `plan.md` |
| `backlog-take` | 2026-03-20 | Same pattern — `BACKLOG/archive/2026-03-20-backlog-take/` |
| `settings-bootstrap` | 2026-03-20 | Same pattern — `BACKLOG/archive/2026-03-20-settings-bootstrap/` |

All remaining 19 backlog items in `BACKLOG/items/` move to `BACKLOG/<id>/item.yaml`.

Remove `BACKLOG/items/` and `TASKS/` once migration is complete and verified.

### 2. Update `YamlAdapter.fs`

Four path constructions change (`src/adapters/YamlAdapter.fs`):

| Operation | Old | New |
|---|---|---|
| Load backlog item | `BACKLOG/items/<id>.yaml` | `BACKLOG/<id>/item.yaml` |
| List tasks for item | `TASKS/<backlog-id>/` glob `*-task.yaml` | `BACKLOG/<backlog-id>/tasks/` glob `*/task.yaml` |
| Write new task | `TASKS/<backlog-id>/<task-id>-task.yaml` | `BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml` (create dir if absent) |
| Archive task | `TASKS/archive/<backlog-id>/<task-id>-*` | rename `tasks/<task-id>/` → `tasks/<date>-<task-id>/` in place |

The task write must `Directory.CreateDirectory` the full `tasks/<task-id>/` path before
writing — it will not exist for a freshly taken item.

### 3. Add backlog item archive operation to `YamlAdapter.fs`

New operation (not yet implemented):

- **Trigger**: all task folders under `<backlog-id>/tasks/` have a date prefix (i.e. none are active)
- **Action**: move `BACKLOG/<backlog-id>/` → `BACKLOG/_archive/<date>-<backlog-id>/` where `<date>` is today's date (the date the archive command is run)
- Expose via `ITaskStore` or a new `IBacklogStore.ArchiveBacklogItem` member

### 4. Update `plan.md` slash command

File: `.opencode/command/plan.md`

Two path references need updating:
- Task file path: `<itr root>/TASKS/<backlog-id>/<task-id>-task.yaml` → `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`
- Backlog item path: `<itr root>/BACKLOG/items/<backlog-id>.yaml` → `<itr root>/BACKLOG/<backlog-id>/item.yaml`
- Output path for plan: `<itr root>/TASKS/<backlog-id>/` → `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/`

### 5. Update documentation

File: `docs/config-files.md`
- Update the directory layout diagram to reflect the new structure
- Update path examples: `BACKLOG/items/<id>.yaml` → `BACKLOG/<id>/item.yaml` and `TASKS/<backlog-id>/<task-id>-task.yaml` → `BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`

File: `docs/lifecycles.md`
- Update `TASKS/archive/` path references to the new `BACKLOG/_archive/<id>/tasks/<date>-<task-id>/` convention

---

## Dependencies / Prerequisites

- `backlog-take` (archived) — establishes `IBacklogStore`, `ITaskStore`, `YamlAdapter.fs` path logic being changed here
- `task-archive` (backlog item, not yet implemented) — the new backlog-item-level archive operation partially overlaps; coordinate to avoid conflict

---

## Acceptance Criteria

- All 22 existing backlog item YAMLs are accessible at `BACKLOG/<id>/item.yaml`
- The three archived tasks are at `BACKLOG/_archive/<date>-<id>/tasks/<date>-<id>/task.yaml` and `plan.md`
- `BACKLOG/items/` directory is removed
- `TASKS/` directory is removed
- `itr backlog take <id>` writes task file to `BACKLOG/<id>/tasks/<task-id>/task.yaml`
- `itr task plan <id>` writes plan to `BACKLOG/<backlog-id>/tasks/<task-id>/plan.md`
- Completing a task renames `tasks/<task-id>/` to `tasks/<date>-<task-id>/`
- Once all tasks are completed, `itr task archive <backlog-id>` moves the whole backlog item folder to `BACKLOG/_archive/<date>-<backlog-id>/`
- Existing tests pass after path changes
- `plan.md` command references correct paths
- `docs/config-files.md` directory layout and path examples reflect the new structure
- `docs/lifecycles.md` archive path references reflect the new structure

---

## Testing Strategy

**Unit tests** (`tests/communication/`):
- Path construction for each of the four updated operations in `YamlAdapter.fs`
- Task write creates intermediate directories
- Archive operation: task folder rename; backlog item folder move

**Acceptance tests** (`tests/acceptance/`):
- Temp dir fixture with existing `BACKLOG/items/` layout — verify reads still work during migration
- Post-migration: `itr backlog take` writes to new path
- Post-migration: task completion renames folder correctly
- Post-migration: `itr task archive` moves backlog item folder atomically

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Migration leaves partial state if interrupted | Run migration as an atomic script; verify before deleting old dirs |
| `task-archive` backlog item is in-flight when this lands | Coordinate: implement new archive operation here, have `task-archive` adopt it |
| Secondary repo has no `item.yaml` — code must not require it | Load backlog item only on primary; secondary path construction must not assume `item.yaml` exists |
| Glob pattern change breaks task listing | Update glob from `*-task.yaml` to `*/task.yaml`; add test covering both active and date-prefixed task folders |

---

## Open Questions

- None — design is settled.
