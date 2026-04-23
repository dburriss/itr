## MODIFIED Requirements

### Requirement: Usecase logic lives in Itr.Domain as vertical slices
Feature usecase logic SHALL reside in `src/domain/` under plural concept sub-folders (`Portfolios/`, `Tasks/`, `Backlogs/`). The `Itr.Features` project SHALL be deleted.

Each command operation SHALL be its own file containing a single public `let execute` function. Query functions SHALL live in a `Query.fs` per concept, exposing named functions (`list`, `filter`, `getDetail`, `load`, `resolveActiveProfile`, etc.).

The compile order within each concept SHALL be: `Query.fs` first, then command files. Concept sub-folders SHALL compile in order: `Portfolios/`, `Tasks/`, `Backlogs/`.

Command usecases that perform IO SHALL own their operation sequencing in `Itr.Domain` through effectful entry points. Those entry points SHALL expose only the dependency interfaces they need by using intersection-constrained dependency surfaces rather than a broad application dependency type.

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

#### Scenario: Effectful usecase surfaces expose only required dependencies
- **WHEN** a caller uses a command usecase such as `Tasks.Plan.execute` or `Tasks.Approve.execute`
- **THEN** the function signature SHALL require only the dependency interfaces that operation uses rather than a catch-all dependency type

## ADDED Requirements

### Requirement: CLI structure is organised as routing plus vertical slices
The CLI layer SHALL organise command mapping and formatting into vertical slices under `src/cli/` while keeping `Program.fs` as the routing entry point.

`src/cli/CliArgs.fs` SHALL contain the Argu discriminated unions, `src/cli/AppDeps.fs` SHALL contain the CLI composition root, `src/cli/ErrorFormatting.fs` SHALL contain shared error rendering, and `src/cli/Shared/Rendering.fs` SHALL contain shared output helpers.

Each routed command SHALL have a dedicated CLI slice file under `src/cli/<Concept>/<Op>.fs` that maps CLI arguments into domain inputs and formats usecase outputs.

#### Scenario: Program fs is limited to routing concerns
- **WHEN** the refactor is complete
- **THEN** `src/cli/Program.fs` SHALL contain only opens, active patterns, portfolio or product resolution helpers, dispatch arms, and `main`

#### Scenario: CLI support files exist
- **WHEN** the refactor is complete
- **THEN** `src/cli/CliArgs.fs`, `src/cli/AppDeps.fs`, `src/cli/ErrorFormatting.fs`, and `src/cli/Shared/Rendering.fs` SHALL exist

#### Scenario: Command-specific CLI slice files exist
- **WHEN** the refactor is complete
- **THEN** command-specific files such as `src/cli/Tasks/Plan.fs`, `src/cli/Tasks/Approve.fs`, `src/cli/Backlogs/List.fs`, and `src/cli/Portfolios/ProductInfo.fs` SHALL own per-command input mapping and result formatting

### Requirement: Usecase refactor is covered by in-memory acceptance and communication tests
The refactor SHALL provide in-memory test doubles and small builder helpers so usecase behavior can be tested at the natural boundary without structural assertions on collaborators.

Acceptance tests SHALL prefer in-memory doubles for filesystem, stores, config, and harness behavior. Communication tests SHALL remain small and focused on IO contracts, mappings, and formatter output.

#### Scenario: In-memory test doubles support usecase acceptance tests
- **WHEN** a domain usecase test is written for an extracted command operation
- **THEN** the test suite SHALL be able to exercise the usecase through in-memory filesystem, store, config, and harness doubles instead of structural mocks

#### Scenario: Builders support focused scenario setup
- **WHEN** a test needs domain data or dependency state
- **THEN** small builders such as `A.<Thing>` and `Given.<Thing>` SHALL make the required setup explicit and composable

#### Scenario: Formatter and IO contract checks stay separate from usecase behavior tests
- **WHEN** the refactor introduces command-specific formatting or path-writing logic
- **THEN** any contract assertions about those boundaries SHALL be covered by small communication tests rather than by asserting internal orchestration inside usecase acceptance tests
