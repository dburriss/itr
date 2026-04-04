## MODIFIED Requirements

### Requirement: Show detailed information for a single backlog item
The system SHALL provide a `backlog info <id>` CLI subcommand that loads and displays all fields of the specified backlog item, whether active or archived, including the absolute path to `item.yaml`.

#### Scenario: Valid id outputs item details
- **WHEN** `itr backlog info my-feature` is run and the item exists
- **THEN** the output includes id, title, type, priority, status, summary, acceptance criteria, dependencies, repos, created date, and path

#### Scenario: Archived item outputs item details with archived status
- **WHEN** `itr backlog info my-feature` is run and the item has been archived
- **THEN** the output includes all item fields (including path to the archived location) and the status shown is `archived`

#### Scenario: Unknown id returns non-zero exit
- **WHEN** `itr backlog info unknown-id` is run and no matching item exists in either active or archive
- **THEN** the command exits with a non-zero code and an error message referencing the id

#### Scenario: Invalid id format returns non-zero exit
- **WHEN** `itr backlog info INVALID_ID` is run with an id that does not match `[a-z0-9][a-z0-9-]*`
- **THEN** the command exits with a non-zero code and a format error message

### Requirement: JSON output mode
The system SHALL accept `--output json` to emit a single JSON object with all item fields, a `tasks` array, and a `path` field.

#### Scenario: JSON output contains all fields
- **WHEN** `itr backlog info my-feature --output json` is run
- **THEN** stdout contains a JSON object with fields: `id`, `title`, `type`, `priority`, `status`, `summary`, `acceptanceCriteria`, `dependencies`, `repos`, `createdAt`, `path`, and `tasks` (array of objects with `id`, `repo`, `state`)

#### Scenario: JSON path field is absolute
- **WHEN** `itr backlog info my-feature --output json` is run
- **THEN** the `path` field is an absolute filesystem path to the item's `item.yaml` file

#### Scenario: JSON output with no tasks has empty tasks array
- **WHEN** `itr backlog info my-feature --output json` is run and the item has no tasks
- **THEN** the `tasks` field is an empty JSON array `[]`

### Requirement: Text output mode for backlog info
The system SHALL accept `--output text` on `backlog info` to emit one field per line as tab-separated `<key>\t<value>` pairs. Multi-value fields SHALL be comma-joined on a single line. Prose fields (`summary`, `acceptanceCriteria`) SHALL be omitted. A `path` key-value line SHALL be included.

#### Scenario: Text output contains key-value lines
- **WHEN** `itr backlog info my-feature --output text` is run
- **THEN** stdout contains lines: `id\t<id>`, `type\t<type>`, `status\t<status>`, `priority\t<priority or ->`, `view\t<view or ->`, `repos\t<repo1>,<repo2>`, `createdAt\t<yyyy-MM-dd>`, `title\t<title>`, `taskCount\t<n>`, `dependencies\t<dep1>,<dep2>`, `dependedOnBy\t<rev1>,<rev2>`, `path\t<absolute-path>`

#### Scenario: Optional fields show dash when absent
- **WHEN** an item has no priority, no view, no repos, no dependencies, and no dependedOnBy, and `--output text` is used
- **THEN** those fields show `-`

#### Scenario: Multi-value fields are comma-joined
- **WHEN** an item has multiple repos and `--output text` is used
- **THEN** the `repos` line is a single line with repo ids joined by commas

#### Scenario: Path field is absolute
- **WHEN** `itr backlog info my-feature --output text` is run
- **THEN** the `path` line contains an absolute filesystem path to `item.yaml`
