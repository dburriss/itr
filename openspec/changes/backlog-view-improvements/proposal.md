## Why

The current `backlog list` command presents items in a flat creation-date sort, includes archived items by default, and ignores view-defined order when filtering by view. This makes the listing less actionable and harder to use in day-to-day workflows.

## What Changes

- **Default ordering** changes from creation-date-only to type → priority → created date (bug > feature > chore > spike; high > medium > low)
- **Archived items hidden by default** — archived items no longer appear unless `--status archived` is explicitly passed
- **View-defined ordering respected** — when `--view <id>` is used, items are ordered according to their position in the view's `Items` list
- **New `--exclude` flag** — allows callers to exclude one or more statuses (can be passed multiple times)
- **New `--order-by` flag** — overrides default ordering with `created`, `priority`, or `type`

## Capabilities

### New Capabilities

*(none — all changes are modifications to existing backlog-list behaviour)*

### Modified Capabilities

- `backlog-list`: Default ordering, archived-exclusion behaviour, view-order respect, and two new CLI flags (`--exclude`, `--order-by`) are being introduced as spec-level requirement changes.

## Impact

- `src/cli/Program.fs` — new `--exclude` and `--order-by` arguments added to `ListArgs`; `handleBacklogList` updated to pass them through
- `src/features/Backlog/BacklogUsecase.fs` — `BacklogListFilter` gains `ExcludeStatuses` and `OrderBy` fields; `loadSnapshot` default sort removed; `listBacklogItems` gains multi-key ordering and exclusion logic
- `tests/acceptance/BacklogAcceptanceTests.fs` — tests updated for new default behaviour (no archived by default, new sort order)
