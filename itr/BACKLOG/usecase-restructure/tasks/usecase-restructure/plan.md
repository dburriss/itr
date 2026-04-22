# Merge Itr.Features into Itr.Domain with vertical slice structure

**Task ID:** usecase-restructure
**Backlog Item:** usecase-restructure
**Repo:** itr

## Description

Move all feature usecases from Itr.Features into Itr.Domain using a vertical slice structure (Portfolios/, Tasks/, Backlogs/ subfolders). Each operation becomes its own module with a single execute function. Standardise all usecases to effectResult CE style. Delete the Itr.Features project once migration is complete.

## Scope

- **Included:**
  - Move `Itr.Features.Backlog` module functions to `Itr.Domain.Backlogs.Create` and `Itr.Domain.Backlogs.Query` modules with `execute` entry points
  - Move `Itr.Features.Task` module functions to `Itr.Domain.Tasks.Take`, `Itr.Domain.Tasks.Query` modules with `execute` entry points
  - Move `Itr.Features.Portfolio` module functions to `Itr.Domain.Portfolios.Profile`, `Itr.Domain.Portfolios.Product` modules with `execute` entry points
  - Update all callers in `Itr.Cli` to use new fully-qualified module paths
  - Update test files that reference `Itr.Features` modules
  - Delete the `Itr.Features` project from the solution

- **Explicitly excluded:**
  - Changes to adapter implementations in `Itr.Adapters`
  - Changes to other entry points (`Itr.Tui`, `Itr.Mcp`) — out of scope for this item even if they reference `Itr.Features`
  - No data migrations required (pure code restructuring)

## Steps

1. Create vertical slice directory structure in `Itr.Domain`:
   - `src/domain/Backlogs/` containing `Create.fs`, `Query.fs`
   - `src/domain/Tasks/` containing `Take.fs`, `Plan.fs`, `Approve.fs`, `Query.fs`
   - `src/domain/Portfolios/` containing `Profile.fs`, `Product.fs`

2. Migrate `BacklogUsecase.fs`:
   - Create `Backlogs/Create.fs` with `let execute` function using `effectResult { }` CE style
   - Create `Backlogs/Query.fs` with named functions (`loadSnapshot`, `list`, `getDetail`) — no `execute`
   - Input types (`CreateBacklogItemInput`, `BacklogListFilter`) stay co-located in the file that uses them

3. Migrate `TaskUsecase.fs`:
   - Create `Tasks/Take.fs` with `let execute` function using `effectResult { }` CE style
   - Create `Tasks/Query.fs` with named functions (`list`, `filter`, `getDetail`) — no `execute`
   - Create `Tasks/Plan.fs` with `let execute` using `effectResult { }` CE style
   - Create `Tasks/Approve.fs` with `let execute` using `effectResult { }` CE style
   - Input/output types (`TakeInput`, `TaskSummary`, `TaskDetail`, `SiblingTask`) stay co-located in the file that uses them

4. Migrate `PortfolioUsecase.fs`:
   - Create per-operation files (`BootstrapIfMissing.fs`, `SetDefaultProfile.fs`, `AddProfile.fs`, `RegisterProduct.fs`, `InitProduct.fs`) each with `let execute` using `effectResult { }` CE style
   - Create `Portfolios/Query.fs` with named functions (`load`, `resolveActiveProfile`, `loadAllDefinitions`, `resolveProduct`) — no `execute`
   - Input types stay co-located in the file that uses them

5. Update callers in `Itr.Cli`:
   - Replace `open Itr.Features` with references to new domain modules
   - Update all qualified names (e.g., `Backlog.createBacklogItem` -> `Backlogs.Create.execute`)

6. Update callers in test files:
   - Update test imports to reference new module paths

7. Update `Itr.Domain.fsproj`:
   - Add new subdirectories to the project file

8. Remove `Itr.Features`:
   - Remove the project from `Itr.sln`
   - Delete `src/features/` directory

9. Verify:
   - Run `dotnet build` to ensure no errors
   - Run `dotnet test` to ensure all tests pass

## Dependencies

- none

## Acceptance Criteria

- All usecases from Itr.Features are moved into Itr.Domain under Portfolios/, Tasks/, and Backlogs/ subdirectories matching the target structure in the architecture review
- Each operation is a separate module with a single `let execute` function using `effectResult { }` CE style
- Query modules (Query.fs) per concept expose named functions; no execute entry point
- Usecases do not call other usecases (queries are allowed)
- Private helpers are co-located in the file that uses them — no standalone helper modules
- Callers reference usecases with full qualification (e.g. Tasks.Take.execute, Backlogs.Query.list)
- Itr.Features project is deleted and removed from the solution
- Build passes with no errors or warnings
- All existing tests pass

## Impact

- **Files changed:**
  - `src/domain/Itr.Domain.fsproj` - adds new subdirectories
  - New files: `Backlogs/Create.fs`, `Backlogs/Query.fs`, `Tasks/Take.fs`, `Tasks/Query.fs`, `Portfolios/Profile.fs`, `Portfolios/Product.fs`
  - `src/cli/Program.fs` - updates module references
  - `src/cli/InteractivePrompts.fs` - updates module references
  - Test files updated to new paths

- **Files deleted:**
  - `src/features/Itr.Features.fsproj`
  - `src/features/Backlog/BacklogUsecase.fs`
  - `src/features/Task/TaskUsecase.fs`
  - `src/features/Portfolio/PortfolioUsecase.fs`

- **Interface changes:**
  - Caller code changes from `Itr.Features.Backlog.createBacklogItem` to `Backlogs.Create.execute`
  - Types move from `Itr.Features.Backlog.CreateBacklogItemInput` to `Backlogs.CreateInput`

- **No data migrations required** - this is a pure code restructure

## Risks

- **Breaking API changes**: Existing internal callers in tests or other entry points need updates. Mitigate: Search for all references before deletion.

- **Module naming conflicts**: New module names may conflict with existing domain types. Mitigate: Use distinct names (e.g., `Backlogs` vs `Backlog` module).

- **Build failures during transition**: Temporary state where code won't compile. Mitigate: Complete migration in small atomic steps, committing after each functional unit works.

- **Missing types**: Some helper types may need to remain accessible. Mitigate: Re-export necessary types from new domain modules or keep them in domain core types file.

## Decisions

- **Input types** are co-located with the usecase module that uses them — not centralised in domain type files
- **Query modules** use named functions (`list`, `filter`, `getDetail`, etc.) with no `execute` entry point
- **Itr.Tui and Itr.Mcp** are out of scope — only `Itr.Cli` callers are updated in this item
- **Input record normalisation**: Usecases with two or more loose data parameters are normalised to a single `Input` record. Usecases with one data parameter are left as-is. Dependency parameters (stores, configs, effect deps) are never folded into `Input`.
  - `Tasks.Approve.execute` gets `Input = { Task: ItrTask; PlanExists: bool }` (was two loose params)
  - `Tasks.Query.getDetail` gets `Input = { TaskId: TaskId; AllTasks: ItrTask list; TaskYamlPath: string }` (was three loose params)
  - `Tasks.Query.filter` gets `Input = { BacklogId: BacklogId option; Repo: RepoId option; State: TaskState option; Exclude: TaskState list }` (was four loose params); the `summaries` list stays as a separate parameter
  - `Tasks.Take.Input` renamed from `TakeInput` for consistency
  - `Tasks.Plan.execute (task: ItrTask)` — single param, no record needed

## Open Questions

- None remaining.