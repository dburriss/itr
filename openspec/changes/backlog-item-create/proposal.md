## Why

The backlog currently supports viewing and taking items but not creating them via the CLI. Users must manually author `item.yaml` files, which is error-prone and bypasses validation. Adding `itr backlog add` closes this gap by providing validated, consistent backlog item creation.

## What Changes

- New CLI command `itr backlog add <id>` that creates `<coordRoot>/BACKLOG/<id>/item.yaml`
- Validation of `id`, `title`, `repos`, `type`, and `dependencies` at creation time
- Duplicate id detection (rejects if item already exists)
- Repo resolution: defaults to sole repo for single-repo products; errors if omitted with multiple repos
- `--type` defaults to `feature`; validates against `feature | bug | chore | spike`
- **BREAKING**: Rename `TakeError` → `BacklogError` with extended error cases (`DuplicateBacklogId`, `InvalidItemType`, `MissingTitle`)
- Extend `BacklogItem` domain record with new fields (`Type`, `Priority`, `Summary`, `AcceptanceCriteria`, `Dependencies`, `CreatedAt`)
- New `WriteBacklogItem` and `BacklogItemExists` members on `IBacklogStore`

## Capabilities

### New Capabilities

- `backlog-item-create`: CLI command to create a new backlog item with validation, repo resolution, and YAML file output

### Modified Capabilities

- `backlog-take`: `TakeError` renamed to `BacklogError`; `BacklogItem` record gains new optional fields — existing take workflow behavior unchanged but error type changes

## Impact

- `src/domain/Domain.fs`: `BacklogItem` record expanded; `TakeError` → `BacklogError`
- `src/domain/Interfaces.fs`: new `IBacklogStore` members; error type references updated
- `src/adapters/YamlAdapter.fs`: `BacklogItemDto` extended; new adapter methods
- `src/features/TaskUsecase.fs`: `TakeError` → `BacklogError` reference update
- `src/features/Backlog/BacklogUsecase.fs`: new file — pure `createBacklogItem` function
- `src/cli/Program.fs`: new `AddArgs`, dispatch branch, handler
- `tests/acceptance/BacklogAcceptanceTests.fs`: new acceptance test file
