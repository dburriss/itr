## ADDED Requirements

### Requirement: Load canonical product definition from product root
The system SHALL load `product.yaml` from a given product root directory and parse `id`, `repos`, `docs`, and `coordination` into a canonical product definition.

#### Scenario: Valid product.yaml at product root
- **WHEN** a `product.yaml` file exists at `<product-root>/product.yaml` with valid `id`, `repos`, `docs`, and `coordination` fields
- **THEN** a `ProductDefinition` value is returned containing all four sections

#### Scenario: product.yaml missing
- **WHEN** no `product.yaml` exists at the given product root
- **THEN** a `ProductConfigError` (or equivalent parse error) is returned containing the expected path

#### Scenario: product.yaml malformed
- **WHEN** the file exists but fails YAML parsing or schema validation
- **THEN** a parse error is returned with the path and a descriptive message

### Requirement: Canonical product id is a validated slug
The `id` field in `product.yaml` SHALL be validated against the pattern `[a-z0-9][a-z0-9\-]*` during parsing. Any invalid value SHALL produce an `InvalidProductId` error.

#### Scenario: Valid id slug
- **WHEN** `product.yaml` has `id: billing-system`
- **THEN** the definition parses successfully

#### Scenario: Invalid id (uppercase)
- **WHEN** `product.yaml` has `id: BillingSystem`
- **THEN** an `InvalidProductId` error is returned with the invalid value

### Requirement: Coordination root derived from coordination section
The system SHALL compute the effective `CoordinationRoot` from the `coordination` block in `product.yaml` combined with the registered product root path. Supported modes are `primary-repo`, `control-repo`, and `standalone`.

#### Scenario: Primary-repo mode
- **WHEN** `coordination.mode` is `primary-repo`, `coordination.repo` names a repo in `repos`, and the corresponding repo `path` is `api/`
- **THEN** the effective coordination root is `<product-root>/api/.itr`

#### Scenario: Standalone mode
- **WHEN** `coordination.mode` is `standalone` and `coordination.path` is `.itr`
- **THEN** the effective coordination root is `<product-root>/.itr`

#### Scenario: Control-repo mode
- **WHEN** `coordination.mode` is `control-repo`, `coordination.repo` names a repo in `repos`, and the corresponding repo `path` is `infra/`
- **THEN** the effective coordination root is `<product-root>/infra/.itr`

#### Scenario: Unknown coordination mode
- **WHEN** `coordination.mode` has an unrecognised value
- **THEN** a `ProductConfigError` is returned

### Requirement: Repo paths in canonical definition
The `repos` section of `product.yaml` SHALL map repo names to objects with at least a `path` field (relative to the product root). An optional `url` field MAY be present.

#### Scenario: Repo with path and url
- **WHEN** `repos.api` has `path: api/` and `url: git@github.com:org/api.git`
- **THEN** both fields are available in the loaded definition

#### Scenario: Repo with path only
- **WHEN** `repos.api` has only `path: api/`
- **THEN** the definition loads successfully with url as absent
