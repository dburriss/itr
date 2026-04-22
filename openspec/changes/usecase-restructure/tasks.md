## 1. Create Portfolios vertical slice

- [ ] 1.1 Create `src/domain/Portfolios/Query.fs` — module `Itr.Domain.Portfolios.Query` — containing `IPortfolioDeps`, `load`, `resolveActiveProfile`, `loadAllDefinitions`, `resolveProduct` (and private `tryFindProfileCaseInsensitive`)
- [ ] 1.2 Create `src/domain/Portfolios/BootstrapIfMissing.fs` — module `Itr.Domain.Portfolios.BootstrapIfMissing` — containing `execute`
- [ ] 1.3 Create `src/domain/Portfolios/SetDefaultProfile.fs` — module `Itr.Domain.Portfolios.SetDefaultProfile` — containing `Input` type and `execute`
- [ ] 1.4 Create `src/domain/Portfolios/AddProfile.fs` — module `Itr.Domain.Portfolios.AddProfile` — containing `Input` type and `execute`
- [ ] 1.5 Create `src/domain/Portfolios/RegisterProduct.fs` — module `Itr.Domain.Portfolios.RegisterProduct` — containing `Input` type and `execute`
- [ ] 1.6 Create `src/domain/Portfolios/InitProduct.fs` — module `Itr.Domain.Portfolios.InitProduct` — containing `Input` type and `execute`

## 2. Create Tasks vertical slice

- [ ] 2.1 Create `src/domain/Tasks/Query.fs` — module `Itr.Domain.Tasks.Query` — containing `TaskSummary`, `SiblingTask`, `TaskDetail` types and `list`, `filter` (with `Input = { BacklogId; Repo; State; Exclude }`), `getDetail` (with `Input = { TaskId; AllTasks; TaskYamlPath }`) functions
- [ ] 2.2 Create `src/domain/Tasks/Take.fs` — module `Itr.Domain.Tasks.Take` — containing `Input` type (renamed from `TakeInput`), private `deriveTaskId`, and `execute`
- [ ] 2.3 Create `src/domain/Tasks/Plan.fs` — module `Itr.Domain.Tasks.Plan` — containing `execute (task: ItrTask)` (single param, no record needed)
- [ ] 2.4 Create `src/domain/Tasks/Approve.fs` — module `Itr.Domain.Tasks.Approve` — containing `Input = { Task: ItrTask; PlanExists: bool }` and `execute`

## 3. Create Backlogs vertical slice

- [ ] 3.1 Create `src/domain/Backlogs/Query.fs` — module `Itr.Domain.Backlogs.Query` — containing `BacklogListFilter` type and `loadSnapshot`, `list`, `getDetail` functions (and private sort/order helpers)
- [ ] 3.2 Create `src/domain/Backlogs/Create.fs` — module `Itr.Domain.Backlogs.Create` — containing `Input` type, private `taskErrorToBacklogError`, and `execute`

## 4. Update Itr.Domain.fsproj

- [ ] 4.1 Add all new files to `src/domain/Itr.Domain.fsproj` in order: Portfolios (Query → BootstrapIfMissing → SetDefaultProfile → AddProfile → RegisterProduct → InitProduct), Tasks (Query → Take → Plan → Approve), Backlogs (Query → Create)
- [ ] 4.2 Run `dotnet build src/domain/Itr.Domain.fsproj` and verify it passes

## 5. Update CLI

- [ ] 5.1 Remove `open Itr.Features` from `src/cli/Program.fs`
- [ ] 5.2 Update all `Portfolio.*` call sites in `Program.fs` to `Portfolios.Query.*` / `Portfolios.<Op>.execute` per the design call-site table
- [ ] 5.3 Update all `Task.*` call sites in `Program.fs` to `Tasks.Query.*` / `Tasks.<Op>.execute`
- [ ] 5.4 Update all `Backlog.*` call sites in `Program.fs` to `Backlogs.Query.*` / `Backlogs.Create.execute`; update type references (`BacklogListFilter`, `CreateBacklogItemInput`, etc.)
- [ ] 5.5 Remove `Itr.Features` project reference from `src/cli/Itr.Cli.fsproj`
- [ ] 5.6 Run `dotnet build src/cli/Itr.Cli.fsproj` and verify it passes

## 6. Update tests

- [ ] 6.1 Remove `open Itr.Features` and update all `Itr.Features.*` references in `tests/acceptance/` to use new qualified names
- [ ] 6.2 Remove `Itr.Features` project reference from `tests/acceptance/Itr.Tests.Acceptance.fsproj`; add `Itr.Domain` reference if not already present
- [ ] 6.3 Run `dotnet test` and verify all tests pass

## 7. Delete Itr.Features

- [ ] 7.1 Delete `src/features/Portfolio/PortfolioUsecase.fs`, `src/features/Task/TaskUsecase.fs`, `src/features/Backlog/BacklogUsecase.fs`
- [ ] 7.2 Delete `src/features/Itr.Features.fsproj` and the `src/features/` directory
- [ ] 7.3 Run `dotnet build` and `dotnet test` and verify everything passes
