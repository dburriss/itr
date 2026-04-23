# Plan: Slim `src/cli/Program.fs` to Pure Routing

Date: 2026-04-23

Context: Commit `b426f` (refactor-program-entry-point) added active patterns
and `resolvePortfolio`/`resolveProduct` helpers but, by design, deferred the
real fat in `Program.fs`. The file is still ~1666 lines. This plan covers how
to actually slim it down.

---

## Decisions locked

- **Usecase style:** Option A — effectful usecases own all IO
  (`execute : Input -> Effect<'deps, Result<Output, Error>>`).
- **Deps shape:** Per-usecase **intersection constraints** so each usecase
  declares its minimum surface, e.g.
  `Effect<#IFileSystem & #ITaskStore & #IBacklogStore & #IAgentHarness, _>`.
  `AppDeps` continues to implement every interface, so satisfies any subset
  at call sites.
- **Tests:** In-memory implementations of each interface plus small
  composable builder helpers (Devon Burriss "maintainable unit tests" style).
- **Formatters:** **vertical-slice** — each command owns its own
  `toInput` and `Format.result` in `src/cli/<Concept>/<Op>.fs`.
  Cross-cutting render helpers (table styling, JSON escaping, OutputFormat
  dispatch) live in `src/cli/Shared/Rendering.fs`. Spectre.Console stays
  out of domain & adapters.
- **Error formatting:** stays as a single `src/cli/ErrorFormatting.fs`.
  Error DUs are domain-level concepts shared across many usecases; their
  string rendering is a single source of truth. Per-slice error formatting
  would only pay off with per-usecase error DUs (separate architectural
  decision). Total surface is ~36 lines — context noise is trivial.

---

## Why "fat IDeps" worry doesn't apply

With intersection constraints each usecase signature exposes only the
interfaces it actually touches. Tests build a fake satisfying just that
subset; `Tasks.Approve` does not need to mock `IViewStore` or
`IPortfolioConfig`. Production wiring stays trivial because `AppDeps`
implements every interface.

Example:

```fsharp
// Tasks/Plan.fs
let execute (input: Input)
    : Effect<#IFileSystem & #ITaskStore & #IBacklogStore & #IAgentHarness,
             Result<Output, TaskError>> =
    effectResult { ... }
```

```fsharp
// Tasks/Approve.fs
let execute (input: Input)
    : Effect<#IFileSystem & #ITaskStore, Result<Output, TaskError>> =
    effectResult { ... }
```

---

## Target shape of `Program.fs` (~400 lines)

1. `open` lines
2. Active patterns (already extracted)
3. `resolvePortfolio` / `resolveProduct` helpers (already extracted)
4. Flat `dispatch` where each leaf is ~3 lines:

```fsharp
| TaskPlan args ->
    resolvePortfolio deps configPath profile
    |> Result.bind (resolveProduct deps)
    |> Result.bind (fun r ->
        Tasks.Plan.execute (TaskPlanArgs.toInput r args)
        |> Effect.run deps
        |> Result.mapError formatTaskError)
    |> Result.map (TaskFormatter.formatPlanResult format)
```

5. `main`

`AppDeps`, Argu DUs, and error formatters live in their own files.

---

## Five sequential changes

Each change is independently shippable, builds, and passes tests.

### Change 1 — Mechanical extracts (~450 lines out, zero logic change)

- `src/cli/CliArgs.fs` — all Argu DUs (lines 1–321 of current `Program.fs`)
- `src/cli/AppDeps.fs` — composition root (lines 327–415)
- `src/cli/ErrorFormatting.fs` — `formatBacklogError`, `formatTaskError`,
  `formatPortfolioError`

After: `Program.fs` ≈ 1200 lines.

### Change 2 — Tighten interfaces with intersection constraints

Refactor existing usecases (`Tasks.Take`, `Backlogs.Create`,
`Portfolios.AddProfile`, etc.) so signatures expose minimum deps via
intersection types. No behavior change. Tests update to use the new fakes
infra (see Change 3).

### Change 3 — In-memory test infrastructure

Create `tests/InMemory/`:

- `InMemoryFileSystem` (use `Testably.Abstractions` per AGENTS.md)
- `InMemoryTaskStore`, `InMemoryBacklogStore`, `InMemoryViewStore`,
  `InMemoryPortfolioConfig`, `InMemoryProductConfig`
- `InMemoryAgentHarness` (records prompts, returns canned responses)
- Builder helpers: `Fakes.empty()`, `Fakes.withTask t`,
  `Fakes.withBacklogItem i`, etc., composable via `|>`

