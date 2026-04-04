### Requirement: Product list subcommand
The system SHALL provide a `product list` CLI subcommand (accessible as `itr product list`) that reads the portfolio from `itr.json`, resolves the active or specified profile, loads all registered product definitions, and outputs the product list. Each entry SHALL include: product id, repo count, and absolute path to the coordination directory.

#### Scenario: Products listed in table format
- **WHEN** the user runs `itr product list` and the active profile has registered products
- **THEN** a table is printed with columns Id, Repo Count, Coord Root and one row per product

#### Scenario: Products listed for explicit profile
- **WHEN** the user runs `itr product list --profile <name>` with a valid profile name
- **THEN** the products of that profile are listed

#### Scenario: Default profile used when no profile flag given
- **WHEN** the user runs `itr product list` without `--profile` and a default profile is set
- **THEN** the products of the default profile are listed

#### Scenario: Empty product list returns empty table
- **WHEN** the active profile has no registered products
- **THEN** the command exits successfully with an empty table (or empty output) and no error

### Requirement: Product list output formats
The system SHALL support `--output` flag with values `table` (default), `json`, and `text`. JSON output SHALL be a JSON array where each element contains `id`, `repoCount`, and `coordRoot` fields. Text output SHALL be tab-delimited with the format `id\trepoCount\tcoordRoot` (one line per product). An unrecognised value SHALL fall back to `table` output.

#### Scenario: JSON output
- **WHEN** the user runs `itr product list --output json`
- **THEN** a JSON array is printed with one object per product containing `id`, `repoCount`, and `coordRoot`

#### Scenario: Text output
- **WHEN** the user runs `itr product list --output text`
- **THEN** one line per product is printed in the tab-delimited format `<id>\t<repoCount>\t<coordRoot>`

#### Scenario: Invalid output format falls back to table
- **WHEN** the user runs `itr product list --output bogus`
- **THEN** table output is produced (same as default)

### Requirement: Product list error handling
The system SHALL return a meaningful error message when the portfolio has no profiles. If the specified profile does not exist, the system SHALL return an error indicating the profile was not found.

#### Scenario: No profiles in portfolio
- **WHEN** `itr.json` exists but has no profiles and the user runs `itr product list`
- **THEN** the command exits with an error: "No profiles found. Run 'itr profile add <name>' to create one."

#### Scenario: Specified profile not found
- **WHEN** the user runs `itr product list --profile nonexistent`
- **THEN** the command exits with a meaningful error indicating the profile was not found
