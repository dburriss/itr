# Domain File Split Plan

Agreed plan for breaking `src/domain/Domain.fs` and `src/domain/Interfaces.fs` into per-concept files aligned with `docs/terminology.md`.

---

## Motivation

- `Domain.fs` is a single 343-line flat file covering all domain concepts
- `Interfaces.fs` defines capability interfaces for all domain concerns in one place
- `docs/terminology.md` defines four clear categories: Profile+Portfolio, Product, Backlog, Task
- Splitting by category improves navigability and maintainability
- State machine and validation logic currently inline; splitting creates natural homes for them
- Co-locating interfaces with their domain files improves cohesion

---

## File Structure

### `Effect.fs` — unchanged
Pure monadic machinery. No domain type dependencies. Moves to top of compile order.

### `Types.fs` — new, shared primitives + infrastructure interfaces
No dependencies on other domain files. Contains types shared across multiple concepts, plus capability interfaces with no domain type dependencies.

Types:
- `ProfileName`
- `ProductId`
- `RepoId`
- `RepoPath`
- `CoordinationMode`
- `CoordinationRoot`
- `ProductRoot`
- `RepoConfig` (used by both `ProductDefinition` in Portfolio and `ProductConfig` in Product)

Infrastructure interfaces (from `Interfaces.fs`):
- `IoError`
- `IFileSystem`
- `IEnvironment`
- `IYamlService`
- `IGitService`
- `IAgentHarness`

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
Types:
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

Interfaces (from `Interfaces.fs`):
- `IPortfolioConfig`
- `IProductConfig`

### `Product.fs` — new, product config for backlog/task usecases
- `ProductConfig` (`Id: ProductId`, `Repos: Map<RepoId, RepoConfig>`)

### `Task.fs` — new, Task execution concept
Types:
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

Interfaces (from `Interfaces.fs`):
- `ITaskStore` (updated: error type changed from `BacklogError` to `TaskError`)

### `Backlog.fs` — new, Backlog planning concept
Types:
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

Interfaces (from `Interfaces.fs`):
- `IBacklogStore`
- `IViewStore`

### `Interfaces.fs` — deleted
Content distributed across domain files. No replacement file.

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
```

Both `Domain.fs` and `Interfaces.fs` are deleted once all content is distributed.

---

## Downstream Impact

- `src/features/` usecases that return `BacklogError` for task-related failures need return types updated to `TaskError`
- `ITaskStore` methods need error type updated from `BacklogError` to `TaskError`
- `src/adapters/` implement interfaces now defined in their respective domain files — all still in `Itr.Domain` namespace, no open/reference changes needed
- Changes are mechanical and predictable — no logic changes, only type reorganisation
