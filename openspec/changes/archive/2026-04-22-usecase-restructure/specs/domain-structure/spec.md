## MODIFIED Requirements

### Requirement: Domain types are organised into per-concept files
The domain layer SHALL organise types and interfaces into separate files per concept: `Types.fs`, `Validation.fs`, `Portfolio.fs`, `Product.fs`, `Task.fs`, and `Backlog.fs`. The monolithic `Domain.fs` and `Interfaces.fs` SHALL NOT exist after the refactor.

The domain layer SHALL also contain vertical-slice usecase modules under plural concept sub-folders. Each concept sub-folder SHALL contain a `Query.fs` and one file per command operation. The `Itr.Features` project SHALL NOT exist after the restructure.

#### Scenario: Build succeeds after split
- **WHEN** `Domain.fs` and `Interfaces.fs` are deleted and their content distributed across the per-concept files
- **THEN** `dotnet build` SHALL complete with no errors

#### Scenario: All tests pass after split
- **WHEN** the per-concept file split is complete
- **THEN** `dotnet test` SHALL pass with no behaviour changes

#### Scenario: Build succeeds after usecase restructure
- **WHEN** the `Itr.Features` project is deleted and all usecase logic is moved into vertical-slice modules in `Itr.Domain`
- **THEN** `dotnet build` SHALL complete with no errors

#### Scenario: All tests pass after usecase restructure
- **WHEN** the vertical-slice restructure is complete
- **THEN** `dotnet test` SHALL pass with no behaviour changes

## ADDED Requirements

### Requirement: Usecase logic lives in Itr.Domain as vertical slices
Feature usecase logic SHALL reside in `src/domain/` under plural concept sub-folders (`Portfolios/`, `Tasks/`, `Backlogs/`). The `Itr.Features` project SHALL be deleted.

Each command operation SHALL be its own file containing a single public `let execute` function. Query functions SHALL live in a `Query.fs` per concept, exposing named functions (`list`, `filter`, `getDetail`, `load`, `resolveActiveProfile`, etc.).

The compile order within each concept SHALL be: `Query.fs` first, then command files. Concept sub-folders SHALL compile in order: `Portfolios/`, `Tasks/`, `Backlogs/`.

#### Scenario: Portfolios vertical slice exists
- **WHEN** the restructure is complete
- **THEN** `src/domain/Portfolios/Query.fs`, `BootstrapIfMissing.fs`, `SetDefaultProfile.fs`, `AddProfile.fs`, `RegisterProduct.fs`, and `InitProduct.fs` SHALL exist

#### Scenario: Tasks vertical slice exists
- **WHEN** the restructure is complete
- **THEN** `src/domain/Tasks/Query.fs`, `Take.fs`, `Plan.fs`, and `Approve.fs` SHALL exist

#### Scenario: Backlogs vertical slice exists
- **WHEN** the restructure is complete
- **THEN** `src/domain/Backlogs/Query.fs` and `Create.fs` SHALL exist

#### Scenario: Itr.Features project is deleted
- **WHEN** the restructure is complete
- **THEN** `src/features/` SHALL NOT exist and the CLI project SHALL NOT reference `Itr.Features`

#### Scenario: Public API surface is accessible via new qualified names
- **WHEN** the restructure is complete
- **THEN** all operations previously accessible via `Itr.Features.Portfolio`, `Itr.Features.Task`, and `Itr.Features.Backlog` SHALL be accessible via `Portfolios.*`, `Tasks.*`, and `Backlogs.*` qualified names in `Itr.Domain`

#### Scenario: fsproj compile order is correct
- **WHEN** the restructure is complete
- **THEN** within each concept sub-folder, `Query.fs` SHALL compile before command files; concept folders SHALL compile in order Portfolios → Tasks → Backlogs
