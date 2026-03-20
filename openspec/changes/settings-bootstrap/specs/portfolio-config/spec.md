## MODIFIED Requirements

### Requirement: Portfolio config path resolution
The system SHALL resolve the portfolio config file path by checking the `ITR_HOME` environment variable first. If `ITR_HOME` is set and non-empty, the config path SHALL be `$ITR_HOME/itr.json`. Otherwise the config path SHALL be `~/.config/itr/itr.json` (where `~` expands to the current user's home directory).

#### Scenario: ITR_HOME is set
- **WHEN** the `ITR_HOME` environment variable is set to `/custom/path`
- **THEN** the resolved config path is `/custom/path/itr.json`

#### Scenario: ITR_HOME is not set
- **WHEN** `ITR_HOME` is absent or empty
- **THEN** the resolved config path is `<home>/.config/itr/itr.json`

### Requirement: Load portfolio from disk
The system SHALL load and deserialize a portfolio from a JSON file at the resolved config path.

#### Scenario: Valid config file exists
- **WHEN** a valid `itr.json` exists at the resolved path
- **THEN** a `Portfolio` value is returned with all profiles and products parsed

#### Scenario: Config file is missing
- **WHEN** no file exists at the resolved config path
- **THEN** a `ConfigNotFound` error is returned containing the expected path

#### Scenario: Config file is malformed JSON
- **WHEN** the file exists but contains invalid JSON or fails schema validation
- **THEN** a `ConfigParseError` error is returned containing the path and a descriptive message
