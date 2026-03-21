# Plan: profile-add

**Status:** Ready  
**Backlog item:** `profile-add`  
**Repo scope:** `itr`

---

## Description

Add a `profiles add` CLI command that inserts a new named profile into `itr.json`. A profile may include a git identity (name required, email optional). Products registered later under that profile are tracked by product root directory path, and product structure is loaded from `product.yaml`. Duplicate profile names must be rejected with a clear error.

---

## Context

`itr.json` is already bootstrapped by `settings-bootstrap` (dependency satisfied). `bootstrapIfMissing` uses `IFileSystem`; `readConfig`/`writeConfig` in `PortfolioAdapter.fs` currently use `System.IO` directly - both will be migrated to use `IFileSystem`.

The `PortfolioAdapter` handles JSON serialization/deserialization of `PortfolioDto`/`ProfileDto`. `Portfolio.tryCreate` already enforces duplicate profile name rejection via `DuplicateProfileName`. `GitIdentity` currently has `Email: string option`; since git identity is only used when committing, `Name` is required but `Email` stays optional.

`CliArgs` currently has `Profile of string` as a top-level `-p`/`--profile` flag for selecting the active profile. Using `profiles` (plural) as the management subcommand avoids any name collision and reads naturally: `itr profiles add <name>` vs `itr -p work backlog take <id>`.

---

## Scope

### 1. Domain (`Itr.Domain` / `Domain.fs`)

- `GitIdentity` - no change needed. `Name: string` is already required; `Email: string option` is correct.
- Add `ProfileName` validation: `ProfileName.tryCreate` should reject blank/whitespace names and enforce a slug rule (same pattern as `ProductId`: `[a-z0-9][a-z0-9-]*`). Add `InvalidProfileName of value: string * rules: string` to `PortfolioError`.
- `Portfolio.tryCreate` - no change needed; duplicate detection already works.

### 2. Interfaces (`Itr.Domain` / `Interfaces.fs`)

Extend `IPortfolioConfig` with a `SaveConfig` member so load and save are co-located:

```fsharp
type IPortfolioConfig =
    abstract ConfigPath: unit -> string
    abstract LoadConfig: path: string -> Result<Portfolio, PortfolioError>
    abstract SaveConfig: path: string -> portfolio: Portfolio -> Result<unit, PortfolioError>
```

This avoids a separate `IPortfolioStore` interface and keeps `AppDeps` simple.

### 3. Adapter (`Itr.Adapters` / `PortfolioAdapter.fs`)

- Migrate `readConfig` and `writeConfig` to accept `IFileSystem` instead of calling `System.IO` directly. `File.ReadAllText`, `File.WriteAllText`, `File.Exists`, `Directory.CreateDirectory` are replaced with `IFileSystem` calls.
- `PortfolioConfigAdapter` is constructed with both `IEnvironment` and `IFileSystem` and implements the updated `IPortfolioConfig` including `SaveConfig`.
- `writeConfig` return type stays `Result<unit, PortfolioError>` (currently using `ConfigParseError` for write failures; keep as-is since there is no write-specific error case in `PortfolioError`).

### 4. Feature usecase (`Itr.Features` / `PortfolioUsecase.fs`)

Add:

```fsharp
let addProfile
    (profileName: string)
    (gitIdentity: GitIdentity option)
    (setAsDefault: bool)
    (configPath: string)
    : EffectResult<#IPortfolioConfig, Portfolio, PortfolioError>
```

Steps:
1. Validate `profileName` via `ProfileName.tryCreate` (returns `InvalidProfileName` on failure).
2. Load current portfolio via `IPortfolioConfig.LoadConfig`.
3. Build new `Profile` record.
4. Determine `defaultProfile`: if `setAsDefault` is true use new name, else preserve existing value.
5. Rebuild portfolio with `Portfolio.tryCreate` (existing profiles + new one); `DuplicateProfileName` surfaces here.
6. Return updated `Portfolio`; caller is responsible for persisting via `SaveConfig`.

Signature returns `EffectResult<#IPortfolioConfig, Portfolio, PortfolioError>`.

### 5. CLI entry point (`Itr.Cli` / `Program.fs`)

#### Argu DU design

`CliArgs` keeps `Profile of string` as the top-level `-p`/`--profile` flag unchanged. The management subcommand uses `Profiles` (plural) to avoid any ambiguity:

```fsharp
[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfilesAddArgs =
    | [<MainCommand; Mandatory>] Name of name: string
    | Git_Name of git_name: string
    | Git_Email of git_email: string
    | Set_Default

[<CliPrefix(CliPrefix.None)>]
type ProfilesArgs =
    | Add of ParseResults<ProfilesAddArgs>

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string              // unchanged
    | Output of string                                        // unchanged
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>   // unchanged
    | [<CliPrefix(CliPrefix.None)>] Profiles of ParseResults<ProfilesArgs>
```

CLI surface:

