## MODIFIED Requirements

### Requirement: Missing or invalid inputs produce clear errors
The system SHALL surface configuration and input errors with specific, actionable messages. Errors are represented by the `BacklogError` type (formerly `TakeError`).

#### Scenario: Missing product.yaml produces an error
- **WHEN** no `product.yaml` exists at the coordination root
- **THEN** the command fails with `ProductConfigNotFound`

#### Scenario: Missing backlog item produces an error
- **WHEN** no backlog item file exists for the given `<backlog-id>`
- **THEN** the command fails with `BacklogItemNotFound <backlog-id>`
