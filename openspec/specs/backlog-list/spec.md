### Requirement: List all backlog items
The system SHALL provide a `backlog list` CLI subcommand that outputs all backlog items (including archived) for the active product's coordination root, sorted by type (bug → feature → chore → refactor → spike), then priority (high → medium → low), then creation date ascending.

#### Scenario: No items returns empty output
- **WHEN** `itr backlog list` is run and no backlog items exist
- **THEN** the command exits successfully with an empty table (human) or empty array (JSON)

#### Scenario: Items sorted by type then priority then creation date
- **WHEN** `itr backlog list` is run with items of mixed types and priorities
- **THEN** the output lists bugs first, then features, then chores, then refactors, then spikes; within each type items are ordered high → medium → low priority, then oldest-first within the same priority

#### Scenario: Archived items are included by default
- **WHEN** `itr backlog list` is run and one item is under `BACKLOG/_archive/`
- **THEN** the archived item appears in the output with status `archived`

### Requirement: Filter by view
The system SHALL accept a `--view <view-id>` flag to restrict output to items belonging to the named view.

#### Scenario: Filter returns only matching items
- **WHEN** `itr backlog list --view my-view` is run and two of five items are in `my-view`
- **THEN** only those two items appear in the output

#### Scenario: Unknown view returns empty output
- **WHEN** `--view unknown-view` is specified and no view with that id exists
- **THEN** the command exits successfully with an empty result

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
- **THEN** items are ordered bug → feature → chore → refactor → spike, regardless of priority or creation date

### Requirement: Filter by computed status
The system SHALL accept a `--status <status>` flag to restrict output to items whose computed status matches.

#### Scenario: Filter by status returns only matching items
- **WHEN** `itr backlog list --status approved` is run
- **THEN** only items with computed status `Approved` appear in the output

#### Scenario: Requesting archived status includes archived items
- **WHEN** `itr backlog list --status archived` is run
- **THEN** archived items appear and non-archived items do not

### Requirement: Filter by item type
The system SHALL accept a `--type <type>` flag to restrict output to items of the given type (`feature|bug|chore|refactor|spike`).

#### Scenario: Filter by type returns only matching items
- **WHEN** `itr backlog list --type bug` is run and only two of five items are bugs
- **THEN** only those two items appear in the output

#### Scenario: Filter by refactor type returns only refactor items
- **WHEN** `itr backlog list --type refactor` is run and two items have type `refactor`
- **THEN** only those two items appear in the output

### Requirement: Human-readable table output
By default, `backlog list` SHALL render output as a Spectre.Console table with columns: `ID | Type | Priority | Status | View | Tasks | Created | Title | Path`.

#### Scenario: Default output is a table
- **WHEN** `itr backlog list` is run without `--output`
- **THEN** a human-readable table is printed to stdout with one row per item

#### Scenario: Table output includes Path column
- **WHEN** `itr backlog list` is run without `--output`
- **THEN** each row includes a `Path` column containing the absolute path to `item.yaml` for that item

### Requirement: JSON output mode
The system SHALL accept `--output json` to emit a JSON array of objects, one per item, with fields: `id`, `type`, `priority`, `status`, `view`, `taskCount`, `createdAt`, `path`.

#### Scenario: JSON output is valid and complete
- **WHEN** `itr backlog list --output json` is run with two items
- **THEN** stdout contains a JSON array with two objects, each containing all required fields including `path`

#### Scenario: JSON path field is absolute
- **WHEN** `itr backlog list --output json` is run
- **THEN** each object's `path` field is an absolute filesystem path to the item's `item.yaml` file

### Requirement: Text output mode for backlog list
The system SHALL accept `--output text` on `backlog list` to emit one item per line as tab-separated values in the order: `id`, `type`, `priority`, `status`, `view`, `taskCount`, `createdAt`, `title`, `path`.

#### Scenario: Text output contains tab-separated fields
- **WHEN** `itr backlog list --output text` is run against a fixture with known items
- **THEN** each line matches `<id>\t<type>\t<priority>\t<status>\t<view>\t<taskCount>\t<createdAt>\t<title>\t<path>`

#### Scenario: No items produces no output lines
- **WHEN** `itr backlog list --output text` is run and no items exist
- **THEN** stdout is empty (no lines, no headers)

#### Scenario: Priority field is dash when absent
- **WHEN** an item has no priority set and `--output text` is used
- **THEN** the priority column is `-`

#### Scenario: Path field is absolute
- **WHEN** `itr backlog list --output text` is run
- **THEN** the 9th column on each line is an absolute path to `item.yaml`

### Requirement: Computed BacklogItemStatus from task states
The system SHALL compute a `BacklogItemStatus` for each item based on its associated tasks' `TaskState` values, evaluated in priority order:
1. `Archived` — item is under `BACKLOG/_archive/`
2. `Completed` — tasks non-empty and all are `Implemented` or `Validated`
3. `InProgress` — any task is `InProgress`
4. `Approved` — tasks non-empty and all are `Approved` or further (but not yet `InProgress`)
5. `Planned` — tasks non-empty and all are `Planned` or further (but not yet `Approved`)
6. `Planning` — tasks exist but none have advanced past `Planning`
7. `Created` — no tasks

#### Scenario: No tasks yields Created
- **WHEN** an item has no task directories
- **THEN** its computed status is `Created`

#### Scenario: Tasks all in Planning yields Planning
- **WHEN** all tasks for an item have state `Planning`
- **THEN** its computed status is `Planning`

#### Scenario: Tasks all Planned yields Planned
- **WHEN** all tasks for an item have state `Planned`
- **THEN** its computed status is `Planned`

#### Scenario: Tasks all Approved yields Approved
- **WHEN** all tasks for an item have state `Approved`
- **THEN** its computed status is `Approved`

#### Scenario: Any task InProgress yields InProgress
- **WHEN** at least one task has state `InProgress`
- **THEN** its computed status is `InProgress`

#### Scenario: All tasks Implemented or Validated yields Completed
- **WHEN** all tasks have state `Implemented` or `Validated`
- **THEN** its computed status is `Completed`

### Requirement: Multi-view membership warning
The system SHALL warn on stderr when the same item id appears in more than one view file. First-match (alphabetically by filename) wins for `ViewId` assignment.

#### Scenario: Item in multiple views triggers warning
- **WHEN** `itr backlog list` is run and item `foo` appears in both `_views/a.yaml` and `_views/b.yaml`
- **THEN** a warning is printed to stderr and `foo` is shown with the view from `a.yaml`

#### Scenario: Missing views directory is not an error
- **WHEN** `itr backlog list` is run and `BACKLOG/_views/` does not exist
- **THEN** the command succeeds with no view assignments
