## MODIFIED Requirements

### Requirement: Show detailed information for a single task
The system SHALL provide a `task info <id>` subcommand that displays the full detail record for a single task, including: id, source backlog item, repo, lifecycle state, whether a `plan.md` exists in the task directory, whether the plan is approved (state is `approved`, `in_progress`, `implemented`, `validated`, or `archived`), creation date, sibling tasks sharing the same source backlog item, the absolute path to `task.yaml`, and the absolute path to `plan.md` when it exists.

#### Scenario: Full detail is shown for a known task
- **WHEN** `itr task info <id>` is run with a valid existing task id
- **THEN** the output shows the task id, backlog item id, repo, state, plan exists flag, plan approved flag, creation date, siblings section, task yaml path, and plan md path

#### Scenario: Task not found returns an error
- **WHEN** `itr task info unknown-id` is run
- **THEN** the command exits with a non-zero code and outputs `Task not found: unknown-id`

### Requirement: JSON output for task info
The system SHALL support `--output json` to emit the task detail as structured JSON.

#### Scenario: JSON output contains all required fields
- **WHEN** `itr task info <id> --output json` is run with a valid task id
- **THEN** the output is valid JSON with fields `id`, `backlog`, `repo`, `state`, `planExists`, `planApproved`, `createdAt`, `siblings` array, `taskYamlPath`, and `planMdPath`

#### Scenario: JSON planMdPath is absent when plan.md does not exist
- **WHEN** `itr task info <id> --output json` is run and the task has no `plan.md`
- **THEN** `planMdPath` is `null` or an empty string in the JSON output

#### Scenario: JSON siblings array is empty when no siblings
- **WHEN** `itr task info <id> --output json` is run and there are no siblings
- **THEN** the `siblings` field is an empty array `[]`

#### Scenario: JSON siblings array contains sibling entries
- **WHEN** `itr task info <id> --output json` is run and siblings exist
- **THEN** each element in `siblings` has fields `id`, `repo`, and `state`

### Requirement: Text output mode for task info
The system SHALL accept `--output text` on `task info` to emit one field per line as tab-separated `<key>\t<value>` pairs. The branch field SHALL be `-` when no branch is associated. The `taskYamlPath` and `planMdPath` fields SHALL always be present; `planMdPath` value SHALL be an empty string when no `plan.md` exists.

#### Scenario: Text output contains key-value lines including paths
- **WHEN** `itr task info <id> --output text` is run
- **THEN** stdout contains lines: `id\t<id>`, `state\t<state>`, `repo\t<repo>`, `backlog\t<backlog-id>`, `branch\t<branch or ->`, `taskYamlPath\t<path>`, `planMdPath\t<path or empty>`

#### Scenario: Branch field is dash when absent
- **WHEN** a task has no associated branch and `--output text` is used
- **THEN** the `branch` line shows `-`

#### Scenario: planMdPath is empty string when plan.md absent in text output
- **WHEN** `itr task info <id> --output text` is run and the task has no `plan.md`
- **THEN** the `planMdPath` line shows `planMdPath\t`
