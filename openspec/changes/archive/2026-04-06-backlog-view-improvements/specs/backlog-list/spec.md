## MODIFIED Requirements

### Requirement: List all backlog items
The system SHALL provide a `backlog list` CLI subcommand that outputs all backlog items (including archived) for the active product's coordination root, sorted by type (bug → feature → chore → spike), then priority (high → medium → low), then creation date ascending.

#### Scenario: No items returns empty output
- **WHEN** `itr backlog list` is run and no backlog items exist
- **THEN** the command exits successfully with an empty table (human) or empty array (JSON)

#### Scenario: Items sorted by type then priority then creation date
- **WHEN** `itr backlog list` is run with items of mixed types and priorities
- **THEN** the output lists bugs first, then features, then chores, then spikes; within each type items are ordered high → medium → low priority, then oldest-first within the same priority

#### Scenario: Archived items are included by default
- **WHEN** `itr backlog list` is run and one item is under `BACKLOG/_archive/`
- **THEN** the archived item appears in the output with status `archived`

### Requirement: Filter by view
The system SHALL accept a `--view <view-id>` flag to restrict output to items belonging to the named view and order them by their position in the view's `Items` list.

#### Scenario: Filter returns only matching items ordered by view position
- **WHEN** `itr backlog list --view my-view` is run and the view defines an ordered item list
- **THEN** only those items appear in the output, ordered according to the view's `Items` sequence

#### Scenario: Unknown view returns empty output
- **WHEN** `--view unknown-view` is specified and no view with that id exists
- **THEN** the command exits successfully with an empty result

## ADDED Requirements

### Requirement: Exclude items by status
The system SHALL accept one or more `--exclude <status>` flags on `backlog list` to omit items whose computed status matches any of the specified values. Valid values mirror `BacklogItemStatus` string forms: `created`, `planning`, `planned`, `approved`, `in-progress`, `completed`, `archived`.

#### Scenario: Single exclude hides matching items
- **WHEN** `itr backlog list --exclude completed` is run and two items have status `Completed`
- **THEN** those two items do not appear in the output

#### Scenario: Multiple excludes hide all matching items
- **WHEN** `itr backlog list --exclude completed --exclude archived` is run
- **THEN** items with status `Completed` or `Archived` are both absent from the output

#### Scenario: Exclude does not affect items with other statuses
- **WHEN** `itr backlog list --exclude planning` is run and items exist with statuses `Created`, `Planning`, and `InProgress`
- **THEN** only items with status `Planning` are excluded; `Created` and `InProgress` items appear

### Requirement: Override sort order with --order-by
The system SHALL accept an `--order-by <key>` flag on `backlog list` to replace the default multi-key sort with a single-dimension sort. Valid values: `created`, `priority`, `type`.

#### Scenario: --order-by created sorts by creation date only
- **WHEN** `itr backlog list --order-by created` is run with items of mixed types and priorities
- **THEN** items are ordered by `CreatedAt` ascending regardless of type or priority

#### Scenario: --order-by priority sorts by priority only
- **WHEN** `itr backlog list --order-by priority` is run
- **THEN** items are ordered high → medium → low priority, regardless of type or creation date

#### Scenario: --order-by type sorts by type only
- **WHEN** `itr backlog list --order-by type` is run
- **THEN** items are ordered bug → feature → chore → spike, regardless of priority or creation date
