# Architecture Review: Codebase vs ARCHITECTURE.md

Date reviewed: 2026-04-13

---

## Layer Naming Divergence

| ARCHITECTURE.md | Actual Code |
|---|---|
| `Itr.Domain` | `Itr.Domain` (`src/domain/`) ✓ |
| `Itr.Features` | `Itr.Features` (`src/features/`) ✓ |
| `Itr.Adapters` | `Itr.Adapters` (`src/adapters/`) ✓ |
| `Itr.Cli`, `Itr.Tui`, `Itr.Mcp`, `Itr.Server` | All present ✓ |

The spec now correctly names the core layer `Itr.Domain`, matching the implementation.

---

## Domain File Structure Divergence

| ARCHITECTURE.md | Actual Code |
|---|---|
| `Domain/Product.fs`, `Backlog.fs`, `Task.fs`, `Feature.fs`, `StateMachine.fs`, `Validation.fs` | Single flat `Domain.fs` |
| `Effect.fs` | `Effect.fs` ✓ |
| `Interfaces.fs` | `Interfaces.fs` ✓ |

All domain types (`Portfolio`, `Profile`, `BacklogItem`, `ItrTask`, `TaskState`, etc.) are consolidated in one `Domain.fs`. The spec envisions per-concept files with a dedicated `StateMachine.fs` and `Validation.fs`. State machine logic (valid transitions) and validation rules (slug regex, ID validation) currently live inline inside `Domain.fs` modules.

---

## Effect Pattern

The `Effect<'deps,'a>` and `EffectResult` types match the spec definition exactly.

The actual implementation adds useful extras beyond the spec:
- `EffectResult.mapError`
- `EffectResult.ask`
- `EffectResult.liftEffect`
- `EffectResult.require`
- Builder members: `Combine`, `Delay`, `Run`, `Source`

These are sensible additions.

---

## Usecase Style: Inconsistent

The spec specifies a uniform `effectResult { }` CE style with `EffectResult.asks` threading deps. Three distinct patterns exist in practice:

### 1. Raw Effect lambda (Portfolio)
```fsharp
// PortfolioUsecase.fs
let loadPortfolio configPath : EffectResult<'deps, Portfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig
        ...)
```
Uses raw `Effect(fun deps -> ...)` lambda. Deps extracted via `:> Interface` upcasts inside the lambda body. No CE used.

### 2. Pure functions with explicit interface args (Backlog)
```fsharp
// BacklogUsecase.fs
let loadSnapshot
    (backlogStore: IBacklogStore)
    (taskStore: ITaskStore)
    (viewStore: IViewStore)
    (coordRoot: string)
    : Result<BacklogSnapshot, BacklogError> =
```
No `Effect` wrapper. Interfaces passed as plain function arguments. Caller (entry point) passes adapters directly.

### 3. Fully pure functions, no DI at all (Task)
```fsharp
// TaskUsecase.fs
let takeBacklogItem
    (productConfig: ProductConfig)
    (backlogItem: BacklogItem)
    (existingTasks: ItrTask list)
    (input: TakeInput)
    (today: DateOnly)
    : Result<ItrTask list, BacklogError> =
```
Pure data-in / result-out. No interfaces, no IO, no `Effect`.

The spec's intended uniform `effectResult { }` CE style is not consistently used.

---

## Composition Root

`AppDeps` in `src/cli/Program.fs:336` correctly implements all interfaces in a single type — matches the spec pattern. The implementation is correct but verbose: each interface member explicitly delegates to a sub-adapter, which is mechanical but clear.

---

## Entry Point Purity

The spec states: *"No entry point contains business logic."*

`Program.fs` currently contains significant non-wiring logic:
- JSON serialization built inline in each handler (not in adapters or a formatter layer)
- String parsing helpers: `tryParseTaskState`, `tryParseBacklogItemStatus` (~30 lines)
- AI harness selection logic in `handleTaskPlan` (protocol dispatch: `acp` vs `opencode-http`)
- Filesystem traversal in `handleProductInfo` (walking up dirs to find `product.yaml`)
- Output formatting (table/JSON/text) entirely inline in every handler

---

## Interfaces: Richer Than Spec

| Spec | Actual |
|---|---|
| `IFileSystem` | ✓ |
| `IGitService` | Defined but not used in any usecase |
| — | `IEnvironment` |
| — | `IPortfolioConfig` |
| — | `IYamlService` (defined, not used — YAML logic is in adapter directly) |
| — | `IProductConfig` |
| — | `IBacklogStore` |
| — | `IViewStore` |
| — | `ITaskStore` |
| — | `IAgentHarness` |

`IYamlService` is defined but never wired; YAML operations live directly in `YamlAdapter.fs` concrete types.

---

## Domain Model: Evolved Beyond Spec

The spec names `Product`, `Backlog`, `Task`, `Feature` as the domain concepts. The actual domain is more developed:

- `Portfolio`, `Profile`, `ProductRef`, `ProductDefinition`
- `CoordinationRoot`, `CoordinationMode`
- `BacklogItem`, `BacklogItemType`, `BacklogItemStatus`
- `ItrTask`, `TaskState` (Planning → Planned → Approved → InProgress → Implemented → Validated → Archived)
- `BacklogView`, `BacklogSnapshot`, `BacklogItemDetail`, `BacklogItemSummary`
- `TaskSummary`, `TaskDetail`, `SiblingTask` (in Features layer)

State machine transitions are enforced in-place (e.g. `planTask`, `approveTask` in `TaskUsecase.fs`) rather than in a dedicated `StateMachine.fs`.

---

## Summary of Key Gaps vs Spec

| Area | Gap | Severity |
|---|---|---|
| Project name | `Itr.Domain` — matches codebase ✓ | Resolved |
| Domain file structure | Single `Domain.fs` vs per-concept files | Medium — maintainability |
| State machine isolation | Inline in usecase functions vs dedicated `StateMachine.fs` | Medium |
| Usecase style | Three different patterns; spec's CE style not used consistently | Medium |
| Entry point purity | Business/formatting logic in `Program.fs` | Medium |
| `IGitService` | Defined, not used | Low |
| `IYamlService` | Defined but unused; YAML logic bypasses it | Low |