```
itr profiles add <name> [--git-name <name>] [--git-email <email>] [--set-default]
```

#### Handler

1. Bootstrap config if missing (same as existing dispatch).
2. Parse `Name`, optional `Git_Name`/`Git_Email`, `Set_Default`.
3. Validate: if `--git-email` is present but `--git-name` is absent, return a clear error before calling the usecase.
4. Call `Portfolio.addProfile` effect.
5. On success, persist via `(deps :> IPortfolioConfig).SaveConfig configPath updatedPortfolio`.
6. Print `"Added profile '<name>'."` or JSON `{"ok": true, "profile": "<name>"}`.
7. On error, format with dedicated `formatPortfolioError` cases.

#### Error formatting

Update `formatPortfolioError` to handle the new cases explicitly:

```fsharp
| DuplicateProfileName name -> $"Profile '{name}' already exists."
| InvalidProfileName(value, rules) -> $"Invalid profile name '{value}': {rules}"
```

### 6. `AppDeps` (`Program.fs`)

`PortfolioConfigAdapter` now requires both `IEnvironment` and `IFileSystem`. Update construction:

```fsharp
let portfolioConfigAdapter = PortfolioAdapter.PortfolioConfigAdapter(envAdapter, fsAdapter)
```

Delegate `SaveConfig` through `AppDeps` the same way `LoadConfig` is delegated today.

---

## Dependencies / Prerequisites

- `settings-bootstrap` complete (satisfied).
- `IFileSystem` migration of `readConfig`/`writeConfig` must land before the usecase is wired up.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `Domain.fs` | Add `ProfileName.tryCreate`, add `InvalidProfileName` to `PortfolioError` |
| `Interfaces.fs` | Add `SaveConfig` to `IPortfolioConfig` |
| `PortfolioAdapter.fs` | Migrate `readConfig`/`writeConfig` to `IFileSystem`; `PortfolioConfigAdapter` accepts `IFileSystem`; expose `SaveConfig` |
| `PortfolioUsecase.fs` | Add `addProfile` usecase |
| `Program.fs` | Add `ProfilesAddArgs`, `ProfilesArgs`, `Profiles` DUs; update `AppDeps`; add handler; update `formatPortfolioError` |
| `PortfolioAcceptanceTests.fs` | Update `TestDeps` to pass `IFileSystem` to adapter; add `addProfile` acceptance tests |
| `PortfolioDomainTests.fs` | Add `addProfile` unit tests; add `ProfileName.tryCreate` validation tests |

Existing `resolveActiveProfile`, `resolveProduct`, `loadPortfolio`, and `bootstrapIfMissing` are not modified.

---

## Acceptance Criteria

- `itr profiles add <name>` writes the new profile to `itr.json`.
- `--set-default` sets `defaultProfile` to the new profile name in `itr.json`.
- `--git-name` and `--git-email` persist a `gitIdentity` on the profile.
- `--git-email` without `--git-name` returns a clear validation error.
- Re-running the same command with the same name returns `Profile '<name>' already exists.`
- Invalid profile names (blank, uppercase, spaces) are rejected with a clear error.
- Existing profiles in `itr.json` are unchanged after adding a new one (round-trip lossless).
- The new profile can be selected via `-p <name>` in subsequent commands.
- Running `itr profiles add` with missing required args prints usage help.

---

## Testing Strategy

### Domain/unit tests (`tests/communication/PortfolioDomainTests.fs`)

- `ProfileName.tryCreate` accepts valid slugs.
- `ProfileName.tryCreate` rejects blank, uppercase, and space-containing names.

### Usecase/integration tests (`tests/communication/PortfolioDomainTests.fs`)

- `addProfile` with a new name returns updated `Portfolio` containing the new profile.
- `addProfile` with a duplicate name returns `DuplicateProfileName` error.
- `addProfile` with `setAsDefault = true` updates `DefaultProfile`.
- `addProfile` with an invalid name returns `InvalidProfileName` error.

### Acceptance tests (`tests/acceptance/PortfolioAcceptanceTests.fs`)

- `writeConfig`/`readConfig` round-trip preserves `defaultProfile` and all profiles (write first, then add this as an early guard before implementing `addProfile`).
- End-to-end: write `itr.json` with one profile, call `addProfile`, read back file, assert second profile is present.
- Round-trip: existing profiles are not altered by the add.
- Duplicate rejected: second add with same name returns `DuplicateProfileName`; file unchanged.

---

## Risks / Challenges

| Risk | Mitigation |
|---|---|
| `IFileSystem` migration of `readConfig`/`writeConfig` breaks existing tests | Migrate first, run tests before adding new functionality |
| `CustomCommandLine "profile"` conflicts with `Profile of string` option in Argu | Use `Profiles` (plural) as the DU case name; no `CustomCommandLine` override needed |
| JSON serialization loses `defaultProfile` or existing entries | Round-trip acceptance test added before `addProfile` is wired up |