Convert one existing usecase test as the pattern reference.

### Change 4 — Push handler logic into effectful usecases

For each `handleXxx` in `Program.fs`, create or extend the matching domain
module with an effectful `execute` that owns the full operation. Order of
attack — smallest payoff first to refine the pattern, then biggest:

| # | Handler → New/extended usecase | LOC moved |
|---|---|---|
| 1 | `handleTaskApprove` → `Tasks.Approve.execute` (effectful) | ~30 |
| 2 | `handleProfileList` → `Portfolios.Query.listProfiles` returns rows | ~30 |
| 3 | `handleProductList` → `Portfolios.Query.listProducts` returns rows | ~35 |
| 4 | `handleBacklogInfo` → already thin, just type-tighten | ~10 |
| 5 | `handleTaskInfo` → `Tasks.Query.getDetail` returns view directly | ~30 |
| 6 | `handleTaskList` → `Tasks.Query.list` accepts shaped filter incl. sort | ~85 |
| 7 | `handleViewList` → `Backlogs.Query.listViews` + new `ViewFormatter` | ~65 |
| 8 | `handleBacklogList` → `Backlogs.Query.list` already mostly thin | ~40 |
| 9 | `handleProductRegister` → `Portfolios.RegisterProduct.executeAndSave` | ~40 |
| 10 | `handleBacklogTake` → `Tasks.Take.executeAndWrite` (effectful) | ~80 |
| 11 | `handleBacklogAdd` → `Backlogs.Create.executeAndWrite` + interactive split kept in CLI | ~100 |
| 12 | `handleProductInfo` → `Portfolios.Query.getProductInfo` (handles cwd-detect) | ~75 |
| 13 | `handleTaskPlan` → `Tasks.Plan.execute` (full IO incl. template, harness, write) | ~115 |

Each step:

1. Add the effectful function in domain with intersection-typed deps.
2. Add a vertical-slice CLI file `src/cli/<Concept>/<Op>.fs` containing
   `toInput` and `Format.result`.
3. Replace handler with 3-line dispatch entry.
4. Add unit tests using in-memory infra.
5. Build + test green.

### Change 5 — Inline trivialised handlers into `dispatch`

Once each handler is ≤5 lines, lift them straight into the `match` arms.
Drop the named `handleXxx` functions.

---

## Final state

- `Program.fs` ≈ 400 lines: opens, active patterns, `dispatch`, `main`.
- Domain modules own all IO sequencing per operation.
- CLI layer is just: parse args → call usecase → format result → exit code.
- Each usecase has a focused unit test that proves the behavior it claims,
  mocking only the interfaces it actually uses.

---

## Risks / things to watch

- **Argu types in domain inputs** — domain modules must NOT take
  `ParseResults<XxxArgs>`. CLI layer owns `XxxArgs.toInput : resolved -> args -> Input`
  adapters.
- **Interactive prompts (`handleBacklogAdd --interactive`)** —
  `InteractivePrompts` uses `Spectre.Console` for `AnsiConsole.Ask`. This
  stays in CLI; only the non-interactive path moves to
  `Backlogs.Create.executeAndWrite`. The interactive branch builds an
  `Input` and calls the same usecase.
- **`AnsiConsole.Ask` calls inside `ProductInit` dispatch** — same: keep
  prompts in CLI, hand a fully-resolved `Input` to
  `Portfolios.InitProduct.execute`.
- **`AppDeps` satisfying all intersection constraints** — already does (it
  implements every interface), so no upcasts needed at call sites.
- **State-machine sorting in `handleTaskList`** — moves into `Tasks.Query`.
  State is domain knowledge, ordering of states is too.

---

## Reference: usecase shape comparison considered

For posterity, three styles were sketched before settling on Option A:

- **Option A (chosen):** effectful usecase owns full IO. Handler is 3 lines.
  Domain unit tests target the whole operation.
- **Option B:** keep usecase pure, orchestrate IO in CLI helper file per
  command. `Program.fs` shrinks but `cli/` grows. Conflicts with spec's
  uniform `effectResult { }` direction.
- **Option C:** pure `transition` + `renderXxx` helpers exposed alongside an
  effectful `executeAndWrite` wrapper. Best testability of pure pieces but
  doubles the surface per usecase.

Option A wins because intersection constraints neutralise the "fat IDeps"
concern, and a meaningful usecase test exercises the whole effectful
operation against in-memory adapters — exactly the seam that gives
production-relevant confidence.

