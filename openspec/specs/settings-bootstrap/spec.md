## ADDED Requirements

### Requirement: Bootstrap creates default config when absent
The system SHALL check whether the resolved global config file exists before loading the portfolio. If the file is absent, the system SHALL write a minimal default `itr.json` with content `{"defaultProfile": null, "profiles": {}}` to the resolved config path, creating parent directories as needed. The system SHALL return a structured `BootstrapWriteError` if the write fails.

#### Scenario: Config absent - file and directory created
- **WHEN** no file exists at the resolved config path and the parent directory does not exist
- **THEN** the parent directory is created, `itr.json` is written with default content, and the command continues normally

#### Scenario: Bootstrap is idempotent
- **WHEN** `itr.json` already exists at the resolved config path
- **THEN** the file is not overwritten and no message is printed

#### Scenario: Bootstrap write fails
- **WHEN** the resolved config path is not writable (e.g., permission denied)
- **THEN** a `BootstrapWriteError` containing the path and error message is returned and displayed to the user

### Requirement: Informational message on first create
The system SHALL print a message to stdout only when a new `itr.json` is created, informing the user of the file location and directing them to run `itr init` to configure profiles and products. No message SHALL be printed on subsequent runs when the file already exists.

#### Scenario: Message printed on creation
- **WHEN** `itr.json` is created by bootstrap
- **THEN** a message containing the config path and "itr init" is printed to stdout

#### Scenario: No message on existing file
- **WHEN** `itr.json` already exists and bootstrap is a no-op
- **THEN** nothing is printed
