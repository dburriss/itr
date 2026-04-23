## 1. Mechanical CLI extracts

- [x] 1.1 Create `src/cli/CliArgs.fs` and move all Argu DU definitions out of `Program.fs`.
- [x] 1.2 Create `src/cli/AppDeps.fs` and move the CLI composition root wiring out of `Program.fs`.
- [x] 1.3 Create `src/cli/ErrorFormatting.fs` and move shared backlog, task, and portfolio error rendering into it.
- [x] 1.4 Create `src/cli/Shared/Rendering.fs` for shared table, text, and JSON output helpers used by CLI slice files.
- [x] 1.5 Update project compile order so the new CLI files build before `Program.fs`, then run `dotnet build` and `dotnet test`.

## 2. In-memory test infrastructure

- [x] 2.1 Add in-memory filesystem, store, config, and agent harness doubles under `tests/` using the repo's fake, stub, and spy naming guidance.
- [x] 2.2 Add small `A.<Thing>` and `Given.<Thing>` builders for test data and dependency setup that compose with pipelines.
- [x] 2.3 Convert one existing usecase test to the new acceptance-style pattern and add any minimal communication test coverage needed for formatter or IO contracts.
- [x] 2.4 Run `dotnet build` and `dotnet test` to lock in the testing pattern before broader refactoring.

## 3. Tighten existing usecase dependency surfaces

- [x] 3.1 Update existing domain command and query usecases to use intersection-constrained dependency signatures that expose only the interfaces each operation needs.
- [x] 3.2 Adjust any affected tests and call sites so they use the narrowed dependency surfaces without changing command behavior.
- [x] 3.3 Run `dotnet build` and `dotnet test` after the signature tightening step.

## 4. Move handler orchestration into domain and CLI slices

- [x] 4.1 Extract `handleTaskApprove`, `handleProfileList`, `handleProductList`, and `handleBacklogInfo` into the corresponding domain and CLI slice modules, with tests.
- [x] 4.2 Extract `handleTaskInfo`, `handleTaskList`, `handleViewList`, and `handleBacklogList` into the corresponding domain and CLI slice modules, with tests.
- [x] 4.3 Extract `handleProductRegister`, `handleBacklogTake`, and `handleBacklogAdd` into the corresponding domain and CLI slice modules, keeping interactive prompting in the CLI, with tests.
- [x] 4.4 Extract `handleProductInfo` and `handleTaskPlan` into the corresponding domain and CLI slice modules, including full IO ownership in the domain usecases, with tests.
- [x] 4.5 After each extraction batch, update dispatch to call the new `toInput` and `Format.result` helpers and run `dotnet build` plus `dotnet test`.

## 5. Finish pure routing shape

- [x] 5.1 Inline any remaining trivial `handleXxx` helpers directly into `Program.fs` dispatch arms and remove the named handler functions.
- [x] 5.2 Add or update CLI namespace opens so dispatch uses the new vertical-slice modules consistently.
- [x] 5.3 Verify `Program.fs` is reduced to routing concerns only and that required CLI slice/support files exist in their final locations.
- [x] 5.4 Run `dotnet build`, `dotnet test`, and `mise run verify` for the completed refactor.
