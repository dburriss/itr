## MODIFIED Requirements

### Requirement: JSON output format
The system SHALL support `--output json` (or `-o json`) to emit task data as structured JSON.

#### Scenario: JSON output contains expected fields
- **WHEN** `itr task list --output json` is run
- **THEN** the output is valid JSON with a `tasks` array where each element has fields `id`, `backlog`, `repo`, `state`, `taskYamlPath`, and `planMdPath`

#### Scenario: planMdPath is present when plan.md exists
- **WHEN** a task has a `plan.md` file in its task directory
- **THEN** `planMdPath` is the absolute path string to that `plan.md` file in the JSON output

#### Scenario: planMdPath is absent when plan.md does not exist
- **WHEN** a task has no `plan.md` file in its task directory
- **THEN** `planMdPath` is `null` or an empty string in the JSON output

### Requirement: Human-readable table output
The system SHALL display `task list` results as a formatted table by default, with columns: `Id`, `Backlog`, `Repo`, `State`, `Task YAML`, `Plan MD`.

#### Scenario: Table displays task fields including paths
- **WHEN** `itr task list` is run and tasks exist
- **THEN** the output is a table with columns `Id`, `Backlog`, `Repo`, `State`, `Task YAML`, `Plan MD` and one row per task

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
The system SHALL accept `--output text` on `task list` to emit one task per line as tab-separated values in the order: `taskId`, `backlogId`, `repoId`, `state`, `taskYamlPath`, `planMdPath`.

#### Scenario: Text output contains tab-separated fields with paths
- **WHEN** `itr task list --output text` is run against a fixture with known tasks
- **THEN** each line matches `<taskId>\t<backlogId>\t<repoId>\t<state>\t<taskYamlPath>\t<planMdPath>`

#### Scenario: planMdPath column is empty string when plan.md absent
- **WHEN** `itr task list --output text` is run and a task has no `plan.md`
- **THEN** the sixth tab-separated column for that task is an empty string

#### Scenario: No tasks produces no output lines
- **WHEN** `itr task list --output text` is run and no tasks exist
- **THEN** stdout is empty (no lines, no headers)

#### Scenario: Text output is suitable for awk processing
- **WHEN** `itr task list --output text | awk -F'\t' '{print $1}'` is run
- **THEN** each line of output contains exactly one task id
