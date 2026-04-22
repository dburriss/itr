# Architecture Review: Codebase vs ARCHITECTURE.md

Date reviewed: 2026-04-13

---

## Layer Naming Divergence

| ARCHITECTURE.md | Actual Code |
|---|---|
| `Itr.Domain` | `Itr.Domain` (`src/domain/`) ✓ |
| `Itr.Features` | `Itr.Features` (`src/features/`) — **planned for deletion** |
| `Itr.Adapters` | `Itr.Adapters` (`src/adapters/`) ✓ |
| `Itr.Cli`, `Itr.Tui`, `Itr.Mcp`, `Itr.Server` | All present ✓ |

The spec now correctly names the core layer `Itr.Domain`, matching the implementation.

**Decided (2026-04-22):** `Itr.Features` will be merged into `Itr.Domain`. Feature usecases are business logic and belong in domain. The separate project layer adds no meaningful seam. See Usecase Structure section below.

---

## Domain File Structure Divergence

| ARCHITECTURE.md | Actual Code |
|---|---|
| `Domain/Product.fs`, `Backlog.fs`, `Task.fs`, `Feature.fs`, `StateMachine.fs`, `Validation.fs` | `Types.fs`, `Product.fs`, `Task.fs`, `Backlog.fs`, `Portfolio.fs`, `Validation.fs` ✓ |
| `Effect.fs` | `Effect.fs` ✓ |
| `Interfaces.fs` | Deleted — interfaces co-located with domain files ✓ |

**Resolved (2026-04-21):** `Domain.fs` and `Interfaces.fs` have been split into per-concept files. Domain types are now distributed across `Types.fs`, `Portfolio.fs`, `Product.fs`, `Task.fs`, `Backlog.fs`, and `Validation.fs`. Interfaces are co-located with their domain files. `TaskError` was extracted from `BacklogError` and lives in `Task.fs`. Remaining gap: no dedicated `StateMachine.fs` — state machine logic still lives inline in usecases.

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

## Usecase Style: Decided

The spec specifies a uniform `effectResult { }` CE style. Three distinct patterns currently exist (raw `Effect` lambda, plain interface args, fully pure functions). These inconsistencies will be resolved as part of the usecase restructure.

**Decided (2026-04-22):** Feature usecases move into `Itr.Domain` using a vertical slice structure:

- Each operation becomes its own module with a single `let execute` function
- Plural concept namespaces (`Portfolios`, `Tasks`, `Backlogs`) avoid clashes with domain types
- Query modules per concept (`Query.fs`) expose named functions (`list`, `filter`, `getDetail` etc.) — no `execute`
- Usecases may call queries; usecases may not call other usecases
- Private helpers stay in the file that uses them (no separate helper modules)

**Target structure:**

```
src/domain/
  ... existing type files ...
  Portfolios/
    Query.fs          (load, resolveActiveProfile, loadAllDefinitions, resolveProduct)
    BootstrapIfMissing.fs
    SetDefaultProfile.fs
    AddProfile.fs
    RegisterProduct.fs
    InitProduct.fs
  Tasks/
    Query.fs          (list, filter, getDetail)
    Take.fs
    Plan.fs
    Approve.fs
  Backlogs/
    Query.fs          (loadSnapshot, list, getDetail)
    Create.fs
```

**Call site convention:** callers always qualify fully, e.g. `Tasks.Take.execute`, `Backlogs.Query.list`.

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

See: https://devonburriss.me/fp-architecture/

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
- `TaskSummary`, `TaskDetail`, `SiblingTask` (currently in Features layer — will move to `Itr.Domain` with usecase restructure)

State machine transitions are enforced in-place (e.g. `planTask`, `approveTask` in `TaskUsecase.fs`) rather than in a dedicated `StateMachine.fs`.

---

## Summary of Key Gaps vs Spec

| Area | Gap | Severity |
|---|---|---|
| Project name | `Itr.Domain` — matches codebase ✓ | Resolved |
| Domain file structure | Single `Domain.fs` vs per-concept files | Resolved (2026-04-21) |
| State machine isolation | Inline in usecase functions vs dedicated `StateMachine.fs` | Medium |
| Usecase style | Three different patterns; spec's CE style not used consistently | Superseded — see Usecase Structure decision |
| `Itr.Features` project | Separate project for feature usecases | Decided: merge into `Itr.Domain` (2026-04-22) |
| Entry point purity | Business/formatting logic in `Program.fs` | Medium |
| `IGitService` | Defined, not used | Low |
| `IYamlService` | Defined but unused; YAML logic bypasses it | Low |
