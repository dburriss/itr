## Context

The features layer currently contains three monolithic usecase files:
- `src/features/Portfolio/PortfolioUsecase.fs` (422 lines)
- `src/features/Task/TaskUsecase.fs` (302 lines)
- `src/features/Backlog/BacklogUsecase.fs` (358 lines)

The architecture review (2026-04-22) decided `Itr.Features` adds no meaningful seam — feature usecases are business logic that belongs in `Itr.Domain`. The target is a vertical-slice structure where each operation has its own file, grouped under plural concept sub-folders, all within the existing `Itr.Domain` project.

The CLI (`src/cli/Program.fs`) currently uses `open Itr.Features` and accesses usecases as `Portfolio.X`, `Task.X`, `Backlog.X`. Tests in `tests/acceptance/` also reference `Itr.Features`. Both need updating.

## Goals / Non-Goals

**Goals:**
- Move all usecase logic from `Itr.Features` into vertical-slice modules in `Itr.Domain`
- Plural namespace sub-folders (`Portfolios/`, `Tasks/`, `Backlogs/`) to avoid clashing with existing domain type modules
- Each command operation: one file, one `let execute` function
- Query functions: one `Query.fs` per concept with named functions
- Delete `Itr.Features` project entirely
- Update CLI and test call sites to new qualified names
- No behaviour changes

**Non-Goals:**
- Changing any business logic or function signatures
- Renaming domain types or error types
- Adding new capabilities

## Decisions

### Decision: Plural sub-folder names (`Portfolios/`, `Tasks/`, `Backlogs/`)

`Portfolio`, `Task`, and `Backlog` are already taken as top-level module names in `Itr.Domain` (e.g. `module Itr.Domain.Portfolio`). Using plural names for the usecase sub-folders avoids the naming collision. Call sites become `Portfolios.Query.load`, `Tasks.Take.execute`, `Backlogs.Create.execute`, etc.

### Decision: Query module per concept, command module per operation

Query functions (pure reads, resolvers, loaders) are grouped into a single `Query.fs` per concept. Each command (state-transition or creation) gets its own file named after the operation (`Take.fs`, `Plan.fs`, etc.) with a single public `let execute` function. Private helpers stay in the same file.

### Decision: `IPortfolioDeps` and input types stay with their commands

`IPortfolioDeps`, `SetDefaultProfileInput`, `AddProfileInput`, `RegisterProductInput`, and `InitProductInput` currently live in `PortfolioUsecase.fs`. They move alongside their related command files in `Portfolios/`. `TaskSummary`, `SiblingTask`, and `TaskDetail` move into `Portfolios/Query.fs` or `Tasks/Query.fs` as they are query return types.

### Decision: Compile order — Query before Commands within each concept

Within each concept folder, `Query.fs` compiles first because command files may call query helpers (e.g. `RegisterProduct` calls `loadAllDefinitions`). Concept folders compile in order: Portfolios → Tasks → Backlogs (Backlogs depends on Task types).

### Decision: Normalise multi-param inputs into a single `Input` record

Usecases that currently take two or more loose data parameters are normalised to receive a single `Input` record. Usecases with only one data parameter are left as-is (a record would add ceremony with no benefit). Dependency parameters (stores, configs, effect deps) are never included in the `Input` record.

Affected usecases:
- `approveTask (task: ItrTask) (planExists: bool)` → `execute (input: Input)` where `Input = { Task: ItrTask; PlanExists: bool }`
- `getTaskDetail (taskId: TaskId) (allTasks: ItrTask list) (taskYamlPath: string)` → `execute (input: Input)` where `Input = { TaskId: TaskId; AllTasks: ItrTask list; TaskYamlPath: string }`
- `takeBacklogItem (productConfig) (backlogItem) (existingTasks) (input: TakeInput) (today: DateOnly)` — already has `TakeInput`; rename to `Input` for consistency
- `listTasks (tasks: (ItrTask * string) list)` — single param, leave as-is
- `filterTasks (backlogId) (repo) (state) (exclude) (summaries)` — five loose params; normalise to `Input = { BacklogId: BacklogId option; Repo: RepoId option; State: TaskState option; Exclude: TaskState list }`; `summaries` stays as a separate param (it is the data being filtered, not configuration)
- `loadSnapshot (backlogStore) (taskStore) (viewStore) (coordRoot)` — all deps/config, not data inputs; leave as-is
- `listBacklogItems (filter: BacklogListFilter) (snapshot: BacklogSnapshot)` — two params but both are already typed records; leave as-is
- `getBacklogItemDetail (backlogStore) (taskStore) (viewStore) (coordRoot) (backlogId)` — deps + single data param `backlogId`; leave as-is

Call sites in `Program.fs` and tests are updated accordingly.

### Decision: Remove `Itr.Features` project and project references

After moving all logic, the `src/features/` directory and `Itr.Features.fsproj` are deleted. The CLI project and test projects remove their `Itr.Features` project reference and add `Itr.Domain` if not already referenced.

