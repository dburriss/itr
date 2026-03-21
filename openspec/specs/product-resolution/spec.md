## MODIFIED Requirements

### Requirement: Resolve product by ID within active profile
The system SHALL look up a product by its string ID within the active profile by loading `product.yaml` from each registered root path and matching the canonical `id`. A `ResolvedProduct` is returned with a `CoordinationRoot` derived from the canonical `product.yaml` `coordination` section.

#### Scenario: Product found and coord root exists
- **WHEN** a valid product ID is passed, a registered product root contains a `product.yaml` with a matching canonical `id`, and the derived coordination root directory exists on disk
- **THEN** a `ResolvedProduct` is returned with `CoordRoot.AbsolutePath` pointing to the correct `.itr` directory as derived from `product.yaml`

#### Scenario: Product not found
- **WHEN** a product ID is passed that does not match the canonical id in any registered root's `product.yaml`
- **THEN** a `ProductNotFound` error is returned containing the product ID

### Requirement: Coordination root path resolution for all three modes
The system SHALL derive the `.itr` directory path from the `coordination` block in `product.yaml` combined with the registered product root path. Path expansion SHALL replace `~` with the user home directory and expand environment variables before checking the filesystem.

#### Scenario: Standalone mode resolved
- **WHEN** `product.yaml` has `coordination.mode: standalone` and `coordination.path: .itr`, and `<product-root>/.itr` exists
- **THEN** `CoordRoot.AbsolutePath` is the absolute expanded path ending in `.itr`

#### Scenario: Primary-repo mode resolved
- **WHEN** `product.yaml` has `coordination.mode: primary-repo`, `coordination.repo: api`, `repos.api.path: api/`, and `<product-root>/api/.itr` exists
- **THEN** `CoordRoot.AbsolutePath` is the absolute expanded path to `<product-root>/api/.itr`

#### Scenario: Control-repo mode resolved
- **WHEN** `product.yaml` has `coordination.mode: control-repo`, `coordination.repo: infra`, `repos.infra.path: infra/`, and `<product-root>/infra/.itr` exists
- **THEN** `CoordRoot.AbsolutePath` is the absolute expanded path to `<product-root>/infra/.itr`

### Requirement: CoordRoot existence validation
The system SHALL verify that the derived `.itr` directory exists on disk. If the directory does not exist, a `CoordRootNotFound` error SHALL be returned containing the product ID and the expected path.

#### Scenario: itr directory missing
- **WHEN** a product's derived `.itr` path does not exist on disk
- **THEN** a `CoordRootNotFound` error is returned with the product ID and the expected path

### Requirement: Duplicate product registration detection
The system SHALL reject duplicate product registrations within a profile. Two registered root paths are considered duplicates if their `product.yaml` files share the same canonical `id`. A `DuplicateProductId` error SHALL be returned when a duplicate is detected.

#### Scenario: Duplicate canonical ids across two root paths
- **WHEN** two registered root paths both contain a `product.yaml` with `id: billing-system`
- **THEN** a `DuplicateProductId` error is returned containing the profile name and the duplicated id

#### Scenario: Distinct canonical ids
- **WHEN** two registered root paths contain `product.yaml` files with different canonical ids
- **THEN** both products load successfully with no error

### Requirement: dirExists is injected for testability
The `resolveProduct` use-case SHALL accept a `dirExists: string -> bool` function parameter (or equivalent dependency injection) so tests can stub the filesystem without real I/O.

#### Scenario: Injected dirExists used
- **WHEN** `resolveProduct` is called with a stub `dirExists` returning `true` for the expected path
- **THEN** a `ResolvedProduct` is returned without accessing the real filesystem

### Requirement: Full resolution pipeline
All entry points (CLI, TUI, MCP, Server) SHALL resolve products via the shared pipeline: `loadPortfolio → resolveActiveProfile → resolveProduct`. The `resolveProduct` step SHALL load `product.yaml` from the registered root path as part of resolution. No entry point SHALL duplicate resolution logic.

#### Scenario: Full pipeline succeeds
- **WHEN** a valid config exists, the profile resolves, a matching `product.yaml` is found, and the derived `.itr` directory exists
- **THEN** a `ResolvedProduct` is returned through the monadic pipeline without branching in the entry point

#### Scenario: Pipeline short-circuits on first error
- **WHEN** the config file is missing
- **THEN** a `ConfigNotFound` error is returned immediately without attempting profile or product resolution
