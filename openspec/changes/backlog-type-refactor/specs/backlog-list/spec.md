## MODIFIED Requirements

### Requirement: Filter by item type
The system SHALL accept a `--type <type>` flag to restrict output to items of the given type (`feature|bug|chore|refactor|spike`).

#### Scenario: Filter by type returns only matching items
- **WHEN** `itr backlog list --type bug` is run and only two of five items are bugs
- **THEN** only those two items appear in the output

#### Scenario: Filter by refactor type returns only refactor items
- **WHEN** `itr backlog list --type refactor` is run and two items have type `refactor`
- **THEN** only those two items appear in the output

### Requirement: List all backlog items
The system SHALL provide a `backlog list` CLI subcommand that outputs all backlog items (including archived) for the active product's coordination root, sorted by type (bug → feature → chore → refactor → spike), then priority (high → medium → low), then creation date ascending.

#### Scenario: No items returns empty output
- **WHEN** `itr backlog list` is run and no backlog items exist
- **THEN** the command exits successfully with an empty table (human) or empty array (JSON)

#### Scenario: Items sorted by type then priority then creation date
- **WHEN** `itr backlog list` is run with items of mixed types and priorities
- **THEN** the output lists bugs first, then features, then chores, then refactors, then spikes; within each type items are ordered high → medium → low priority, then oldest-first within the same priority

#### Scenario: Archived items are included by default
- **WHEN** `itr backlog list` is run and one item is under `BACKLOG/_archive/`
- **THEN** the archived item appears in the output with status `archived`
