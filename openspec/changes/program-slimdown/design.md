## Context

`src/cli/Program.fs` currently mixes CLI argument definitions, dependency composition, command dispatch, formatter logic, and command-specific orchestration. The repo already decided on a vertical-slice domain structure and an effect system, but the CLI entry point still carries most of the operational branching and remains roughly 1600 lines.

The supplied planning docs lock several constraints for this refactor:
- command usecases own their IO sequencing through `execute : Input -> Effect<#DepsSubset, Result<Output, Error>>`
- dependency surfaces should be expressed with intersection constraints so each usecase exposes only the interfaces it actually needs
- CLI formatting should follow vertical slices with per-command `toInput` and `Format.result`
- shared error formatting stays centralized because the error unions are reused across commands
- tests should prefer in-memory acceptance coverage at the natural boundary, with small communication tests for formatting and IO contracts

This is a cross-cutting architectural change across the CLI, domain usecases, test infrastructure, and project compile ordering, so design guidance is useful before implementation starts.

## Goals / Non-Goals

**Goals:**
- Reduce `Program.fs` to a routing-focused module with minimal per-command dispatch arms.
- Move command orchestration into domain usecases and query modules without changing command behavior.
- Establish a stable CLI vertical-slice structure that keeps command mapping and formatting close to each operation.
- Add in-memory test doubles and builders that support acceptance-style usecase tests and minimal communication tests.
- Keep production wiring simple by letting `AppDeps` implement the full dependency surface while usecases expose only the subset they need.

**Non-Goals:**
- No intentional user-facing behavior changes for task, backlog, portfolio, product, or view commands.
- No redesign of shared domain error unions beyond continuing to format them from a single CLI file.
- No movement of interactive prompt ownership out of the CLI for `backlog add --interactive` or `product init` prompt flows.
- No new runtime dependency or protocol changes beyond the existing architecture already in the repo.

## Decisions

### Keep `Program.fs` as pure routing
`Program.fs` will retain only top-level CLI concerns: opens, active patterns, portfolio/product resolution helpers, a flat dispatch function, and `main`.

Rationale:
- This creates a predictable entry point for every command.
- It removes command-specific sequencing from the highest-conflict file in the CLI.
- It aligns the code with the target shape already documented in the planning notes.

Alternative considered:
- Leave thin handler functions in `Program.fs`. Rejected because it preserves an unnecessary extra layer and keeps command logic spread between handlers and dispatch.

### Use effectful domain usecases with intersection-constrained dependencies
Each command usecase will expose the minimum dependency surface it needs, for example `#IFileSystem & #ITaskStore`, while `AppDeps` remains the full production implementation.

Rationale:
- This preserves simple production wiring while improving locality and testability.
- It addresses the "fat deps" concern without introducing service locators or bespoke dependency records per command.
- It lets acceptance tests build only the doubles required for the behavior under test.

Alternative considered:
- Keep orchestration in CLI slice files while domain usecases remain partially pure. Rejected because the repo has already chosen the effectful usecase shape and the refactor goal is to remove orchestration from `Program.fs`, not relocate it to another CLI layer.

### Split CLI support code by natural seams
The CLI layer will be reorganized into:
- `CliArgs.fs` for Argu discriminated unions
- `AppDeps.fs` for composition root wiring
- `ErrorFormatting.fs` for shared error rendering
- `Shared/Rendering.fs` for table/json/text helper functions
- `src/cli/<Concept>/<Op>.fs` files for per-command mapping and formatting

Rationale:
- `CliArgs.fs` must remain consolidated because nested `ParseResults<XxxArgs>` create ordering constraints.
- `ErrorFormatting.fs` stays shared because the same error unions cross multiple commands.
- Per-command slice files improve context locality and reduce merge conflicts on shared formatter files.

Alternative considered:
- Split error formatting by command. Rejected because it duplicates logic for shared error unions without adding a useful seam.

### Keep interactive prompting in CLI, but funnel non-interactive flows into the same usecases
Interactive branches for `backlog add --interactive` and `product init` will remain in the CLI. They will build fully resolved inputs and call the same underlying domain operations used by non-interactive flows.

Rationale:
- Spectre.Console prompting is a UI concern.
- This keeps the domain free of terminal interaction while still centralizing the operation behavior in one usecase.

Alternative considered:
- Push prompting into domain usecases. Rejected because it violates the architectural boundary between domain logic and terminal UI.

### Build in-memory test infrastructure around behavior-focused usecase entry points
The tests will add in-memory implementations for filesystem, stores, config, and harness dependencies, plus small builder helpers following the existing naming guidance.

Rationale:
- This supports the chosen effectful usecase shape without falling back to structural mocks.
- It matches the repo's testing guidance: acceptance tests at the usecase boundary, communication tests for formatter and IO contracts.

Alternative considered:
- Continue using broader fixtures or structural collaborator assertions. Rejected because they obscure behavior and create brittle refactor costs.

## Risks / Trade-offs

- [Compile ordering across many new F# files] -> Update project files incrementally and keep `CliArgs.fs` before consumers, shared CLI files before slices, and concept folders in the documented order.
- [Behavior drift while moving handler sequencing into usecases] -> Move operations in small vertical slices and keep build/test green after each extracted command.
- [Test infrastructure becoming too broad] -> Start with the minimal fake surfaces required by the first converted usecase test and expand only as later handlers need more coverage.
- [Shared rendering helpers growing into a behavior-heavy utility layer] -> Keep `Shared/Rendering.fs` small and limited to presentation helpers rather than command logic.
- [Some commands still need CLI-owned inputs such as prompt answers or cwd-derived context] -> Keep those concerns as explicit CLI-to-domain adapters instead of leaking Argu or console types into the domain.

## Migration Plan

1. Perform the mechanical CLI extracts first so subsequent command work no longer competes inside `Program.fs`.
2. Add the in-memory test infrastructure and convert one usecase test to establish the pattern.
3. Tighten existing usecase signatures to intersection constraints without changing behavior.
4. Move handler orchestration into domain usecases in the planned order, adding matching CLI slice files and tests as each command is extracted.
5. Inline any remaining trivial handlers into dispatch arms and leave `Program.fs` as pure routing.
6. Verify the final state with `dotnet build`, `dotnet test`, and the repo verification command before merging.

Rollback is straightforward because the change is structural and local to source layout and tests: revert the refactor commit set if any slice migration introduces unacceptable regressions.

## Open Questions

- None currently. The planning documents already lock the main architectural choices, command order, and testing approach needed to begin implementation.
