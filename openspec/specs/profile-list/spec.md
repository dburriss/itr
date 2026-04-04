## ADDED Requirements

### Requirement: List profiles subcommand
The system SHALL provide a `profile list` subcommand that reads the portfolio from `itr.json` and outputs all registered profiles. Each row SHALL include: profile name (with a `*` prefix for the default profile in table and text output), git name, git email, and product count. If no profiles exist the command SHALL return an empty list without error.

#### Scenario: Profiles listed in table format
- **WHEN** the user runs `itr profile list` and profiles exist in `itr.json`
- **THEN** a table is printed with columns Name, Git Name, Git Email, Products and one row per profile

#### Scenario: Default profile marked with asterisk
- **WHEN** a profile is set as the default in `itr.json` and the user runs `itr profile list`
- **THEN** the default profile name is prefixed with `*` in table and text output

#### Scenario: Empty portfolio returns empty list
- **WHEN** `itr.json` exists but has no profiles and the user runs `itr profile list`
- **THEN** the command exits successfully with an empty table (or empty output) and no error message

#### Scenario: Profile with no git identity shows empty fields
- **WHEN** a profile has no git identity and the user runs `itr profile list`
- **THEN** the git name and git email columns are blank for that profile

### Requirement: Profile list output formats
The system SHALL support `--output` flag with values `table` (default), `json`, and `text`. An unrecognised value SHALL fall back to `table` output. JSON output SHALL be a JSON array where each element contains `name`, `isDefault`, `gitName`, `gitEmail` (nullable), and `productCount` fields.

#### Scenario: JSON output
- **WHEN** the user runs `itr profile list --output json`
- **THEN** a JSON array is printed with one object per profile containing `name`, `isDefault`, `gitName`, `gitEmail`, and `productCount`

#### Scenario: Text output
- **WHEN** the user runs `itr profile list --output text`
- **THEN** one line per profile is printed as tab-separated values in the format `<marker>\t<name>\t<gitName>\t<gitEmail>\t<productCount>` where `<marker>` is `*` for the default profile and a space for others

#### Scenario: Invalid output format falls back to table
- **WHEN** the user runs `itr profile list --output bogus`
- **THEN** table output is produced (same as default)
