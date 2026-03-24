## ADDED Requirements

### Requirement: Show detailed information for a single backlog item
The system SHALL provide a `backlog info <id>` CLI subcommand that loads and displays all fields of the specified non-archived backlog item.

#### Scenario: Valid id outputs item details
- **WHEN** `itr backlog info my-feature` is run and the item exists
- **THEN** the output includes id, title, type, priority, status, summary, acceptance criteria, dependencies, repos, and created date

#### Scenario: Unknown id returns non-zero exit
- **WHEN** `itr backlog info unknown-id` is run and no matching item exists
- **THEN** the command exits with a non-zero code and an error message referencing the id

#### Scenario: Invalid id format returns non-zero exit
- **WHEN** `itr backlog info INVALID_ID` is run with an id that does not match `[a-z0-9][a-z0-9-]*`
- **THEN** the command exits with a non-zero code and a format error message

### Requirement: Display associated tasks for the item
The system SHALL list all tasks for the backlog item as part of the `backlog info` output, showing each task's id, repo, and state.

#### Scenario: Item with tasks shows task list
- **WHEN** `itr backlog info my-feature` is run and the item has two tasks
- **THEN** both tasks appear in the output with their id, repo, and state

#### Scenario: Item with no tasks shows empty tasks section
- **WHEN** `itr backlog info my-feature` is run and the item has no tasks
- **THEN** the output includes a tasks section that is empty (no rows or empty array)

### Requirement: JSON output mode
The system SHALL accept `--output json` to emit a single JSON object with all item fields and a `tasks` array.

#### Scenario: JSON output contains all fields
- **WHEN** `itr backlog info my-feature --output json` is run
- **THEN** stdout contains a JSON object with fields: `id`, `title`, `type`, `priority`, `status`, `summary`, `acceptanceCriteria`, `dependencies`, `repos`, `createdAt`, and `tasks` (array of objects with `id`, `repo`, `state`)

#### Scenario: JSON output with no tasks has empty tasks array
- **WHEN** `itr backlog info my-feature --output json` is run and the item has no tasks
- **THEN** the `tasks` field is an empty JSON array `[]`

### Requirement: Display computed status
The system SHALL display the computed `BacklogItemStatus` for the item, derived from its tasks using the same logic as `backlog list`.

#### Scenario: Status reflects task states
- **WHEN** `itr backlog info my-feature` is run and all tasks are `Approved`
- **THEN** the status shown is `approved`

#### Scenario: No tasks yields Created status
- **WHEN** `itr backlog info my-feature` is run and the item has no tasks
- **THEN** the status shown is `created`
