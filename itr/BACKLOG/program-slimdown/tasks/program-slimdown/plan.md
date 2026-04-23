# Slim Program.fs to pure routing

**Task ID:** program-slimdown
**Backlog Item:** program-slimdown
**Repo:** itr

## Description

Reduce src/cli/Program.fs from ~1666 lines to ~400 lines by extracting Argu DUs to CliArgs.fs, composition root to AppDeps.fs, error formatters to ErrorFormatting.fs, tightening usecase deps with intersection constraints, building in-memory test infrastructure, pushing handler logic into effectful usecases with vertical-slice CLI files, and finally inlining trivialised handlers into dispatch arms.

Testing expectations for this refactor are defined in
`knowledge/testing-style-guide.md`.

## Scope

**Included:**
- Extract all Argu DU definitions (~lines 1-321) to `src/cli/CliArgs.fs`
- Extract `AppDeps` composition root (~lines 327-415) to `src/cli/AppDeps.fs`
- Extract `formatBacklogError`, `formatTaskError`, `formatPortfolioError` to `src/cli/ErrorFormatting.fs`
- Refactor existing usecase signatures to use intersection constraints (e.g. `#IFileSystem & #ITaskStore`)
- Create `tests/InMemory/` infrastructure: `InMemoryFileSystem`, `InMemoryTaskStore`, `InMemoryBacklogStore`, `InMemoryViewStore`, `InMemoryPortfolioConfig`, `InMemoryProductConfig`, `InMemoryAgentHarness`, and `Fakes` builder helpers
- Extract all 13 handlers to effectful domain usecases + vertical-slice `src/cli/<Concept>/<Op>.fs` files
- Inline trivialised handlers into `dispatch` match arms; remove `handleXxx` functions
- Create `src/cli/Shared/Rendering.fs` for cross-cutting render helpers

**Excluded:**
- Interactive prompt logic (`handleBacklogAdd --interactive`, `ProductInit` `AnsiConsole.Ask` calls) stays in CLI
- `ErrorFormatting.fs` per-slice split deferred unless file exceeds ~150 lines; shared error DUs are reused across commands so one shared formatter file remains the natural seam for now
- Any behaviour changes; this is a pure structural refactor

## Steps

### Step 1 - Mechanical extracts (Change 1)

1. Create `src/cli/CliArgs.fs`; move all Argu DU definitions from `Program.fs` lines 1-321
2. Create `src/cli/AppDeps.fs`; move `AppDeps` type and composition wiring from lines 327-415
3. Create `src/cli/ErrorFormatting.fs`; move `formatBacklogError`, `formatTaskError`, `formatPortfolioError`
4. Add new files to `itr.fsproj` in correct order before `Program.fs`
5. Build + tests green; `Program.fs` ~1200 lines

### Step 2 - In-memory test infrastructure (Change 3)

1. Create `tests/InMemory/` project (or folder within existing test project)
2. Implement `InMemoryFileSystem` using `Testably.Abstractions`
3. Implement `InMemoryTaskStore`, `InMemoryBacklogStore`, `InMemoryViewStore`, `InMemoryPortfolioConfig`, `InMemoryProductConfig`
4. Implement `InMemoryAgentHarness` with canned responses and prompt recording when contract checks need it
5. Implement Burriss-style test builders using precise test-double naming:
   - `A.<Thing>` builders for test data
   - `Given.<Thing>` builders for dependency and scenario setup
   - in-memory implementations named by their actual double type (`Fake`, `Stub`, `Spy`) as described in `knowledge/testing-style-guide.md`
6. Follow `knowledge/testing-style-guide.md` when introducing this infrastructure:
   - Acceptance tests cover usecase behavior at the natural boundary with in-memory doubles
   - Communication tests cover minimal IO, mapping, and formatting contracts
   - Avoid structural assertions on collaborator calls or internal orchestration in usecase tests
7. Convert one existing usecase test as the pattern reference
8. Build + tests green

### Step 3 - Intersection constraints on existing usecases (Change 2)

