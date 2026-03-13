## Context

The codebase currently has no concept of a "portfolio" — a user's named collection of products with their coordination roots. Commands that need to operate on a product today require explicit path arguments. The plan is to introduce a stable, testable portfolio resolution pipeline that all entry points (CLI, TUI, MCP, Server) can share without duplicating resolution logic.

The architectural shape is an onion: `Itr.Domain` (pure types, no I/O) ← `Itr.Commands` (use cases, injected I/O) ← `Itr.Adapters` (real I/O) ← entry points. This change adds the first concrete vertical slice through all layers.

## Goals / Non-Goals

**Goals:**
- Introduce `Itr.Domain` as the new innermost project containing all portfolio/profile/product types
- Implement three pure application use-cases in `Itr.Commands`: `loadPortfolio`, `resolveActiveProfile`, `resolveProduct`
- Implement `Itr.Adapters.PortfolioAdapter` to read `portfolio.json` from disk via `System.Text.Json`
- Resolve config path from `ITR_HOME` env var or default `~/.config/itr/portfolio.json`
- Expose `--profile`/`-p` and `--output json` global flags in `Itr.Cli`
- Make all I/O injectable so use-cases are unit-testable without a filesystem

**Non-Goals:**
- Product-level backlog, feature, or view operations
- `itr init` / `itr product init` scaffolding commands
- Git clone detection or branch validation for `control-repo` mode
- Profile-level `repoRoots` discovery scanning
- Cross-product aggregation

## Decisions

### 1. New `Itr.Domain` project rather than adding types to `Itr.Commands`

**Decision:** Create `src/domain/Itr.Domain.fsproj` with no project references and no external packages.

**Rationale:** Domain types must be importable by every layer without pulling in application-level dependencies. Keeping them in a dedicated project with zero dependencies enforces this cleanly and makes the boundary explicit.

**Alternatives considered:** Adding domain types to `Itr.Commands` (rejected: would prevent adapters from depending on domain types without also depending on commands).

### 2. Injected I/O functions rather than interface types

**Decision:** Use plain `string -> bool` and `string -> string option` function parameters for `dirExists` and `readEnv` in use-cases.

**Rationale:** F# functions are first-class. Injecting functions keeps use-cases pure and trivially testable without introducing interface boilerplate. Real implementations (`Directory.Exists`, `Environment.GetEnvironmentVariable`) are wired at the entry-point layer.

**Alternatives considered:** DI container / interface types (rejected: unnecessary ceremony for two functions).

### 3. `System.Text.Json` for JSON parsing (no extra package)

**Decision:** Use the built-in `System.Text.Json` APIs with `camelCase` property names and `JsonIgnoreCondition.WhenWritingNull`.

**Rationale:** .NET 10 ships STJ; adding `Newtonsoft.Json` or `FSharp.SystemTextJson` is unnecessary weight for a straightforward read-only deserialization task.

**Note:** STJ does not natively support F# discriminated unions. The `CoordinationRootConfig` DU will be deserialized via a custom `JsonConverter` that reads the `mode` field first and then constructs the correct case.

### 4. Profile name lookup is case-insensitive

**Decision:** `Portfolio.Profiles` is stored as `Map<ProfileName, Profile>` but lookups normalise the key to lowercase before comparison.

**Rationale:** Users should not need to remember exact casing in environment variables or flags. Stored names preserve original casing for display.

### 5. All three coordination modes resolve identically at the filesystem level

**Decision:** For MVP, `standalone`, `primary-repo`, and `control-repo` all resolve to `<configuredDir>/.itr/`. The `mode` value is semantic metadata only.

**Rationale:** Simplifies resolution code for MVP. Future tooling (git clone detection, branch validation) can use `mode` as a hint without requiring a migration.

## Risks / Trade-offs

- **STJ custom converter for DU**: Writing a `JsonConverter` for `CoordinationRootConfig` is slightly fiddly. Risk: edge cases in invalid JSON are not caught early. → Mitigation: unit tests covering all three modes plus invalid/missing `mode` field.

- **Case-insensitive profile lookup via linear scan**: `Map` is ordered by structural equality of `ProfileName`. A case-insensitive lookup requires iterating keys. → Mitigation: portfolios will have very few profiles (< 10); performance is not a concern. Document the behaviour in code comments.

- **`~` expansion is platform-dependent**: `~` is a shell convention, not a .NET path feature. Expansion must be implemented explicitly (`Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), rest)`). → Mitigation: centralise expansion in `Itr.Adapters` (never domain/commands); test with a stubbed `$HOME`.

## Migration Plan

No migration required. The portfolio layer is entirely additive:
- Existing CLI commands continue to work without `--profile`
- `portfolio.json` does not exist for current users; `ConfigNotFound` is the graceful fallback until `itr init` is implemented
- New project references are backwards-compatible

## Open Questions

- None blocking MVP. `--output json` flag format is specified in `ARCHITECTURE.md §9` and can be wired at CLI layer without design changes.
