## MODIFIED Requirements

### Requirement: Human-readable table output
By default, `backlog list` SHALL render output as a Spectre.Console table with columns: `ID | Type | Priority | Status | View | Tasks | Created`.

#### Scenario: Default output is a table
- **WHEN** `itr backlog list` is run without `--output`
- **THEN** a human-readable table is printed to stdout with one row per item

### Requirement: JSON output mode
The system SHALL accept `--output json` to emit a JSON array of objects, one per item, with fields: `id`, `type`, `priority`, `status`, `view`, `taskCount`, `createdAt`.

#### Scenario: JSON output is valid and complete
- **WHEN** `itr backlog list --output json` is run with two items
- **THEN** stdout contains a JSON array with two objects, each containing all required fields

## ADDED Requirements

### Requirement: Text output mode for backlog list
The system SHALL accept `--output text` on `backlog list` to emit one item per line as tab-separated values in the order: `id`, `type`, `status`, `priority`, `title`.

#### Scenario: Text output contains tab-separated fields
- **WHEN** `itr backlog list --output text` is run against a fixture with known items
- **THEN** each line matches `<id>\t<type>\t<status>\t<priority>\t<title>`

#### Scenario: No items produces no output lines
- **WHEN** `itr backlog list --output text` is run and no items exist
- **THEN** stdout is empty (no lines, no headers)

#### Scenario: Priority field is dash when absent
- **WHEN** an item has no priority set and `--output text` is used
- **THEN** the priority column is `-`
