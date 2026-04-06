## Context

`backlog list` currently loads all items (active + archived) into a flat snapshot sorted by `CreatedAt` ascending. The `listBacklogItems` function applies view/status/type filters but no ordering logic — it inherits the creation-date sort from `loadSnapshot`. Archived items are included by default, which is noisy for day-to-day use.

Two layers need changes:
1. **`BacklogListFilter`** — add `ExcludeStatuses: BacklogItemStatus list` and `OrderBy: string option`
2. **`listBacklogItems`** — apply exclusion then ordering (default multi-key, or view-order, or `--order-by` override)
3. **`loadSnapshot`** — remove the hardcoded `List.sortBy CreatedAt`; sorting is now `listBacklogItems`'s responsibility
4. **`Program.fs` / `ListArgs`** — two new Argu arguments: `--exclude` (multiple) and `--order-by`

## Goals / Non-Goals

**Goals:**
- All items (including archived) included by default; user controls exclusion via `--exclude`
- Multi-key default sort: type (bug→feature→chore→spike) → priority (high→medium→low) → created date asc
- View-defined ordering when `--view` is active
- `--exclude` flag to remove specific statuses
- `--order-by` flag to override default sort key

**Non-Goals:**
- Changes to `BacklogItemStatus.compute` or item storage
- View creation/modification
- Any output format changes (columns remain the same)

## Decisions

### 1. Sorting moved entirely to `listBacklogItems`

Keeping `loadSnapshot` unsorted avoids leaking presentation concerns into the data-loading layer and makes `listBacklogItems` the single authoritative place for ordering.

*Alternative*: Keep sort in `loadSnapshot` with a configurable comparator. Rejected — more complex, harder to test in isolation.

### 2. View ordering as primary key overrides default sort

When `--view` is passed, items are ordered by their index in `view.Items`. Items in the snapshot but not in the view (shouldn't normally exist but could after view edits) are appended at the end in default sort order.

*Alternative*: Ignore view order and use default sort even with `--view`. Rejected — the user explicitly chose a view which has intentional ordering.

### 3. `--order-by` overrides default entirely (single-key)

`--order-by created|priority|type` sorts by that single dimension only. It does not stack with other default keys.

*Alternative*: `--order-by` as a tiebreaker on top of default. Rejected — the plan spec says "regardless of type or priority", implying full replacement.

### 4. No default archived exclusion — user-driven via `--exclude`

`listBacklogItems` applies only what `filter.ExcludeStatuses` explicitly contains. There is no automatic addition of `Archived`. Users who want to hide archived items pass `--exclude archived`. This keeps `listBacklogItems` predictable and avoids silent data loss when `--view` is used with a view that contains archived items.

*Alternative*: Auto-exclude `Archived` unless `--status archived` is passed. Rejected — breaks `--view` when all view items are archived, and conflates CLI policy with pure filter logic.

### 5. Priority sort order: unknown values treated as lowest

Priority is `string option`; canonical values are "low"/"medium"/"high". Case-insensitive comparison. `None` and unrecognised strings sort below "low".

## Risks / Trade-offs

- **Priority string drift** — manual YAML edits may introduce non-canonical values. Mitigation: treat unknown as lowest priority (no crash).
- **View items referencing missing backlog items** — if a view references an item id not in the snapshot, it is silently ignored (the item simply isn't in the result set).
