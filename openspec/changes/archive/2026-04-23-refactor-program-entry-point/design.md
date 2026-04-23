## Context

`Program.fs` (2353 lines) is the CLI entry point. It currently handles argument parsing, dependency wiring, and also contains: inline JSON/text/table formatting per handler, string parsing helpers (`tryParseTaskState`, `tryParseBacklogItemStatus`), AI harness selection (acp vs opencode-http protocol dispatch), and filesystem traversal (walk-up to find `product.yaml`). The `dispatch` function alone spans ~550 lines with 4–6 level `TryGetResult` nesting chains.

The adapters layer (`src/adapters/`) already contains `AcpAdapter.fs`, `OpenCodeAdapter.fs`, `PortfolioAdapter.fs`, and `YamlAdapter.fs`. The domain layer (`src/domain/`) contains `Task.fs` and `Backlog.fs`.

## Goals / Non-Goals

**Goals:**
- `Program.fs` contains only: Argu argument definitions, `AppDeps` wiring, handler function calls, and a flat `dispatch` function
- Output formatting (JSON/text/table) lives in per-type formatter modules under `src/adapters/`
- String parsing helpers live in the adapter/CLI layer (not domain)
- AI harness selection is encapsulated in `AgentHarnessSelector`
- Filesystem traversal for `product.yaml` is encapsulated in `ProductLocator`
- `dispatch` is simplified to ~40-line flat match using partial active patterns

**Non-Goals:**
- Changes to Argu argument definitions
- Changes to error formatting functions (`formatBacklogError`, etc.)
- Changes to write-command handlers at MVP (can be brought into scope via decision)
- Refactoring `AppDeps` composition root
- Any domain logic changes

## Decisions

**1. Parsing helpers stay in adapter/CLI layer**
`tryParseTaskState` and `tryParseBacklogItemStatus` parse CLI input strings. Placing them in domain would introduce CLI concerns into the domain. They belong in a `CliParsers` module in `src/adapters/` or remain in a dedicated `src/cli/CliHelpers.fs`.

Alternatives considered: move to domain (rejected — domain should not know about CLI string formats).

**2. Per-type formatters, not a single generic OutputFormatter**
Each entity type (`Task`, `BacklogItem`, `Portfolio`) has structurally different formatting requirements. A single `OutputFormatter` with a generic `format<'T>` would require dynamic dispatch or type tests. Per-type modules (`TaskFormatter`, `BacklogFormatter`, `PortfolioFormatter`) are more idiomatic F# and allow each formatter to be independently testable.

Alternatives considered: single generic module (rejected — loses static type safety, complicates the interface).

**3. OutputFormat enum moves to a shared types location**
`OutputFormat` is referenced by all formatter modules and by `Program.fs`. Define it in `src/adapters/OutputFormat.fs` (or `src/cli/OutputFormat.fs`) to avoid circular dependencies.

**4. Active patterns for dispatch simplification**
Replace nested `TryGetResult` chains with ~16 partial active patterns that each map one Argu command path to the leaf `ParseResults`. A shared `resolvePortfolio` helper extracts the repeated `load → resolveActiveProfile` boilerplate. Result: `dispatch` becomes a flat `match results with` of ~40 lines.

Alternatives considered: keep nested structure but extract helpers (rejected — doesn't meaningfully reduce nesting depth or cognitive overhead).

**5. Write handlers brought into OutputFormat scope**
Replace `outputJson: bool` parameters with `OutputFormat` for consistency. This was initially excluded but the plan.md decision log includes it. Keeping two parallel output-format representations would create confusion.

## Risks / Trade-offs

- **Breaking existing tests** → Tests that import helpers from `Program.fs` will break when those helpers move. Mitigation: update import paths; add re-exports during transition if needed.
- **Circular dependencies** → If `OutputFormat` is defined in `src/adapters/` and `Program.fs` (in `src/cli/`) imports it, dependency direction is fine. If domain imports formatter, that would be a cycle. Mitigation: formatters live in adapters, domain imports nothing from adapters.
- **Behavioral drift in formatters** → New formatter modules must produce byte-identical output. Mitigation: golden tests (Verify) comparing output before and after for each handler.
- **Scope creep in dispatch refactor** → Active pattern approach is a meaningful rewrite of `dispatch`. Mitigation: handler function bodies remain untouched; only the dispatch routing changes.
