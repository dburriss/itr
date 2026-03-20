## Why

Tasks live in a top-level `TASKS/` tree separate from their parent backlog items, making the parent-child relationship implicit and preventing atomic archiving of a backlog item together with all its work. Moving tasks under their parent backlog folder fixes discoverability and unlocks clean lifecycle management.

## What Changes

- `BACKLOG/items/<id>.yaml` moves to `BACKLOG/<id>/item.yaml`
- Task files move from `TASKS/<backlog-id>/<task-id>-task.yaml` to `BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`
- Task plan files move from `TASKS/<backlog-id>/<task-id>-plan.md` to `BACKLOG/<backlog-id>/tasks/<task-id>/plan.md`
- Completed tasks gain a date prefix on their folder: `tasks/<date>-<task-id>/`
- Archived backlog items move atomically to `BACKLOG/archive/<date>-<backlog-id>/` (entire folder including tasks)
- Three existing archived items are migrated on disk; 19 active items are migrated
- `BACKLOG/items/` and `TASKS/` directories are removed after migration
- New `ArchiveBacklogItem` operation added to store interface
- `.opencode/command/plan.md` path references updated
- `docs/config-files.md` and `docs/lifecycles.md` updated

## Capabilities

### New Capabilities

- `backlog-item-archive`: Archive an entire backlog item atomically — move `BACKLOG/<id>/` to `BACKLOG/archive/<date>-<id>/` once all tasks are completed

### Modified Capabilities

- `backlog-take`: Task file path changes from `TASKS/<backlog-id>/<task-id>-task.yaml` to `BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`; requires directory creation before write

## Impact

- `src/adapters/YamlAdapter.fs` — four path constructions updated; new archive operation added
- `itr/` coordination directory — all existing files migrated to new layout
- `.opencode/command/plan.md` — two path references updated
- `docs/config-files.md`, `docs/lifecycles.md` — layout diagrams and path examples updated
- Unit and acceptance tests in `tests/` — existing path-dependent tests updated; new tests added for archive operation
