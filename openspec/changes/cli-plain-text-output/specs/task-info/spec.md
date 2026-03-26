## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Text output mode for task info
The system SHALL accept `--output text` on `task info` to emit one field per line as tab-separated `<key>\t<value>` pairs. The branch field SHALL be `-` when no branch is associated.

#### Scenario: Text output contains key-value lines
- **WHEN** `itr task info <id> --output text` is run
- **THEN** stdout contains lines: `id\t<id>`, `state\t<state>`, `repo\t<repo>`, `backlog\t<backlog-id>`, `branch\t<branch or ->`

#### Scenario: Branch field is dash when absent
- **WHEN** a task has no associated branch and `--output text` is used
- **THEN** the `branch` line shows `-`