---

## Vertical-slice CLI structure

Goal: when working on a usecase, only the files relevant to that usecase
are loaded into context. No per-layer formatter file holding rendering for
five unrelated commands.

### Layout

```
src/cli/
  CliArgs.fs                 ← all Argu DUs (kept together; nested
                                ParseResults<XxxArgs> means parent DUs
                                must reference children)
  AppDeps.fs                 ← composition root
  ErrorFormatting.fs         ← format{Backlog,Task,Portfolio}Error
  Shared/
    Rendering.fs             ← table styling, JSON escaping,
                                OutputFormat dispatch helpers
  Tasks/
    Plan.fs                  ← toInput + Format.result
    Approve.fs
    Take.fs                  ← (CLI command is `backlog take`, but the
                                operation is task-take, so it lives under
                                Tasks/ to mirror the domain)
    List.fs
    Info.fs
  Backlogs/
    Add.fs
    List.fs
    Info.fs
  Portfolios/
    InitProduct.fs
    RegisterProduct.fs
    AddProfile.fs
    SetDefaultProfile.fs
    ListProfiles.fs
    ListProducts.fs
    ProductInfo.fs
  Views/
    List.fs
  Program.fs                 ← opens, active patterns, dispatch, main
                                (~400 lines)
```

### Slice file shape

```fsharp
module Itr.Cli.Tasks.Plan

open Itr.Cli              // CliArgs, ErrorFormatting, Shared.Rendering
open Itr.Domain.Tasks

let toInput (resolved: ResolvedProduct) (args: ParseResults<TaskPlanArgs>)
    : Plan.Input =
    { TaskId      = TaskId.create (args.GetResult TaskPlanArgs.Task_Id)
      CoordRoot   = resolved.CoordRoot.AbsolutePath
      UseAi       = args.Contains TaskPlanArgs.Ai
      Debug       = args.Contains TaskPlanArgs.Debug
      AgentConfig = AgentConfigResolver.resolve resolved }

module Format =
    let result (fmt: OutputFormat) (out: Plan.Output) : unit =
        match fmt with
        | Json  -> Shared.Rendering.json [ "ok", "true"
                                           "planPath", out.PlanPath ]
        | Text  -> printfn "%s" out.PlanPath
        | Table -> printfn "Plan written: %s" out.PlanPath
```

### Working-on-Plan context

Editing `Tasks.Plan` loads:

- `src/domain/Tasks/Plan.fs` — Input, Output, execute
- `src/cli/Tasks/Plan.fs` — toInput, Format.result
- `tests/domain/Tasks/Plan.fs` — tests

Plus shared infra only when actually touched: `Shared/Rendering.fs`,
`CliArgs.fs`, `ErrorFormatting.fs`. No unrelated formatter neighbours.

### Updated dispatch arms

```fsharp
| TaskPlan args ->
    resolvePortfolio deps configPath profile
    |> Result.bind (resolveProduct deps)
    |> Result.bind (fun r ->
        Domain.Tasks.Plan.execute (Cli.Tasks.Plan.toInput r args)
        |> Effect.run deps
        |> Result.mapError formatTaskError)
    |> Result.map (Cli.Tasks.Plan.Format.result format)
```

`Program.fs` opens grow:

```fsharp
open Itr.Cli.Tasks
open Itr.Cli.Backlogs
open Itr.Cli.Portfolios
open Itr.Cli.Views
```

### Why error formatting is *not* sliced

Considered and rejected. `BacklogError`, `TaskError`, `PortfolioError`
are domain-level DUs reused across many usecases:

- `formatPortfolioError` is hit by every command (bootstrap +
  `resolvePortfolio`).
- `formatBacklogError` is used by Backlog.* and also `Tasks.Plan` (loads
  the backlog item) and `Views.List` (reads archived items).
- `formatTaskError` is used by Task.* and `Backlog.Take` (writes tasks).

Slicing them would either duplicate the same `match` per usecase or
require redesigning the domain to expose per-usecase error DUs. Total
surface today is ~36 lines, so the noise cost of one shared file is
trivial. Split along the natural seam (per error DU = three files) only
if `ErrorFormatting.fs` ever grows past ~150 lines.

### Trade-offs accepted

- More files (~14 `cli/<Concept>/<Op>.fs` slice files vs 4 layered
  formatters). Worth it for context locality and zero merge conflicts on
  shared formatter files.
- `Shared/Rendering.fs` is the one cross-cutting risk surface — keep it
  small and behaviour-free.
