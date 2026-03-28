## ADDED Requirements

### Requirement: Approve a task plan
The system SHALL provide a `task approve <task-id>` subcommand that transitions a task from `planned` state to `approved` state, provided a `plan.md` artifact exists in the task directory.

#### Scenario: Approve a planned task with a plan artifact
- **WHEN** `itr task approve <task-id>` is run for a task in `planned` state and `plan.md` exists in the task directory
- **THEN** the task state is updated to `approved` in `task.yaml` and a confirmation message is printed to stdout

#### Scenario: Approval rejected when plan artifact is missing
- **WHEN** `itr task approve <task-id>` is run for a task in `planned` state and no `plan.md` exists in the task directory
- **THEN** the command exits with a non-zero code and an error message indicating the plan artifact does not exist

#### Scenario: Approval rejected for task not in planned or approved state
- **WHEN** `itr task approve <task-id>` is run for a task in a state other than `planned` or `approved` (e.g. `planning`, `in_progress`)
- **THEN** the command exits with a non-zero code and an error message indicating an invalid state transition

#### Scenario: Re-approving an already-approved task is idempotent
- **WHEN** `itr task approve <task-id>` is run for a task already in `approved` state
- **THEN** the command exits with code 0 and an informational message is printed indicating the task was already approved (no state change is made)

#### Scenario: Error for task not found
- **WHEN** `itr task approve unknown-id` is run and no such task exists
- **THEN** the command exits with a non-zero code and an appropriate error message is printed
