## MODIFIED Requirements

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
