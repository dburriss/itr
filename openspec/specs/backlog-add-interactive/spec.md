### Requirement: Interactive guided input for backlog add
The system SHALL accept an `--interactive` / `-i` flag on `itr backlog add` that, when present, prompts the user for each required and optional field using Spectre.Console prompts instead of requiring all arguments upfront.

#### Scenario: Interactive flag triggers prompts
- **WHEN** `itr backlog add --interactive` is run in a TTY environment with no other arguments
- **THEN** the user is prompted sequentially for backlog-id, title, type, priority, summary, repo(s), acceptance criteria, and dependencies before the item is created

#### Scenario: Explicit CLI args pre-fill and skip prompts
- **WHEN** `itr backlog add --interactive --title "My Feature"` is run
- **THEN** the title prompt is skipped and "My Feature" is used as the title, while all other fields are still prompted

#### Scenario: Single-repo product auto-fills repo prompt
- **WHEN** `--interactive` is used and the product has exactly one repo
- **THEN** the repo field is pre-filled with that repo and the repo prompt is skipped

#### Scenario: Multi-repo product shows repo selection
- **WHEN** `--interactive` is used and the product has more than one repo
- **THEN** a selection prompt shows all available repos and the user must choose one

#### Scenario: Type presented as selection prompt
- **WHEN** the user reaches the type prompt
- **THEN** a `SelectionPrompt` shows exactly `["feature", "bug", "chore", "spike"]` and the user selects one

#### Scenario: Priority presented as selection prompt
- **WHEN** the user reaches the priority prompt
- **THEN** a `SelectionPrompt` shows exactly `["low", "medium", "high"]` and the user selects one

#### Scenario: Dependencies presented as multi-select
- **WHEN** the user reaches the dependencies prompt
- **THEN** a `MultiSelectionPrompt` is shown populated with existing backlog item IDs sorted alphabetically, allowing zero or more selections

#### Scenario: Acceptance criteria collected in a loop
- **WHEN** the user reaches the acceptance criteria prompt
- **THEN** the system prompts for criteria entries one at a time and continues until the user provides an empty entry or signals completion

#### Scenario: Confirmation summary shown before creation
- **WHEN** all fields have been collected interactively
- **THEN** a summary of all entered values is displayed and the user is asked to confirm before the item is created

#### Scenario: Creation proceeds on confirmation
- **WHEN** the user confirms the summary
- **THEN** the backlog item is created and a success message is displayed

#### Scenario: Creation aborted on rejection
- **WHEN** the user declines the summary confirmation
- **THEN** no backlog item is created and the command exits with a message indicating the operation was cancelled

#### Scenario: Non-TTY environment produces an error
- **WHEN** `--interactive` is used but the input is redirected (non-TTY environment)
- **THEN** the command fails with a clear error message instructing the user to provide required fields as CLI arguments instead
