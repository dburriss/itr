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

### Requirement: Portfolio JSON schema
The portfolio config SHALL use a `camelCase` JSON schema. Unknown fields SHALL be ignored (forward-compatible). The `defaultProfile` field is optional. The `profiles` object is required (but may be empty).

#### Scenario: Unknown fields ignored
- **WHEN** `itr.json` contains fields not in the schema
- **THEN** the file parses successfully and unknown fields are discarded

#### Scenario: Missing defaultProfile
- **WHEN** `itr.json` omits `defaultProfile`
- **THEN** the portfolio loads successfully with `DefaultProfile = None`

### Requirement: ProductId slug validation at parse time
The system SHALL validate every `product.id` in the portfolio against the pattern `[a-z0-9][a-z0-9\-]*` during parsing. Any invalid ID SHALL result in an `InvalidProductId` error containing the invalid value and a description of the slug rules.

#### Scenario: Valid product ID
- **WHEN** a product has `"id": "my-lib"`
- **THEN** the product parses successfully

#### Scenario: Invalid product ID (uppercase)
- **WHEN** a product has `"id": "MyLib"`
- **THEN** an `InvalidProductId` error is returned with the value `"MyLib"` and a slug-rule description

#### Scenario: Duplicate product IDs within a profile
- **WHEN** two products in the same profile share the same `id`
- **THEN** an `InvalidProductId` (or equivalent parse-time error) is returned

### Requirement: Coordination root config JSON mapping
The system SHALL deserialize the `root` object in each product using the `mode` field to determine the DU case. Supported modes are `"standalone"` (requires `dir`), `"primary-repo"` (requires `repoDir`), and `"control-repo"` (requires `repoDir`).

#### Scenario: Standalone mode parsed
- **WHEN** a product root has `"mode": "standalone"` and `"dir": "~/foo"`
- **THEN** `CoordRoot` is `StandaloneConfig "~/foo"`

#### Scenario: Primary-repo mode parsed
- **WHEN** a product root has `"mode": "primary-repo"` and `"repoDir": "~/repos/api"`
- **THEN** `CoordRoot` is `PrimaryRepoConfig "~/repos/api"`

#### Scenario: Unknown mode
- **WHEN** a product root has an unrecognised `mode` value
- **THEN** a `ConfigParseError` is returned
