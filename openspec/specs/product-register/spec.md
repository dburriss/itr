## Purpose

Defines the behaviour for registering a product root in the active (or named) profile within `itr.json`. A product is registered by pointing the CLI at an existing directory containing a `product.yaml`; the system validates the path and product configuration before appending a `ProductRef` to the profile.

## Requirements

### Requirement: Register product root in active profile
The system SHALL append a new `ProductRef` entry to the specified (or default) profile's product list in `itr.json` when given a valid path to an existing directory containing a `product.yaml`.

#### Scenario: Successful registration
- **WHEN** the user runs `itr product register <path>` where `<path>` is a directory containing a valid `product.yaml`
- **THEN** the product root is appended to the active profile's product list in `itr.json` and the system prints `"Registered product '<id>' from '<path>'."`

#### Scenario: Explicit profile targeting
- **WHEN** the user runs `itr -p <profile> product register <path>`
- **THEN** the product root is appended to the named profile's product list instead of the default profile

#### Scenario: Path stored as supplied
- **WHEN** a product is registered with a relative or absolute path
- **THEN** the path is stored in `itr.json` as supplied by the user; `expandPath` is applied at read time by the adapter

### Requirement: Reject registration when directory does not exist
The system SHALL return a `ProductConfigError` and leave `itr.json` unchanged when the supplied path does not refer to an existing directory.

#### Scenario: Non-existent directory
- **WHEN** the user runs `itr product register <path>` where `<path>` does not exist on disk
- **THEN** the system returns an error and `itr.json` is not modified

### Requirement: Reject registration when product.yaml is missing or invalid
The system SHALL return a `ProductConfigError` and leave `itr.json` unchanged when `<path>/product.yaml` is absent or cannot be parsed.

#### Scenario: Missing product.yaml
- **WHEN** the user runs `itr product register <path>` where `<path>` exists but contains no `product.yaml`
- **THEN** the system returns a `ProductConfigError` and `itr.json` is not modified

### Requirement: Reject duplicate product id within profile
The system SHALL return a `DuplicateProductId` error and leave `itr.json` unchanged when the canonical product id from the supplied path's `product.yaml` already exists in the target profile.

#### Scenario: Duplicate canonical id
- **WHEN** the user runs `itr product register <path>` and the profile already contains a product whose canonical id (from `product.yaml`) matches
- **THEN** the system returns `DuplicateProductId` and `itr.json` is not modified

### Requirement: Reject registration for unknown profile
The system SHALL return a `ProfileNotFound` error when the profile name supplied via `-p` does not exist in the portfolio.

#### Scenario: Profile not found
- **WHEN** the user runs `itr -p nonexistent product register <path>`
- **THEN** the system returns a `ProfileNotFound` error and `itr.json` is not modified

### Requirement: Round-trip lossless itr.json update
The system SHALL preserve all existing profiles and products in `itr.json` unchanged after a successful registration.

#### Scenario: Existing products preserved
- **WHEN** a product is successfully registered
- **THEN** all previously registered products and profiles remain present and unmodified in `itr.json`

### Requirement: Registered product is resolvable
The system SHALL allow a product registered via `itr product register` to be subsequently resolved by product-consuming commands such as `itr backlog take`.

#### Scenario: Resolve registered product
- **WHEN** a product is registered via `itr product register <path>`
- **THEN** subsequent `itr backlog take` invocations can resolve it through the standard product resolution pipeline
