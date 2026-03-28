## MODIFIED Requirements

### Requirement: Planning blocked for tasks beyond planned state
The system SHALL reject `task plan` for tasks in states beyond `planned` (e.g. `approved`, `in_progress`, `implemented`, `validated`, `archived`), returning an error and writing no files.

#### Scenario: Error for task in approved state
- **WHEN** `itr task plan <task-id>` is run for a task with state `approved`
- **THEN** the command exits with a non-zero code, an error is printed indicating an invalid state transition for the task, and no files are written

#### Scenario: Error for task not found
- **WHEN** `itr task plan unknown-id` is run and no such task exists
- **THEN** the command exits with a non-zero code and outputs an appropriate error message
