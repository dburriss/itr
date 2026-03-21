## MODIFIED Requirements

### Requirement: Load portfolio from disk
The system SHALL load and deserialize a portfolio from a JSON file at the resolved config path.

#### Scenario: Valid config file exists
- **WHEN** a valid `itr.json` exists at the resolved path with path-string product entries
- **THEN** a `Portfolio` value is returned with all profiles and their registered root paths parsed

#### Scenario: Config file is missing
- **WHEN** no file exists at the resolved config path
- **THEN** a `ConfigNotFound` error is returned containing the expected path

#### Scenario: Config file is malformed JSON
- **WHEN** the file exists but contains invalid JSON or fails schema validation
- **THEN** a `ConfigParseError` error is returned containing the path and a descriptive message

### Requirement: Save portfolio to disk
The system SHALL serialize and write a `Portfolio` value to a JSON file at the provided config path. The write SHALL be performed via `IFileSystem` (not `System.IO` directly). On failure, a `ConfigParseError` SHALL be returned.

#### Scenario: Portfolio written successfully
- **WHEN** `SaveConfig` is called with a valid path and a `Portfolio` value
- **THEN** the JSON file is written at the given path and `Ok ()` is returned

#### Scenario: Portfolio round-trip lossless
- **WHEN** a portfolio is written with `SaveConfig` and then read back with `LoadConfig`
- **THEN** the deserialized value equals the original, including `defaultProfile` and all profiles
