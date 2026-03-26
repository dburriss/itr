## ADDED Requirements

### Requirement: Show detailed information for a single task
The system SHALL provide a `task info <id>` subcommand that displays the full detail record for a single task, including: id, source backlog item, repo, lifecycle state, whether a `plan.md` exists in the task directory, whether the plan is approved (state is `approved`, `in_progress`, `implemented`, `validated`, or `archived`), creation date, and sibling tasks sharing the same source backlog item.

#### Scenario: Full detail is shown for a known task
- **WHEN** `itr task info <id>` is run with a valid existing task id
- **THEN** the output shows the task id, backlog item id, repo, state, plan exists flag, plan approved flag, creation date, and siblings section

#### Scenario: Task not found returns an error
- **WHEN** `itr task info unknown-id` is run
- **THEN** the command exits with a non-zero code and outputs `Task not found: unknown-id`

### Requirement: Plan existence detection
The system SHALL report `plan exists: yes` when the file `<coordRoot>/BACKLOG/<backlogId>/tasks/<taskId>/plan.md` exists on disk, and `no` otherwise.

#### Scenario: plan.md present
- **WHEN** `itr task info <id>` is run and `plan.md` exists in the task directory
- **THEN** the output shows `plan exists   yes`

#### Scenario: plan.md absent
- **WHEN** `itr task info <id>` is run and no `plan.md` exists in the task directory
- **THEN** the output shows `plan exists   no`

### Requirement: Plan approval status
The system SHALL report `plan approved: yes` when the task's lifecycle state is `approved`, `in_progress`, `implemented`, `validated`, or `archived`. For all other states the value SHALL be `no`.

#### Scenario: Plan approved for approved-and-beyond state
- **WHEN** the task has state `approved`, `in_progress`, `implemented`, `validated`, or `archived`
- **THEN** the output shows `plan approved   yes`

#### Scenario: Plan not approved for pre-approval state
- **WHEN** the task has state `planning` or `planned`
- **THEN** the output shows `plan approved   no`

### Requirement: Sibling task listing
The system SHALL list all other tasks that share the same source backlog item as the queried task. When no siblings exist, the siblings section SHALL indicate none.

#### Scenario: No siblings
- **WHEN** `itr task info <id>` is run and no other tasks share the same backlog item
- **THEN** the siblings section shows `(none)`

#### Scenario: Siblings are listed
- **WHEN** `itr task info <id>` is run and other tasks share the same backlog item
- **THEN** the siblings section lists each sibling with its id, repo, and state

### Requirement: JSON output for task info
The system SHALL support `--output json` to emit the task detail as structured JSON.

#### Scenario: JSON output contains all required fields
- **WHEN** `itr task info <id> --output json` is run with a valid task id
- **THEN** the output is valid JSON with fields `id`, `backlog`, `repo`, `state`, `planExists`, `planApproved`, `createdAt`, and `siblings` array

#### Scenario: JSON siblings array is empty when no siblings
- **WHEN** `itr task info <id> --output json` is run and there are no siblings
- **THEN** the `siblings` field is an empty array `[]`

#### Scenario: JSON siblings array contains sibling entries
- **WHEN** `itr task info <id> --output json` is run and siblings exist
- **THEN** each element in `siblings` has fields `id`, `repo`, and `state`
