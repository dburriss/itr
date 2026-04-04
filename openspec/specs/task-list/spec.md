## ADDED Requirements

### Requirement: List all tasks across a product
The system SHALL provide a `task list` subcommand that retrieves all tasks across all backlog items in a product (both active and archived backlog items). By default, tasks in the `Archived` state SHALL be excluded from output unless explicitly requested.

#### Scenario: Default listing excludes archived tasks
- **WHEN** `itr task list` is run with no filters
- **THEN** the output contains all tasks whose state is not `Archived`

#### Scenario: Archived tasks are shown with explicit state filter
- **WHEN** `itr task list --state archived` is run
- **THEN** the output contains tasks in the `Archived` state, including those belonging to archived backlog items

#### Scenario: No tasks exist
- **WHEN** `itr task list` is run and the product has no tasks
- **THEN** the output is `No tasks found.`

### Requirement: Filter tasks by backlog item
The system SHALL support filtering `task list` output to tasks belonging to a specific backlog item via `--backlog <id>`.

#### Scenario: Filter by backlog id returns matching tasks
- **WHEN** `itr task list --backlog <id>` is run
- **THEN** only tasks whose `SourceBacklog` equals `<id>` are shown

#### Scenario: Filter by backlog id with no matches
- **WHEN** `itr task list --backlog <unknown-id>` is run
- **THEN** the output is `No tasks found.`

### Requirement: Filter tasks by repository
The system SHALL support filtering `task list` output to tasks for a specific repository via `--repo <id>`.

#### Scenario: Filter by repo returns matching tasks
- **WHEN** `itr task list --repo <id>` is run
- **THEN** only tasks whose `Repo` equals `<id>` are shown

#### Scenario: Filter by repo with no matches
- **WHEN** `itr task list --repo <unknown-repo>` is run
- **THEN** the output is `No tasks found.`

### Requirement: Filter tasks by lifecycle state
The system SHALL support filtering `task list` output to tasks in a specific lifecycle state via `--state <state>`.

#### Scenario: Filter by valid state returns matching tasks
- **WHEN** `itr task list --state planning` is run
- **THEN** only tasks in the `Planning` state are shown

#### Scenario: Filter by unknown state returns an error
- **WHEN** `itr task list --state unknownstate` is run
- **THEN** the command exits with a non-zero code and emits a descriptive error message

### Requirement: Combine multiple filters
The system SHALL support combining `--backlog`, `--repo`, and `--state` filters, applying them as a logical AND.

#### Scenario: Combined filters narrow results
- **WHEN** `itr task list --repo <id> --state planning` is run
- **THEN** only tasks matching both the repo and state criteria are shown

### Requirement: JSON output format
The system SHALL support `--output json` (or `-o json`) to emit task data as structured JSON.

#### Scenario: JSON output contains expected fields
- **WHEN** `itr task list --output json` is run
- **THEN** the output is valid JSON with a `tasks` array where each element has fields `id`, `repo`, `state`, `taskYamlPath`, and `planMdPath` (the `backlog` field SHALL NOT be present)

#### Scenario: planMdPath is present when plan.md exists
- **WHEN** a task has a `plan.md` file in its task directory
- **THEN** `planMdPath` is the absolute path string to that `plan.md` file in the JSON output

#### Scenario: planMdPath is absent when plan.md does not exist
- **WHEN** a task has no `plan.md` file in its task directory
- **THEN** `planMdPath` is `null` or an empty string in the JSON output

### Requirement: Human-readable table output
The system SHALL display `task list` results as a formatted table by default, with columns: `Id`, `Repo`, `State`, `Task YAML`, `Plan MD`.

#### Scenario: Table displays task fields including paths
- **WHEN** `itr task list` is run and tasks exist
- **THEN** the output is a table with columns `Id`, `Repo`, `State`, `Task YAML`, `Plan MD` and one row per task (the `Backlog` column SHALL NOT be present)

#### Scenario: State column is lowercase
- **WHEN** a task is in the `InProgress` state
- **THEN** the `State` column shows `in_progress` (lowercase)

#### Scenario: Plan MD column shows path when plan exists
- **WHEN** a task has a `plan.md` file
- **THEN** the `Plan MD` column shows the absolute path to that file

#### Scenario: Plan MD column is empty when plan absent
- **WHEN** a task has no `plan.md` file
- **THEN** the `Plan MD` column is empty

### Requirement: Text output mode for task list
The system SHALL accept `--output text` on `task list` to emit one task per line as tab-separated values in the order: `taskId`, `repoId`, `state`, `taskYamlPath`, `planMdPath`.

#### Scenario: Text output contains tab-separated fields with paths
- **WHEN** `itr task list --output text` is run against a fixture with known tasks
- **THEN** each line matches `<taskId>\t<repoId>\t<state>\t<taskYamlPath>\t<planMdPath>` (5 fields, no backlog field)

#### Scenario: planMdPath column is empty string when plan.md absent
- **WHEN** `itr task list --output text` is run and a task has no `plan.md`
- **THEN** the fifth tab-separated column for that task is an empty string

#### Scenario: No tasks produces no output lines
- **WHEN** `itr task list --output text` is run and no tasks exist
- **THEN** stdout is empty (no lines, no headers)

#### Scenario: Text output is suitable for awk processing
- **WHEN** `itr task list --output text | awk -F'\t' '{print $1}'` is run
- **THEN** each line of output contains exactly one task id
