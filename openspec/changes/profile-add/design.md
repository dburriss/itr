## Context

`itr.json` holds the portfolio config: a set of named profiles, each optionally carrying a git identity and a list of product root paths. The config is bootstrapped by `settings-bootstrap`. Load is already implemented via `IPortfolioConfig.LoadConfig`; however, there is no save path exposed through the interface, and the low-level `readConfig`/`writeConfig` helpers in `PortfolioAdapter.fs` call `System.IO` directly rather than going through `IFileSystem`. This must be fixed before wiring up any write command.

`CliArgs` already has a `-p`/`--profile` flag for selecting an active profile. A new `profiles` management subcommand is introduced using `Profiles` (plural) as the DU case, avoiding any name conflict with the existing `Profile of string` flag.

## Goals / Non-Goals

**Goals:**
- Add `itr profiles add <name>` CLI command to insert a new named profile into `itr.json`
- Validate profile names against a slug rule (`[a-z0-9][a-z0-9-]*`) in the domain layer
- Support optional git identity (`--git-name`, `--git-email`) and `--set-default` flag
- Migrate `readConfig`/`writeConfig` to `IFileSystem` to enable testable acceptance tests
- Expose `SaveConfig` on `IPortfolioConfig` so the CLI handler can persist changes

**Non-Goals:**
- Listing, updating, or removing profiles (separate changes)
- Associating products with profiles (separate change)
- Changing how `-p`/`--profile` resolves the active profile

## Decisions

### D1: Add `SaveConfig` to `IPortfolioConfig` rather than a separate `IPortfolioStore` interface

The load and save operations are tightly coupled - they share the same path and serialization format. Putting them on the same interface keeps `AppDeps` simple and avoids a second constructor injection. A separate store interface would add indirection with no benefit at this scope.

*Alternative considered*: Separate `IPortfolioStore` with only `SaveConfig`. Rejected: extra interface complexity for a single new method.

### D2: Validate profile names in the domain layer with `ProfileName.tryCreate`

Name rules belong in the domain, not the CLI or adapter. The slug pattern `[a-z0-9][a-z0-9-]*` mirrors the existing `ProductId` validation, keeping validation conventions consistent. `InvalidProfileName of value * rules` is added to `PortfolioError` so all callers get a typed error.

*Alternative considered*: Validate in the CLI handler only. Rejected: domain invariants should be enforced at the domain boundary, not the presentation layer.

### D3: Migrate `readConfig`/`writeConfig` to `IFileSystem` before adding `SaveConfig`

`bootstrapIfMissing` already uses `IFileSystem`. Migrating the remaining I/O ensures acceptance tests can use `Testably.Abstractions` in-memory filesystem without needing a real disk. This also removes the `System.IO` direct dependency from the adapter.

### D4: Use `Profiles` (plural) as the CLI DU case name

`CliArgs` already has `Profile of string` for `-p`. Using `Profiles` (plural) for the management subcommand avoids the name collision without needing `CustomCommandLine` overrides in Argu.

### D5: `addProfile` usecase returns updated `Portfolio`; caller persists

The usecase function validates, loads, builds, and returns the updated portfolio. The CLI handler is responsible for calling `SaveConfig`. This keeps the usecase pure (no side-effects) and testable without mocking the filesystem.

## Risks / Trade-offs

- [`IFileSystem` migration of `readConfig`/`writeConfig` breaks existing tests] → Migrate first, run full test suite before adding new functionality; the change is mechanical.
- [JSON round-trip silently drops `defaultProfile` or existing profiles] → A round-trip acceptance test (`writeConfig` then `readConfig`) is added as an early guard before wiring up `addProfile`.
- [`Profiles` DU case collides with something in Argu internals] → Use `CliPrefix(CliPrefix.None)` on the `Profiles` case, consistent with `Backlog`; confirmed safe by Argu disambiguation rules.
