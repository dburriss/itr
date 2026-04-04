### Requirement: Product info subcommand
The system SHALL provide a `product info [productId]` CLI subcommand (accessible as `itr product info`) that loads the product definition and outputs its details. When a product ID is provided, the system SHALL locate the product in the active profile by ID. When no ID is provided, the system SHALL traverse up from the current working directory to find a `product.yaml` file and load that product directly.

#### Scenario: Info displayed for explicit product ID
- **WHEN** the user runs `itr product info <id>` and the product exists in the active profile
- **THEN** the product details are printed for that product

#### Scenario: Info displayed for current directory product
- **WHEN** the user runs `itr product info` without an ID and the current directory is within a product directory tree (contains `product.yaml` in an ancestor)
- **THEN** the product details are printed for the detected product

#### Scenario: No ID and not in product directory
- **WHEN** the user runs `itr product info` without an ID and no `product.yaml` is found traversing up from the current directory
- **THEN** the command exits with an error: "No product ID provided and no product.yaml found in current directory or any parent directory."

#### Scenario: Product ID not found in profile
- **WHEN** the user runs `itr product info <id>` and no product with that ID exists in the active profile
- **THEN** the command exits with an error indicating the product was not found

### Requirement: Product info output fields
The system SHALL display the following fields for a product: `id`, `docs` (as absolute paths), `repos` (as absolute paths), `coord mode`, and `coord details` (repo and/or path if present).

#### Scenario: Docs shown as absolute paths
- **WHEN** the product has one or more doc entries configured
- **THEN** each doc is displayed with its resolved absolute path

#### Scenario: Repos shown with absolute paths
- **WHEN** the product has one or more repo entries configured
- **THEN** each repo is displayed with its id and resolved absolute path

#### Scenario: Coordination details shown
- **WHEN** the product info is displayed
- **THEN** the coord mode is shown, along with coord repo and coord path when present

### Requirement: Product info output formats
The system SHALL support `--output` flag with values `table` (default), `json`, and `text`. Text output SHALL be tab-delimited for easy shell parsing. An unrecognised value SHALL fall back to `table` output.

#### Scenario: Table output (default)
- **WHEN** the user runs `itr product info` without `--output` or with `--output table`
- **THEN** product details are printed in a formatted table

#### Scenario: JSON output
- **WHEN** the user runs `itr product info --output json`
- **THEN** a JSON object is printed with fields `id`, `docs` (object), `repos` (object with `path` and optional `url`), `coordMode`, `coordRepo`, `coordPath`

#### Scenario: Text output
- **WHEN** the user runs `itr product info --output text`
- **THEN** one tab-delimited line per field is printed; multi-value fields (docs, repos) produce one line each in format `<field>\t<key>\t<value>`

#### Scenario: Invalid output format falls back to table
- **WHEN** the user runs `itr product info --output bogus`
- **THEN** table output is produced (same as default)
