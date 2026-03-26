## MODIFIED Requirements

### Requirement: JSON output format
The system SHALL support `--output json` (or `-o json`) to emit task data as structured JSON.

#### Scenario: JSON output contains expected fields
- **WHEN** `itr task list --output json` is run
- **THEN** the output is valid JSON with a `tasks` array where each element has fields `id`, `backlog`, `repo`, `state`, and `planApproved`

#### Scenario: planApproved is true for approved-and-beyond states
- **WHEN** a task has state `approved`, `in_progress`, `implemented`, `validated`, or `archived`
- **THEN** `planApproved` is `true` in the JSON output

#### Scenario: planApproved is false for pre-approval states
- **WHEN** a task has state `planning` or `planned`
- **THEN** `planApproved` is `false` in the JSON output

### Requirement: Human-readable table output
The system SHALL display `task list` results as a formatted table by default, with columns: `Id`, `Backlog`, `Repo`, `State`, `Plan Approved`.

#### Scenario: Table displays task fields
- **WHEN** `itr task list` is run and tasks exist
- **THEN** the output is a table with columns `Id`, `Backlog`, `Repo`, `State`, `Plan Approved` and one row per task

#### Scenario: State column is lowercase
- **WHEN** a task is in the `InProgress` state
- **THEN** the `State` column shows `in_progress` (lowercase)

#### Scenario: Plan Approved column shows yes/no
- **WHEN** a task has `planApproved = true`
- **THEN** the `Plan Approved` column shows `yes`
- **WHEN** a task has `planApproved = false`
- **THEN** the `Plan Approved` column shows `no`

## ADDED Requirements

### Requirement: Text output mode for task list
The system SHALL accept `--output text` on `task list` to emit one task per line as tab-separated values in the order: `id`, `state`, `repo`, `backlog-id`.

#### Scenario: Text output contains tab-separated fields
- **WHEN** `itr task list --output text` is run against a fixture with known tasks
- **THEN** each line matches `<id>\t<state>\t<repo>\t<backlog>`

#### Scenario: No tasks produces no output lines
- **WHEN** `itr task list --output text` is run and no tasks exist
- **THEN** stdout is empty (no lines, no headers)

#### Scenario: Text output is suitable for awk processing
- **WHEN** `itr task list --output text | awk -F'\t' '{print $1}'` is run
- **THEN** each line of output contains exactly one task id