## File Mapping

| Old location | New location |
|---|---|
| `PortfolioUsecase.fs` — `IPortfolioDeps`, input types | `Portfolios/Query.fs` (IPortfolioDeps) + each command file (input type) |
| `PortfolioUsecase.fs` — `loadPortfolio`, `resolveActiveProfile`, `loadAllDefinitions`, `resolveProduct` | `Portfolios/Query.fs` |
| `PortfolioUsecase.fs` — `bootstrapIfMissing` | `Portfolios/BootstrapIfMissing.fs` |
| `PortfolioUsecase.fs` — `setDefaultProfile` | `Portfolios/SetDefaultProfile.fs` |
| `PortfolioUsecase.fs` — `addProfile` | `Portfolios/AddProfile.fs` |
| `PortfolioUsecase.fs` — `registerProduct` | `Portfolios/RegisterProduct.fs` |
| `PortfolioUsecase.fs` — `initProduct` | `Portfolios/InitProduct.fs` |
| `TaskUsecase.fs` — `TaskSummary`, `SiblingTask`, `TaskDetail` | `Tasks/Query.fs` |
| `TaskUsecase.fs` — `listTasks`, `filterTasks`, `getTaskDetail` | `Tasks/Query.fs` |
| `TaskUsecase.fs` — `takeBacklogItem` (+ `TakeInput`, `deriveTaskId`) | `Tasks/Take.fs` |
| `TaskUsecase.fs` — `planTask` | `Tasks/Plan.fs` |
| `TaskUsecase.fs` — `approveTask` | `Tasks/Approve.fs` |
| `BacklogUsecase.fs` — `CreateBacklogItemInput`, `BacklogListFilter` | `Backlogs/Query.fs` (filter type) + `Backlogs/Create.fs` (input type) |
| `BacklogUsecase.fs` — `loadSnapshot`, `listBacklogItems`, `getBacklogItemDetail` | `Backlogs/Query.fs` |
| `BacklogUsecase.fs` — `createBacklogItem` (+ `taskErrorToBacklogError`) | `Backlogs/Create.fs` |

## Call Site Changes

| Old | New |
|---|---|
| `Portfolio.loadPortfolio` | `Portfolios.Query.load` |
| `Portfolio.resolveActiveProfile` | `Portfolios.Query.resolveActiveProfile` |
| `Portfolio.loadAllDefinitions` | `Portfolios.Query.loadAllDefinitions` |
| `Portfolio.resolveProduct` | `Portfolios.Query.resolveProduct` |
| `Portfolio.bootstrapIfMissing` | `Portfolios.BootstrapIfMissing.execute` |
| `Portfolio.setDefaultProfile` | `Portfolios.SetDefaultProfile.execute` |
| `Portfolio.addProfile` | `Portfolios.AddProfile.execute` |
| `Portfolio.registerProduct` | `Portfolios.RegisterProduct.execute` |
| `Portfolio.initProduct` | `Portfolios.InitProduct.execute` |
| `Task.listTasks` | `Tasks.Query.list` |
| `Task.filterTasks` | `Tasks.Query.filter` |
| `Task.getTaskDetail` | `Tasks.Query.getDetail` |
| `Task.takeBacklogItem` | `Tasks.Take.execute` |
| `Task.planTask` | `Tasks.Plan.execute` |
| `Task.approveTask` | `Tasks.Approve.execute` |
| `Backlog.loadSnapshot` | `Backlogs.Query.loadSnapshot` |
| `Backlog.listBacklogItems` | `Backlogs.Query.list` |
| `Backlog.getBacklogItemDetail` | `Backlogs.Query.getDetail` |
| `Backlog.createBacklogItem` | `Backlogs.Create.execute` |
| `Backlog.BacklogListFilter` type | `Backlogs.Query.BacklogListFilter` |
| `Task.TaskSummary` type | `Tasks.Query.TaskSummary` |
| `Task.SiblingTask` type | `Tasks.Query.SiblingTask` |
| `Task.TaskDetail` type | `Tasks.Query.TaskDetail` |
| `Portfolio.RegisterProductInput` | `Portfolios.RegisterProduct.Input` |
| `Portfolio.SetDefaultProfileInput` | `Portfolios.SetDefaultProfile.Input` |
| `Portfolio.AddProfileInput` | `Portfolios.AddProfile.Input` |
| `Portfolio.InitProductInput` | `Portfolios.InitProduct.Input` |
| `Backlog.CreateBacklogItemInput` | `Backlogs.Create.Input` |

## Risks / Trade-offs

- [Risk] Large CLI refactor (~78+ references in Program.fs) — Mitigation: systematic find/replace per call site group; build after each section
- [Risk] Test project references need updating — Mitigation: update fsproj and open statements alongside CLI
- [Risk] Partial moves leave broken builds — Mitigation: migrate one concept at a time (Portfolios → Tasks → Backlogs); build must pass before moving to next

## Open Questions

*(none)*
