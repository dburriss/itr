

# Improvements to backlog-list when listing views

**Task ID:** backlog-view-improvements
**Backlog Item:** backlog-view-improvements
**Repo:** itr

## Description

Listing backlog should be improved so the order of items is more intuitive and useful.

## Scope

**Included:**
- CLI argument additions: `--exclude` for filtering states, `--order-by` for custom ordering
- Filtering logic changes to hide archived items by default (unless `--status archived` or `--status` specifies archived)
- Ordering logic: default ordering (type → priority → created date), view-defined ordering, and `--order-by` overrides
- Updates to `BacklogListFilter` type and `listBacklogItems` function

**Excluded:**
- Changes to how backlog items are stored or created
- Changes to `BacklogItemStatus.compute` logic
- Changes to view storage or manipulation (only reading view order)

## Steps

1. Add new CLI arguments to `ListArgs` in `Program.fs`:
   - Add `--exclude` argument (values: created, planning, planned, approved, in-progress, completed, archived); supports multiple values
   - Add `--order-by` argument (values: created, priority, type)
   
2. Update `BacklogListFilter` type in `BacklogUsecase.fs` to include:
   - `ExcludeStatuses: BacklogItemStatus list` for exclusion filtering (supports multiple)
   - `OrderBy: string option` for custom ordering

3. Modify `loadSnapshot` in `BacklogUsecase.fs`:
   - Change default to exclude archived items from snapshot (or add a flag to include)
   - Remove hardcoded sort by `CreatedAt` - move sorting to `listBacklogItems`

4. Implement default ordering in `listBacklogItems`:
   - Define priority order mapping (high > medium > low)
   - Define type order (bug > feature > chore > spike)
   - Sort by type, then priority, then created date (oldest first)

5. Implement view-based ordering:
   - When `filter.ViewId` is specified, order items according to the order in the view's `Items` list
   - Use the view's item order as primary sort key

6. Implement `--order-by` overrides:
   - `--order-by created`: sort by `CreatedAt` ascending
   - `--order-by priority`: sort by priority only (using priority order mapping)
   - `--order-by type`: sort by type only (using type order)

7. Implement `--exclude` filtering:
   - Add exclusion logic in `listBacklogItems` that filters out items where `Status` is in `ExcludeStatuses`
   - Valid values are the actual `BacklogItemStatus` DU cases: created, planning, planned, approved, in-progress, completed, archived

8. Update CLI handler `handleBacklogList` to parse new arguments and pass to filter

9. Update acceptance tests in `BacklogAcceptanceTests.fs` for new default behavior

10. Run existing tests and fix any failures

## Dependencies

- none

## Acceptance Criteria

- Running `backlog-list` does not show archived items by default
- Running `backlog-list --exclude xyz` excludes items in state xyz; `--exclude` can be specified multiple times for multiple statuses; valid values: created, planning, planned, approved, in-progress, completed, archived
- Running `backlog-list --state xyz` shows only tasks in state xyz, including archived ones if they are in that state
- When listing a view with --view, only show items that are part of that view, and order them by the order defined in the view, not by the default backlog ordering.
- Backlog items should be ordered by type (bug, then feature, then chore), then by priority (high, medium, low), then by creation date (oldest first).
- Do not show archived unless the user explicitly requests to see archived items with --status archived.
- When using --order-by created, items should be ordered by creation date regardless of type or priority.
- When using --order-by priority, items should be ordered by priority regardless of type or creation date.
- When using --order-by type, items should be ordered by type regardless of priority or creation date.

## Impact

- **Files changed:**
  - `src/cli/Program.fs` - Add new CLI arguments to `ListArgs` and update `handleBacklogList`
  - `src/features/Backlog/BacklogUsecase.fs` - Update `BacklogListFilter` type, modify `loadSnapshot` and `listBacklogItems`
  - `tests/acceptance/BacklogAcceptanceTests.fs` - Update tests for new default behavior

- **Interfaces affected:**
  - `BacklogListFilter` record type - Added fields for exclusion and ordering
  - CLI argument interface - New `--exclude` and `--order-by` options

- **Data migrations:** None required

- **Breaking changes:**
  - Default listing will no longer include archived items (previously test 8.8 shows both were included)
  - Default ordering changes from `CreatedAt` only to multi-key sorting

## Risks

1. **Priority string format inconsistency** - Priority is stored as `string option` with canonical values "low", "medium", "high" (from `InteractivePrompts.fs`). Other formats may exist from manual edits.
   - *Mitigation:* Use case-insensitive comparison; treat unknown values as lowest priority

2. **Backward compatibility** - Existing users may depend on current default behavior (showing archived items)
   - *Mitigation:* Consider adding `--include archived` flag, or document the change. -> don't worry about backwards compatibility for this change, it's an improvement to the default behavior

3. **View ordering with missing items** - If a view references items that don't exist, ordering may be inconsistent
   - *Mitigation:* Handle gracefully by placing missing items at end or ignoring. -> Display missing ones in order of the view, but add missing as the status.

## Open Questions

~~1. What priority string values are expected in the system? (e.g., "high", "medium", "low", or case-insensitive variants?)~~
**Answered:** Canonical values are `"low"`, `"medium"`, `"high"` (lowercase), defined in `InteractivePrompts.fs:139`. Priority is `string option` — use case-insensitive comparison and treat unknowns as lowest priority.

~~2. Should `--exclude` support multiple values (e.g., `--exclude-archived --exclude-active`)? If so, how are they combined (AND/OR)?~~
**Answered:** Yes, `--exclude` supports multiple values (pass the flag multiple times). Items are excluded if their status matches ANY of the specified values (OR logic). `BacklogListFilter` uses `ExcludeStatuses: BacklogItemStatus list`.

~~3. Should the "active" status for `--exclude active` mean "non-archived" or "in-progress + planned + approved + created + planning"?~~
**Answered:** "active" is not a valid status. Only actual `BacklogItemStatus` DU cases are supported: `Created`, `Planning`, `Planned`, `Approved`, `InProgress`, `Completed`, `Archived` (string forms: created, planning, planned, approved, in-progress, completed, archived).

~~4. Is there an existing preference for how priority strings are stored/validated?~~
**Answered:** No DU/enum exists. Priority is `string option` throughout. Canonical values are "low", "medium", "high". See `Domain.fs:173` and `InteractivePrompts.fs:139`.

~~5. Should `--status active` be supported as a filter (requiring interpretation of what "active" means)?~~
**Answered:** No. Only actual statuses are supported. There is no "active" status in `BacklogItemStatus`.