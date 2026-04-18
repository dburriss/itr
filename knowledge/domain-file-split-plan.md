# Domain File Split Plan

Agreed plan for breaking `src/domain/Domain.fs` into per-concept files aligned with `docs/terminology.md`.

---

## Motivation

- `Domain.fs` is a single 343-line flat file covering all domain concepts
- `docs/terminology.md` defines four clear categories: Profile+Portfolio, Product, Backlog, Task
- Splitting by category improves navigability and maintainability
- State machine and validation logic currently inline; splitting creates natural homes for them

---

## File Structure

### `Effect.fs` — unchanged
Pure monadic machinery. No domain type dependencies. Moves to top of compile order.

### `Types.fs` — new, shared primitives
No dependencies on other domain files. Contains types shared across multiple concepts.

- `ProfileName`
- `ProductId`
- `RepoId`
- `RepoPath`
- `CoordinationMode`
- `CoordinationRoot`
- `ProductRoot`
- `RepoConfig` (used by both `ProductDefinition` in Portfolio and `ProductConfig` in Product)

### `Validation.fs` — new, pure utility module
No domain types. General-purpose validation predicates. Domain modules call in and wrap results in their own error types.

```fsharp
module Validation =

    type ValidationResult =
        | Valid of value: string
        | Invalid of value: string * rule: string * description: string

    module Slug =
        let validate (value: string) : ValidationResult =
            // returns Valid or Invalid with rule + human-readable description
```

Call site pattern:
```fsharp
match Validation.Slug.validate value with
| Valid v -> Ok (ProductId v)
| Invalid (_, _, desc) -> Error (InvalidProductId (value, desc))
```

### `Portfolio.fs` — new, Profile + Portfolio concept
- `GitIdentity`
- `AgentConfig`
- `CoordinationConfig`
- `ProductRef`
- `ProductDefinition`
- `Profile`
- `Portfolio`
- `ResolvedProduct`
- `PortfolioError`
- Modules: `ProductId`, `ProfileName`, `Portfolio`

### `Product.fs` — new, product config for backlog/task usecases
- `ProductConfig` (`Id: ProductId`, `Repos: Map<RepoId, RepoConfig>`)

### `Task.fs` — new, Task execution concept
- `TaskId`
- `TaskState`
- `ItrTask`
- `TaskError` (split out from current `BacklogError`):
  - `TaskNotFound`
  - `InvalidTaskState`
  - `MissingPlanArtifact`
  - `TaskIdConflict`
  - `TaskIdOverrideRequiresSingleRepo`
- Modules: `TaskId`, `RepoId`, `ItrTask`

### `Backlog.fs` — new, Backlog planning concept
- `BacklogId`
- `BacklogItemType`
- `BacklogItem`
- `BacklogItemStatus` (references `TaskState` from `Task.fs`)
- `BacklogItemSummary`
- `BacklogItemDetail`
- `BacklogView`
- `BacklogSnapshot`
- `BacklogError` (remaining after task errors extracted):
  - `ProductConfigNotFound`
  - `ProductConfigParseError`
  - `BacklogItemNotFound`
  - `RepoNotInProduct`
  - `DuplicateBacklogId`
  - `InvalidItemType`
  - `MissingTitle`
- Modules: `BacklogId`, `BacklogItemType`, `BacklogItemStatus`, `BacklogItem`

### `Interfaces.fs` — updated
References types from all domain files. Task-related interface methods (`ListTasks`, `WriteTask`, `ArchiveTask`, etc.) currently typed with `BacklogError` — update to `TaskError` after split.

---

## Compile Order in `Itr.Domain.fsproj`

```
Effect.fs
Types.fs
Validation.fs
Portfolio.fs
Product.fs
Task.fs
Backlog.fs
Interfaces.fs
```

`Domain.fs` is deleted once all content is distributed.

---

## Downstream Impact

- `src/features/` usecases that return `BacklogError` for task-related failures need return types updated to `TaskError`
- `Interfaces.fs` task store methods need error type updated from `BacklogError` to `TaskError`
- Changes are mechanical and predictable — no logic changes, only type reorganisation
