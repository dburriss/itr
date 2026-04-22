## Why

The three usecase files (`BacklogUsecase.fs`, `TaskUsecase.fs`, `PortfolioUsecase.fs`) in `Itr.Features` mix pure functions, I/O orchestration, and auxiliary types in large monolithic files. Additionally, the `Itr.Features` project is an unnecessary seam: feature usecases are business logic and belong in the domain layer alongside the types they operate on. The architecture review (2026-04-22) decided to merge `Itr.Features` into `Itr.Domain` using a vertical-slice structure where each operation is its own focused module.

## What Changes

- `Itr.Features` project and its three usecase files are deleted
- All usecase logic moves into `src/domain/` under plural concept sub-folders: `Portfolios/`, `Tasks/`, `Backlogs/`
- Each operation becomes its own file with a single `let execute` function; query modules expose named functions (`list`, `filter`, `getDetail`, etc.)
- `Itr.Domain.fsproj` compile order is updated to include all new files
- `src/cli/Program.fs` call sites are updated to the new qualified names (e.g. `Tasks.Take.execute`, `Backlogs.Query.list`)
- `Itr.Features` project reference is removed from the CLI project

## Capabilities

### New Capabilities

*(none — this is a structural refactor, no new user-facing capabilities)*

### Modified Capabilities

- `domain-structure`: The domain layer now contains vertical-slice usecase modules in addition to domain types. Each concept sub-folder (`Portfolios/`, `Tasks/`, `Backlogs/`) SHALL contain a `Query.fs` and one file per command operation. The `Itr.Features` project SHALL NOT exist after the restructure.

## Impact

- `src/domain/` — new sub-folders and files added
- `src/domain/Itr.Domain.fsproj` — compile order updated
- `src/features/` — all usecase files deleted; project deleted
- `src/cli/Program.fs` — `open Itr.Features` removed; call sites updated to new qualified names
- `src/cli/Itr.Cli.fsproj` — `Itr.Features` project reference removed
- Tests — call sites updated to match new qualified names; no behaviour changes
