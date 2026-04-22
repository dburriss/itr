## ADDED Requirements

### Requirement: Output format selection
The system SHALL support selecting output format (json, text, table) via a shared `OutputFormat` discriminated union, replacing any `bool outputJson` parameters.

#### Scenario: Parse valid format string
- **WHEN** caller passes `"json"`, `"text"`, or `"table"` to `parseOutputFormat`
- **THEN** the corresponding `OutputFormat` case is returned

#### Scenario: Parse invalid format string
- **WHEN** caller passes an unrecognised string to `parseOutputFormat`
- **THEN** an error or `None` is returned without throwing

### Requirement: Task output formatting
The system SHALL provide a `TaskFormatter` module that renders task data in the requested `OutputFormat`.

#### Scenario: JSON output
- **WHEN** `TaskFormatter.format` is called with `OutputFormat.Json`
- **THEN** the result is valid JSON representing the task(s)

#### Scenario: Table output
- **WHEN** `TaskFormatter.format` is called with `OutputFormat.Table`
- **THEN** the result is a Spectre.Console-compatible table string

#### Scenario: Text output
- **WHEN** `TaskFormatter.format` is called with `OutputFormat.Text`
- **THEN** the result is a human-readable plain-text representation

### Requirement: Backlog output formatting
The system SHALL provide a `BacklogFormatter` module that renders backlog item data in the requested `OutputFormat`.

#### Scenario: JSON output
- **WHEN** `BacklogFormatter.format` is called with `OutputFormat.Json`
- **THEN** the result is valid JSON representing the backlog item(s)

#### Scenario: Table output
- **WHEN** `BacklogFormatter.format` is called with `OutputFormat.Table`
- **THEN** the result is a Spectre.Console-compatible table string

### Requirement: Portfolio output formatting
The system SHALL provide a `PortfolioFormatter` module that renders portfolio/profile data in the requested `OutputFormat`.

#### Scenario: JSON output
- **WHEN** `PortfolioFormatter.format` is called with `OutputFormat.Json`
- **THEN** the result is valid JSON representing the portfolio data

#### Scenario: Text output
- **WHEN** `PortfolioFormatter.format` is called with `OutputFormat.Text`
- **THEN** the result is a human-readable plain-text representation

### Requirement: Formatter output matches pre-refactor output
Each formatter SHALL produce output byte-identical to the inline formatting it replaces for the same input data.

#### Scenario: Golden test for task list
- **WHEN** `TaskFormatter.format` is invoked with the same data that the old inline handler used
- **THEN** the output matches the previously recorded golden output
