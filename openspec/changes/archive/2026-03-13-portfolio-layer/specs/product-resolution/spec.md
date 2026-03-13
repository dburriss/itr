## ADDED Requirements

### Requirement: Resolve product by ID within active profile
The system SHALL look up a product by its string ID within the active profile's product list and return a `ResolvedProduct` with a validated `CoordinationRoot`.

#### Scenario: Product found and coord root exists
- **WHEN** a valid product ID is passed and its `.itr/` directory exists on disk
- **THEN** a `ResolvedProduct` is returned with `CoordRoot.AbsolutePath` pointing to the `.itr/` directory

#### Scenario: Product not found
- **WHEN** a product ID is passed that does not exist in the active profile
- **THEN** a `ProductNotFound` error is returned containing the product ID

### Requirement: Coordination root path resolution for all three modes
The system SHALL resolve the `.itr/` directory path by appending `/.itr` to the configured directory regardless of coordination mode (`standalone`, `primary-repo`, `control-repo`). Path expansion SHALL replace `~` with the user home directory and expand environment variables before checking the filesystem.

#### Scenario: Standalone mode resolved
- **WHEN** a product has `StandaloneConfig "~/projects/foo"` and `~/projects/foo/.itr` exists
- **THEN** `CoordRoot.AbsolutePath` is the absolute expanded path ending in `.itr`

#### Scenario: Primary-repo mode resolved
- **WHEN** a product has `PrimaryRepoConfig "~/repos/api"` and `~/repos/api/.itr` exists
- **THEN** `CoordRoot.AbsolutePath` is the absolute expanded path ending in `.itr`

#### Scenario: Control-repo mode resolved
- **WHEN** a product has `ControlRepoConfig "~/repos/coord"` and `~/repos/coord/.itr` exists
- **THEN** `CoordRoot.AbsolutePath` is the absolute expanded path ending in `.itr`

### Requirement: CoordRoot existence validation
The system SHALL verify that the resolved `.itr/` directory exists on disk. If the directory does not exist, a `CoordRootNotFound` error SHALL be returned containing the product ID and the expected path.

#### Scenario: itr directory missing
- **WHEN** a product's resolved `.itr/` path does not exist on disk
- **THEN** a `CoordRootNotFound` error is returned with the product ID and the expected path

### Requirement: dirExists is injected for testability
The `resolveProduct` use-case SHALL accept a `dirExists: string -> bool` function parameter so tests can stub the filesystem without real I/O.

#### Scenario: Injected dirExists used
- **WHEN** `resolveProduct` is called with a stub `dirExists` returning `true` for the expected path
- **THEN** a `ResolvedProduct` is returned without accessing the real filesystem

### Requirement: Full resolution pipeline
All entry points (CLI, TUI, MCP, Server) SHALL resolve products via the shared pipeline: `loadPortfolio → resolveActiveProfile → resolveProduct`. No entry point SHALL duplicate resolution logic.

#### Scenario: Full pipeline succeeds
- **WHEN** a valid config exists, the profile resolves, and the product's `.itr/` directory exists
- **THEN** a `ResolvedProduct` is returned through the monadic pipeline without branching in the entry point

#### Scenario: Pipeline short-circuits on first error
- **WHEN** the config file is missing
- **THEN** a `ConfigNotFound` error is returned immediately without attempting profile or product resolution
