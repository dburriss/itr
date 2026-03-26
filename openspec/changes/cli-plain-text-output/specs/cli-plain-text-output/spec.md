## ADDED Requirements

### Requirement: Plain-text output mode
The system SHALL accept `--output text` on all list and info commands, emitting tab-separated plain text with no ANSI escape codes, borders, or alignment padding.

#### Scenario: Unknown output value falls back to table
- **WHEN** an unknown value such as `--output csv` is passed
- **THEN** the command produces table output (default behaviour preserved)

#### Scenario: Text output contains no ANSI sequences
- **WHEN** any list or info command is run with `--output text`
- **THEN** stdout contains no ESC (0x1B) characters
