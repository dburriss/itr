## Why

The tool currently has no concept of a portfolio — a user's collection of products and how to locate their coordination roots. Without this layer, every command must accept ad-hoc paths and cannot operate across multiple products or profiles (e.g., work vs. personal). The portfolio layer is the foundational data model that all product-level commands will build on.

## What Changes

- **NEW** `Itr.Domain` project added as the innermost architectural layer with all core portfolio domain types (no I/O, no dependencies)
- **NEW** `Itr.Commands` project gains portfolio use-cases: `loadPortfolio`, `resolveActiveProfile`, `resolveProduct`
- **NEW** `Itr.Adapters` project gains `PortfolioAdapter` for reading `portfolio.json` from disk and resolving the config file path
- `Itr.Commands` and `Itr.Adapters` gain a project reference to `Itr.Domain`
- CLI (`Itr.Cli`) gains a global `--profile` / `-p` flag and an `--output json` flag
- MCP tools gain an optional `profile` parameter
- Portfolio config stored at `~/.config/itr/portfolio.json` (overridable via `ITR_HOME` env var)
- Profile selection order: `--profile` flag > `ITR_PROFILE` env var > `defaultProfile` in config

## Capabilities

### New Capabilities

- `portfolio-config`: Load and parse `portfolio.json`; resolve the config file path from `ITR_HOME` or the default XDG location; handle `ConfigNotFound` and `ConfigParseError`
- `profile-resolution`: Select the active profile from a loaded portfolio using the `--profile` flag, `ITR_PROFILE` env var, or `defaultProfile`; handle `ProfileNotFound`
- `product-resolution`: Resolve a named product's coordination root (`.itr/` directory) within the active profile across all three coordination modes (`standalone`, `primary-repo`, `control-repo`); validate slug format; handle `ProductNotFound`, `CoordRootNotFound`, `InvalidProductId`

### Modified Capabilities

## Impact

- New project `src/domain/Itr.Domain.fsproj` (no dependencies)
- `src/commands/Itr.Commands.fsproj` gains `<ProjectReference>` to `Itr.Domain`; new `Portfolio.fs` file
- `src/adapters/Itr.Adapters.fsproj` gains references to `Itr.Domain` and `Itr.Commands`; new `PortfolioAdapter.fs` file
- `src/cli/Program.fs` updated to add global `--profile` and `--output` args
- New test files in `tests/communication/` (unit) and `tests/acceptance/` (integration)
- No breaking changes to existing CLI commands; `--profile` is additive
