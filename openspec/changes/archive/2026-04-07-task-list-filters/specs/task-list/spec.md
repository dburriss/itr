## MODIFIED Requirements

### Requirement: List all tasks across a product
The system SHALL provide a `task list` subcommand that retrieves all tasks across all backlog items in a product (both active and archived backlog items). By default, ALL tasks including those in the `Archived` state SHALL be shown unless explicitly excluded.

#### Scenario: Default listing includes archived tasks
- **WHEN** `itr task list` is run with no filters
- **THEN** the output contains all tasks regardless of state, including `Archived` tasks

#### Scenario: Archived tasks are shown with explicit state filter
- **WHEN** `itr task list --state archived` is run
- **THEN** the output contains only tasks in the `Archived` state, including those belonging to archived backlog items

#### Scenario: No tasks exist
- **WHEN** `itr task list` is run and the product has no tasks
- **THEN** the output is `No tasks found.`

## ADDED Requirements

### Requirement: Exclude tasks by state
The system SHALL support `--exclude <state>` on `task list` to filter out tasks whose state matches the given value.

#### Scenario: Exclude archived hides archived tasks
- **WHEN** `itr task list --exclude archived` is run
- **THEN** the output contains no tasks in the `Archived` state

#### Scenario: Exclude with unknown state returns an error
- **WHEN** `itr task list --exclude unknownstate` is run
- **THEN** the command exits with a non-zero code and emits a descriptive error message

#### Scenario: Exclude and state filters can be combined
- **WHEN** `itr task list --state planning --exclude planning` is run
- **THEN** the output contains no tasks (both filters applied; explicit user intent, no error)

### Requirement: Order task list results
The system SHALL support `--order-by <field>` on `task list` where `<field>` is `created` or `state`.

#### Scenario: Order by created sorts ascending by creation date
- **WHEN** `itr task list --order-by created` is run and tasks with different creation dates exist
- **THEN** tasks are returned oldest-first (ascending by `CreatedAt`)

#### Scenario: Default order is by created ascending
- **WHEN** `itr task list` is run with no `--order-by` flag
- **THEN** tasks are returned oldest-first (ascending by `CreatedAt`)

#### Scenario: Order by state sorts by priority order descending
- **WHEN** `itr task list --order-by state` is run and tasks in different states exist
- **THEN** tasks are returned in descending priority order: `planning` before `planned` before `approved` before `in_progress` before `implemented` before `validated` before `archived`

#### Scenario: Order by unknown field returns an error
- **WHEN** `itr task list --order-by unknown` is run
- **THEN** the command exits with a non-zero code and emits a descriptive error message
