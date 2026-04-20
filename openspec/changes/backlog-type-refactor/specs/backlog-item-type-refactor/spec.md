## ADDED Requirements

### Requirement: Refactor is a valid backlog item type
The system SHALL recognize `refactor` as a valid `BacklogItemType` alongside `feature`, `bug`, `chore`, and `spike`. It SHALL parse the string `"refactor"` to the `Refactor` DU case and serialize `Refactor` back to the string `"refactor"`.

#### Scenario: Parse "refactor" succeeds
- **WHEN** `BacklogItemType.tryParse "refactor"` is called
- **THEN** it returns `Ok Refactor`

#### Scenario: Stringify Refactor produces "refactor"
- **WHEN** `BacklogItemType.toString Refactor` is called
- **THEN** it returns `"refactor"`

#### Scenario: Round-trip parse and stringify
- **WHEN** `BacklogItemType.tryParse "refactor"` is called and the result is passed to `BacklogItemType.toString`
- **THEN** the final string is `"refactor"`

### Requirement: Unknown types fail explicitly in YAML adapter
The system SHALL return a structured `Result` error when a YAML file contains an unrecognized `type` string, rather than silently falling back to `Feature`.

#### Scenario: Unknown type string returns error
- **WHEN** a YAML backlog item file contains `type: unknown-type`
- **THEN** the adapter returns an error result rather than defaulting to `Feature`

### Requirement: Refactor sort position is after Chore and before Spike
The system SHALL assign `Refactor` a sort order position of 3 (after Chore at position 2, before Spike at position 4) in the `typeOrder` function.

#### Scenario: Refactor sorts after Chore
- **WHEN** `backlog list` is run with one `chore` item and one `refactor` item
- **THEN** the `chore` item appears before the `refactor` item

#### Scenario: Refactor sorts before Spike
- **WHEN** `backlog list` is run with one `refactor` item and one `spike` item
- **THEN** the `refactor` item appears before the `spike` item

### Requirement: Refactor is selectable in the interactive TUI
The system SHALL include `"refactor"` in the type selection list presented during `itr backlog add --interactive`.

#### Scenario: TUI type selection includes refactor
- **WHEN** the user reaches the type selection prompt in `itr backlog add --interactive`
- **THEN** `"refactor"` appears as one of the selectable options

### Requirement: Error messages list refactor as a valid type
The system SHALL include `"refactor"` in all error messages that enumerate valid backlog item types.

#### Scenario: Invalid type error lists refactor
- **WHEN** the user provides an invalid `--item-type` or `--type` value
- **THEN** the error message lists `"refactor"` among the valid options
