## ADDED Requirements

### Requirement: Set an existing profile as the default via CLI
The system SHALL allow users to set an existing named profile as the default via `itr profile set-default <name>`. The profile name SHALL be looked up case-insensitively. If the profile does not exist the command SHALL return a `ProfileNotFound` error and leave all config files unchanged.

#### Scenario: Profile set as default globally
- **WHEN** the user runs `itr profile set-default work --global` and a profile named `work` exists in the global `itr.json`
- **THEN** `defaultProfile` in `~/.config/itr/itr.json` is updated to `"work"` and the command prints `Profile 'work' set as default. (~/.config/itr/itr.json)`

#### Scenario: Profile set as default locally
- **WHEN** the user runs `itr profile set-default work --local` from within a product directory and a profile named `work` exists
- **THEN** `defaultProfile` in `<productRoot>/itr.json` is updated to `"work"` and the command prints `Profile 'work' set as default. (<productRoot>/itr.json)`

#### Scenario: Profile not found
- **WHEN** the user runs `itr profile set-default staging --global` but no profile named `staging` exists
- **THEN** the command returns the error `Profile 'staging' not found. Run 'profile add staging' to create it.` and no config file is modified

#### Scenario: Case-insensitive profile match
- **WHEN** the user runs `itr profile set-default WORK --global` and a profile named `work` exists
- **THEN** `defaultProfile` is updated to `"work"` (the stored name) and the success message uses the stored name

### Requirement: Auto-detect config file when no flag is given
When neither `--local` nor `--global` is specified the system SHALL detect the appropriate config file using local-over-global precedence: if a local `<productRoot>/itr.json` exists it SHALL be updated, otherwise the global `itr.json` SHALL be updated. The success message SHALL include the resolved file path.

#### Scenario: Auto-detect selects local when local config exists
- **WHEN** neither `--local` nor `--global` is passed, a product context is resolvable, and `<productRoot>/itr.json` exists
- **THEN** `defaultProfile` in the local file is updated and the output shows the local path

#### Scenario: Auto-detect falls back to global when no local config
- **WHEN** neither `--local` nor `--global` is passed and no local `itr.json` exists
- **THEN** `defaultProfile` in `~/.config/itr/itr.json` is updated and the output shows the global path

### Requirement: --local flag creates local itr.json if absent
When `--local` is specified and `<productRoot>/itr.json` does not yet exist the system SHALL create it, writing only the `defaultProfile` field. If the file exists the system SHALL merge by updating only the `defaultProfile` field without altering other content.

#### Scenario: Local file created on first use
- **WHEN** the user runs `itr profile set-default work --local` and no `<productRoot>/itr.json` exists
- **THEN** a new `<productRoot>/itr.json` is created containing `defaultProfile = "work"` and the command succeeds

#### Scenario: Local file updated without destroying existing content
- **WHEN** `<productRoot>/itr.json` already contains agent config and the user runs `itr profile set-default work --local`
- **THEN** `defaultProfile` is updated to `"work"` and all other fields in the file are preserved unchanged

### Requirement: --local requires a resolvable product context
When `--local` is specified but no product root can be resolved the system SHALL return an error without modifying any file.

#### Scenario: No product context with --local
- **WHEN** the user runs `itr profile set-default work --local` outside any product directory
- **THEN** the command returns `--local flag requires a product context. Run this command from within a product directory or specify --global instead.` and no file is modified

### Requirement: ProfileNotFound error is user-friendly
The system SHALL format a `ProfileNotFound` error as a human-readable message rather than a debug dump.

#### Scenario: ProfileNotFound formatted correctly
- **WHEN** a `ProfileNotFound "staging"` error is passed to the error formatter
- **THEN** the output is `Profile 'staging' not found. Run 'profile add staging' to create it.`
