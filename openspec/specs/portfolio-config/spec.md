## MODIFIED Requirements

### Requirement: Portfolio JSON schema
The portfolio config SHALL use a `camelCase` JSON schema. Unknown fields SHALL be ignored (forward-compatible). The `defaultProfile` field is optional. The `profiles` object is required (but may be empty). Each product entry in `profiles[*].products` SHALL be a plain string representing the product root directory path. Product identity and coordination configuration SHALL NOT be stored in `itr.json`.

#### Scenario: Unknown fields ignored
- **WHEN** `itr.json` contains fields not in the schema
- **THEN** the file parses successfully and unknown fields are discarded

#### Scenario: Missing defaultProfile
- **WHEN** `itr.json` omits `defaultProfile`
- **THEN** the portfolio loads successfully with `DefaultProfile = None`

#### Scenario: Product entries are path strings
- **WHEN** `itr.json` has `"products": ["~/dev/billing-system", "~/dev/infra"]`
- **THEN** two product root paths are registered in the profile, with no embedded id or coordination mode

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

### Requirement: Portfolio config path resolution
The system SHALL resolve the portfolio config file path by checking the `ITR_HOME` environment variable first. If `ITR_HOME` is set and non-empty, the config path SHALL be `$ITR_HOME/itr.json`. Otherwise the config path SHALL be `~/.config/itr/itr.json` (where `~` expands to the current user's home directory).

#### Scenario: ITR_HOME is set
- **WHEN** the `ITR_HOME` environment variable is set to `/custom/path`
- **THEN** the resolved config path is `/custom/path/itr.json`

#### Scenario: ITR_HOME is not set
- **WHEN** `ITR_HOME` is absent or empty
- **THEN** the resolved config path is `<home>/.config/itr/itr.json`

## REMOVED Requirements

### Requirement: ProductId slug validation at parse time
**Reason**: Product ids are no longer stored in `itr.json`. Slug validation now happens when loading `product.yaml` via the `product-config` capability.
**Migration**: Slug validation is enforced at the `product-config` loading boundary instead.

### Requirement: Coordination root config JSON mapping
**Reason**: Coordination configuration is no longer persisted in `itr.json`. It is derived from the `coordination` section in `product.yaml` when resolving a product.
**Migration**: See `product-config` spec for coordination root derivation rules.
