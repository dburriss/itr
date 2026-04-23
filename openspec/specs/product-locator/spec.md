# Spec: Product Locator

## Purpose

Provides a module responsible for locating the product root directory by walking up the directory tree, with support for testable filesystem abstractions.

## Requirements

### Requirement: Locate product root by walking up directory tree
The system SHALL provide a `ProductLocator` module with a `locateProductRoot` function that searches for `product.yaml` by walking up from a given start path.

#### Scenario: Found in start directory
- **WHEN** `product.yaml` exists in the start directory
- **THEN** `locateProductRoot` returns `Some` with that directory path

#### Scenario: Found in ancestor directory
- **WHEN** `product.yaml` does not exist in the start directory but exists in a parent directory
- **THEN** `locateProductRoot` returns `Some` with the ancestor directory path

#### Scenario: Not found
- **WHEN** no `product.yaml` exists in the start directory or any ancestor up to the filesystem root
- **THEN** `locateProductRoot` returns `None`

### Requirement: ProductLocator uses an abstracted filesystem
The `ProductLocator` module SHALL accept a filesystem abstraction (compatible with `Testably.Abstractions`) so that it can be tested without touching the real filesystem.

#### Scenario: Testable with in-memory filesystem
- **WHEN** a test provides an in-memory `IFileSystem` to `ProductLocator`
- **THEN** the traversal logic can be exercised without real disk access
