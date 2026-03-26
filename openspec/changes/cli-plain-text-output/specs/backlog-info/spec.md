## MODIFIED Requirements

### Requirement: JSON output mode
The system SHALL accept `--output json` to emit a single JSON object with all item fields and a `tasks` array.

#### Scenario: JSON output contains all fields
- **WHEN** `itr backlog info my-feature --output json` is run
- **THEN** stdout contains a JSON object with fields: `id`, `title`, `type`, `priority`, `status`, `summary`, `acceptanceCriteria`, `dependencies`, `repos`, `createdAt`, and `tasks` (array of objects with `id`, `repo`, `state`)

#### Scenario: JSON output with no tasks has empty tasks array
- **WHEN** `itr backlog info my-feature --output json` is run and the item has no tasks
- **THEN** the `tasks` field is an empty JSON array `[]`

## ADDED Requirements

### Requirement: Text output mode for backlog info
The system SHALL accept `--output text` on `backlog info` to emit one field per line as tab-separated `<key>\t<value>` pairs. Multi-value fields SHALL be comma-joined on a single line. Prose fields (`summary`, `acceptanceCriteria`) SHALL be omitted.

#### Scenario: Text output contains key-value lines
- **WHEN** `itr backlog info my-feature --output text` is run
- **THEN** stdout contains lines: `id\t<id>`, `type\t<type>`, `status\t<status>`, `priority\t<priority or ->`, `view\t<view or ->`, `repos\t<repo1>,<repo2>`, `createdAt\t<yyyy-MM-dd>`, `title\t<title>`, `taskCount\t<n>`, `dependencies\t<dep1>,<dep2>`, `dependedOnBy\t<rev1>,<rev2>`

#### Scenario: Optional fields show dash when absent
- **WHEN** an item has no priority, no view, no repos, no dependencies, and no dependedOnBy, and `--output text` is used
- **THEN** those fields show `-`

#### Scenario: Multi-value fields are comma-joined
- **WHEN** an item has multiple repos and `--output text` is used
- **THEN** the `repos` line is a single line with repo ids joined by commas
