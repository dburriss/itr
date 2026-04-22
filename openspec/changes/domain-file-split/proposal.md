## Why

`Domain.fs` (343 lines) and `Interfaces.fs` define all domain types and capability interfaces in two flat files, making it hard to navigate, reason about concept boundaries, and find natural homes for future validation and state machine logic. Splitting them into per-concept files improves maintainability and creates clear separation of concerns.

## What Changes

- `Domain.fs` is deleted and its content split into: `Types.fs`, `Product.fs`, `Task.fs`, `Backlog.fs`
- `Interfaces.fs` is deleted and its interfaces co-located with their domain concept files
- `Validation.fs` is created as a generic shared validation utility module
- `TaskError` is extracted from `BacklogError` into `Task.fs`
- `ITaskStore` methods updated to use `TaskError` instead of `BacklogError`
- `Itr.Domain.fsproj` compile order updated to reflect new file dependency graph
- Feature usecases returning `BacklogError` for task-related failures updated to `TaskError`

## Capabilities

### New Capabilities

- None — this is a pure structural reorganisation with no new user-facing capabilities

### Modified Capabilities

- None — no spec-level requirement changes; only internal code structure changes

## Impact

- `src/domain/Domain.fs` — deleted
- `src/domain/Interfaces.fs` — deleted
- `src/domain/Itr.Domain.fsproj` — updated compile order
- New files: `src/domain/Validation.fs`, `src/domain/Product.fs`, `src/domain/Task.fs`, `src/domain/Backlog.fs`
- `src/adapters/YamlAdapter.fs` — updated to use `TaskError` for task operations
- Test files may need namespace/open updates if they reference `Itr.Domain` directly
