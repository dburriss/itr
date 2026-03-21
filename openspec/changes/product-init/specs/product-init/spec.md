## ADDED Requirements

### Requirement: Scaffold new product on disk
The system SHALL create `product.yaml`, `PRODUCT.md`, `ARCHITECTURE.md`, and the coordination directory at a given path when `initProduct` is called with valid inputs.

#### Scenario: Valid inputs produce all expected files
- **WHEN** `initProduct` is called with a valid `Id`, an existing `Path`, a `RepoId`, `CoordPath` of `.itr`, and `RegisterProfile = None`
- **THEN** `<path>/product.yaml`, `<path>/PRODUCT.md`, `<path>/ARCHITECTURE.md`, and `<path>/.itr/.gitkeep` are written and `None` is returned

#### Scenario: product.yaml already exists returns error
- **WHEN** `<path>/product.yaml` already exists at the target path
- **THEN** a `ProductConfigError` is returned and no files are written or overwritten

#### Scenario: Target directory does not exist returns error
- **WHEN** the supplied `Path` does not exist on disk
- **THEN** a `ProductConfigError` is returned

#### Scenario: Invalid product id returns error
- **WHEN** `initProduct` is called with an `Id` that does not match `[a-z0-9][a-z0-9\-]*`
- **THEN** an `InvalidProductId` error is returned before any files are written

### Requirement: Generated product.yaml content
The `product.yaml` written by `initProduct` SHALL include `id`, `docs`, `repos`, and `coordination` sections populated from the supplied inputs.

#### Scenario: Primary-repo mode yaml
- **WHEN** `CoordinationMode` is `"primary-repo"` and `RepoId` is `"my-repo"` and `CoordPath` is `".itr"`
- **THEN** the written `product.yaml` contains `coordination.mode: primary-repo`, `coordination.repo: my-repo`, `coordination.path: .itr`, and `repos.my-repo.path: .`

#### Scenario: Standalone mode yaml omits coordination repo
- **WHEN** `CoordinationMode` is `"standalone"`
- **THEN** the written `product.yaml` contains `coordination.mode: standalone` and no `coordination.repo` field

### Requirement: Generated starter document content
`PRODUCT.md` and `ARCHITECTURE.md` written by `initProduct` SHALL contain minimal templates with the product id substituted.

#### Scenario: PRODUCT.md template
- **WHEN** `initProduct` is called with `Id` of `"my-product"`
- **THEN** `PRODUCT.md` contains `# Product: my-product` and a `## Purpose` section with a TODO placeholder

#### Scenario: ARCHITECTURE.md template
- **WHEN** `initProduct` is called with `Id` of `"my-product"`
- **THEN** `ARCHITECTURE.md` contains `# Architecture: my-product` and a `## Technology Stack` section with a TODO placeholder

### Requirement: Optional registration after scaffold
When `RegisterProfile` is `Some profile`, `initProduct` SHALL delegate to `registerProduct` logic to add the new product root to `itr.json` under the named profile and return the updated `Portfolio`.

#### Scenario: Registration requested returns updated portfolio
- **WHEN** `RegisterProfile` is `Some "default"` and `itr.json` exists with a `"default"` profile
- **THEN** the product root is appended to that profile and `Some updatedPortfolio` is returned

#### Scenario: Registration skipped returns None
- **WHEN** `RegisterProfile` is `None`
- **THEN** files are written, `itr.json` is not modified, and `None` is returned

#### Scenario: Duplicate product id in profile returns error
- **WHEN** `RegisterProfile` is `Some profile` and a product with the same root already exists in that profile
- **THEN** a `DuplicateProductId` (or equivalent) error is returned and `itr.json` is unchanged

### Requirement: CLI init subcommand
The CLI SHALL expose `itr product init <path> [id]` with optional flags `--repo-id`, `--coord-mode`, `--coord-path`, `--register-profile`, and `--no-register`. Missing required inputs SHALL be collected via interactive prompts.

#### Scenario: All inputs supplied via flags — no prompts
- **WHEN** `itr product init ./my-product my-prod --repo-id my-repo --no-register` is executed
- **THEN** scaffolding runs without any interactive prompt

#### Scenario: Missing id triggers prompt
- **WHEN** `itr product init ./my-product` is executed without an id argument
- **THEN** the CLI prompts "Product id:" before proceeding

#### Scenario: Missing repo-id triggers prompt
- **WHEN** neither `id` nor `--repo-id` flag supplies a repo id
- **THEN** the CLI prompts "Repo id (default: same as id):" before proceeding

#### Scenario: Success message printed
- **WHEN** `initProduct` succeeds
- **THEN** the CLI prints `Initialized product '<id>' at <path>.`

#### Scenario: Registration success message printed
- **WHEN** registration also succeeds
- **THEN** the CLI additionally prints `Registered in profile '<profile>'.`
