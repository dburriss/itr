## ADDED Requirements

### Requirement: Add a named profile to the portfolio
The system SHALL allow users to add a new named profile to `itr.json` via `itr profiles add <name>`. The profile name SHALL be validated against the slug rule `[a-z0-9][a-z0-9-]*` before any I/O is performed. If the name is already present in the portfolio, the command SHALL return a `DuplicateProfileName` error and leave the file unchanged.

#### Scenario: Profile added successfully
- **WHEN** the user runs `itr profiles add my-work` and no profile named `my-work` exists
- **THEN** `itr.json` is updated with the new profile and the command prints `Added profile 'my-work'.`

#### Scenario: Duplicate profile name rejected
- **WHEN** the user runs `itr profiles add my-work` and a profile named `my-work` already exists
- **THEN** the command returns an error `Profile 'my-work' already exists.` and `itr.json` is unchanged

#### Scenario: Invalid profile name rejected
- **WHEN** the user runs `itr profiles add My Work` (uppercase or spaces)
- **THEN** the command returns an error describing the slug rule and `itr.json` is unchanged

#### Scenario: Blank profile name rejected
- **WHEN** the user runs `itr profiles add` with no name argument
- **THEN** Argu prints usage help and the command exits with a non-zero code

### Requirement: Profile name slug validation
The system SHALL validate profile names using `ProfileName.tryCreate`. A valid profile name SHALL match the pattern `[a-z0-9][a-z0-9-]*` (lowercase alphanumeric start, optionally followed by lowercase alphanumerics or hyphens). Blank or whitespace-only names SHALL be rejected. Names with uppercase letters, spaces, or special characters SHALL be rejected.

#### Scenario: Valid slug accepted
- **WHEN** `ProfileName.tryCreate` is called with `"my-profile"` or `"work"` or `"dev2"`
- **THEN** it returns `Ok` with the validated name

#### Scenario: Blank name rejected
- **WHEN** `ProfileName.tryCreate` is called with `""` or `"   "`
- **THEN** it returns `Error (InvalidProfileName(value, rules))`

#### Scenario: Uppercase name rejected
- **WHEN** `ProfileName.tryCreate` is called with `"MyProfile"` or `"WORK"`
- **THEN** it returns `Error (InvalidProfileName(value, rules))`

#### Scenario: Name with spaces rejected
- **WHEN** `ProfileName.tryCreate` is called with `"my profile"`
- **THEN** it returns `Error (InvalidProfileName(value, rules))`

### Requirement: Optional git identity on profile
The system SHALL allow an optional git identity to be associated with a profile at creation time via `--git-name` and `--git-email` flags. `--git-name` is required when any git identity flag is provided; `--git-email` is optional. If `--git-email` is provided without `--git-name`, the command SHALL return a validation error before calling the usecase.

#### Scenario: Git identity stored with profile
- **WHEN** the user runs `itr profiles add dev --git-name "Alice" --git-email "alice@example.com"`
- **THEN** the new profile in `itr.json` has `gitIdentity.name = "Alice"` and `gitIdentity.email = "alice@example.com"`

#### Scenario: Git name only (no email)
- **WHEN** the user runs `itr profiles add dev --git-name "Alice"`
- **THEN** the new profile has `gitIdentity.name = "Alice"` and no `gitIdentity.email` field

#### Scenario: Git email without git name rejected
- **WHEN** the user runs `itr profiles add dev --git-email "alice@example.com"` without `--git-name`
- **THEN** the command returns a validation error before writing to disk

### Requirement: Set profile as default
The system SHALL allow a profile to be set as the default at creation time via `--set-default`. When set, the `defaultProfile` field in `itr.json` SHALL be updated to the new profile name.

#### Scenario: Default profile set
- **WHEN** the user runs `itr profiles add work --set-default`
- **THEN** `itr.json` has `defaultProfile = "work"` after the command completes

#### Scenario: Default profile not changed when flag absent
- **WHEN** the user runs `itr profiles add work` without `--set-default`
- **THEN** the `defaultProfile` field in `itr.json` is unchanged

### Requirement: Existing profiles preserved after add
The system SHALL ensure all pre-existing profiles in `itr.json` are unchanged after adding a new profile (round-trip lossless).

#### Scenario: Existing profiles preserved
- **WHEN** `itr.json` has profile `"personal"` and the user runs `itr profiles add work`
- **THEN** `itr.json` contains both `"personal"` (unchanged) and the new `"work"` profile