1. For each usecase module (`Tasks.Take`, `Backlogs.Create`, `Portfolios.AddProfile`, etc.), replace the `deps: AppDeps` parameter with an intersection-constrained SRTP signature exposing only the interfaces it actually uses
2. Keep behavior unchanged; update tests that construct fakes to pass only the required dependency subset
3. Build + tests green

### Step 4 - Push handlers into effectful usecases (Change 4)

Work through all 13 handlers in order (smallest first):

| # | Handler | Target usecase | Approx LOC |
|---|---|---|---|
| 1 | `handleTaskApprove` | `Tasks.Approve.execute` (effectful) | ~30 |
| 2 | `handleProfileList` | `Portfolios.Query.listProfiles` | ~30 |
| 3 | `handleProductList` | `Portfolios.Query.listProducts` | ~35 |
| 4 | `handleBacklogInfo` | type-tighten only | ~10 |
| 5 | `handleTaskInfo` | `Tasks.Query.getDetail` | ~30 |
| 6 | `handleTaskList` | `Tasks.Query.list` (incl. sort) | ~85 |
| 7 | `handleViewList` | `Backlogs.Query.listViews` + `src/cli/Views/List.fs` | ~65 |
| 8 | `handleBacklogList` | `Backlogs.Query.list` | ~40 |
| 9 | `handleProductRegister` | `Portfolios.RegisterProduct.executeAndSave` | ~40 |
| 10 | `handleBacklogTake` | `Tasks.Take.executeAndWrite` (effectful) | ~80 |
| 11 | `handleBacklogAdd` | `Backlogs.Create.executeAndWrite` + interactive in CLI | ~100 |
| 12 | `handleProductInfo` | `Portfolios.Query.getProductInfo` (cwd-detect) | ~75 |
| 13 | `handleTaskPlan` | `Tasks.Plan.execute` (full IO) | ~115 |

For each handler:
1. Add the domain entry point with the agreed shape:
   - command operations use `execute : Input -> Effect<#DepsSubset, Result<Output, Error>>`
   - query operations stay in `Query.fs` with named functions such as `list`, `getDetail`, and `resolveProduct`
   - the domain function owns the operation's IO sequencing instead of leaving orchestration in `Program.fs`
2. Create `src/cli/<Concept>/<Op>.fs` with `toInput` and `Format.result`
3. Replace handler body with ~3-line dispatch entry
4. Add tests using the split in `knowledge/testing-style-guide.md`:
   - Acceptance tests for usecase behavior with in-memory doubles
   - Communication tests only where IO, mapping, or formatting contracts need explicit coverage
5. Build + tests green after each step

### Step 5 - Inline trivialised handlers (Change 5)

1. For any remaining named `handleXxx` function that is now <=5 lines, lift its body directly into the `dispatch` match arm
2. Remove the named function definitions
3. Add namespace opens for `Itr.Cli.Tasks`, `Itr.Cli.Backlogs`, `Itr.Cli.Portfolios`, `Itr.Cli.Views`
4. Final `Program.fs` should contain only opens, active patterns, `resolvePortfolio` / `resolveProduct`, flat dispatch arms, and `main`
5. Build + tests green; verify `Program.fs` ~400 lines

## Risks

- **Argu types in domain inputs** - domain modules must NOT take `ParseResults<XxxArgs>`; CLI layer owns `toInput` adapters
- **Interactive prompts** - `InteractivePrompts` / `AnsiConsole.Ask` stays in CLI for `handleBacklogAdd` and `ProductInit`
- **`AppDeps` satisfying all intersection constraints** - already implements every interface; no upcasts needed
- **State-machine sorting in `handleTaskList`** - moves into `Tasks.Query`; state ordering is domain knowledge
- **`CliArgs.fs` ordering in fsproj** - nested `ParseResults<XxxArgs>` means parent DUs must reference children; keep all Argu DUs in one file
- **Test drift toward structure assertions** - usecase tests should assert behavior at the natural boundary, not collaborator call counts or internal orchestration; keep IO/formatting contract checks minimal and in Communication tests
