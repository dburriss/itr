## Context

The domain layer currently has two flat files:
- `Domain.fs` (346 lines): all domain types and validation modules for Profile, Portfolio, Product, Backlog, Task, and BacklogItem
- `Interfaces.fs` (100 lines): all capability interfaces in one place

This makes it difficult to navigate concepts, understand boundaries, and extend individual domains. The refactor is a pure structural reorganisation — no logic changes — aligned with the terminology categories: Types, Portfolio, Product, Task, Backlog.

## Goals / Non-Goals

**Goals:**
- Split `Domain.fs` into per-concept files: `Types.fs` (shared primitives + infrastructure interfaces), `Product.fs`, `Task.fs`, `Backlog.fs`
- Co-locate each interface with its domain concept
- Extract `TaskError` from `BacklogError` into `Task.fs`; update `ITaskStore` to use `TaskError`
- Create `Validation.fs` as a generic shared validation utility (slug regex helper)
- Update `Itr.Domain.fsproj` compile order
- Delete `Domain.fs` and `Interfaces.fs`

**Non-Goals:**
- No logic changes to usecases, adapters, or tests (beyond namespace fixes)
- No new capabilities or behavior

## Decisions

### File layout and compile order

Compile order in `Itr.Domain.fsproj`:
1. `Effect.fs` — already exists, unchanged
2. `Types.fs` — shared primitive types (`ProfileName`, `RepoPath`, `ProductRoot`, `GitIdentity`, `CoordinationMode`, `CoordinationRoot`, `RepoId`) + infrastructure interfaces (`IoError`, `IFileSystem`, `IEnvironment`, `IYamlService`, `IGitService`, `IAgentHarness`)
3. `Validation.fs` — shared slug regex helper only (no domain-specific logic)
4. `Portfolio.fs` — `Profile`, `Portfolio`, `PortfolioError`, `ProfileName` validation, `Portfolio` module, `IPortfolioConfig`
5. `Product.fs` — `ProductId`, `ProductDefinition`, `RepoConfig`, `CoordRoot`, `AgentConfig`, `CoordinationConfig`, `ProductConfig`, `ProductId` module, `RepoId` module, `IProductConfig`
6. `Task.fs` — `TaskId`, `TaskState`, `ItrTask`, `TaskError` (extracted from `BacklogError`), `TaskId` module, `ItrTask` module, `ITaskStore`
7. `Backlog.fs` — `BacklogId`, `BacklogItemType`, `BacklogItem`, `BacklogItemStatus`, `BacklogItemSummary`, `BacklogItemDetail`, `BacklogView`, `BacklogSnapshot`, `BacklogError` (task-related cases removed), `BacklogItemStatus` module, `BacklogItemType` module, `BacklogId` module, `BacklogItem` module, `IBacklogStore`, `IViewStore`

**Rationale:** This order respects F# top-down compile dependencies. `Types.fs` has no domain deps; `Portfolio.fs` depends only on `Types.fs`; `Product.fs` depends on `Types.fs`/`Portfolio.fs`; `Task.fs` depends on `Backlog` concepts indirectly via `BacklogId`, so must come before `Backlog.fs`; `Backlog.fs` depends on `Task.fs` for `ItrTask`.

### TaskError extraction

`TaskError` cases extracted from `BacklogError`:
- `TaskNotFound of TaskId`
- `InvalidTaskState of taskId: TaskId * current: TaskState`
- `MissingPlanArtifact of TaskId`
- `TaskIdConflict of TaskId`
- `TaskIdOverrideRequiresSingleRepo`

`BacklogError` retains only backlog-item-level errors. `ITaskStore` methods return `Result<_, TaskError>`. Feature usecases that currently return `BacklogError` for task operations are updated to return `TaskError`.

**Rationale:** Separating error domains makes type signatures more precise and prevents mixing concerns at call sites.

### Validation.fs scope

Contains only: a shared `slugRegex` pattern and a generic `isValidSlug` helper. Domain-specific validation modules (`ProductId`, `BacklogId`, `TaskId`, `ProfileName`, `BacklogItemType`) remain in their respective files, using the shared helper if needed.

## Risks / Trade-offs

- **Risk:** `ITaskStore` interface change breaks `YamlAdapter.fs` implementation → Mitigation: update `YamlAdapter` task methods to map errors to `TaskError`
- **Risk:** Usecases mixing `BacklogError` and `TaskError` at call sites → Mitigation: update affected usecases to use the correct error type; use `Result.mapError` where needed
- **Risk:** F# compile order circular deps if `Backlog.fs` references `Task.fs` types → Mitigation: `Task.fs` must precede `Backlog.fs`; verify `ItrTask` is only referenced forward
- **Risk:** Tests that `open Itr.Domain` may pick up type name conflicts → Mitigation: run tests immediately after split; fix any ambiguous opens
