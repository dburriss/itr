# Plan: profile-add

**Status:** Draft  
**Backlog item:** `profile-add`  
**Repo scope:** `itr`

---

## Description

Add a `profile add` CLI command that inserts a new named profile into `itr.json`. A profile may include an optional git identity. Products registered later under that profile persist their own coordination root config (`mode` plus `dir` or `repoDir`). Duplicate profile names must be rejected with a clear error.

---

## Context

`itr.json` is already bootstrapped by `settings-bootstrap` (dependency satisfied). The file is read and written via `IFileSystem`. The `PortfolioAdapter` already handles JSON serialization/deserialization of the `PortfolioDto` shape (including `ProfileDto` with `gitIdentity`). The `Portfolio.tryCreate` domain function already enforces duplicate profile name rejection.

---

## Scope

### 1. Domain (`Itr.Domain` / `Domain.fs`)

No new domain types required. The existing `Profile`, `GitIdentity`, `CoordinationRootConfig`, and `PortfolioError.DuplicateProfileName` cover the model. No changes needed.

### 2. Feature usecase (`Itr.Features` / `Portfolio` module)

Add a new function `addProfile` to `PortfolioUsecase.fs`:

- Accepts: `profileName: string`, `gitIdentity: GitIdentity option`, `setAsDefault: bool`
- Reads current `itr.json` via `IPortfolioConfig.LoadConfig`
- Constructs the new `Profile` record
- Determines `defaultProfile`: if `setAsDefault` is true use the new name, otherwise preserve existing value
- Calls `Portfolio.tryCreate` with the existing profiles + new one to validate (catches `DuplicateProfileName`)
- Returns the updated `Portfolio` on success; entry point is responsible for persisting

Signature: `addProfile ... : EffectResult<#IPortfolioConfig, Portfolio, PortfolioError>`

### 3. Adapter (`Itr.Adapters` / `PortfolioAdapter.fs`)

Add a `saveConfig` function (or extend adapter) that serializes a `Portfolio` back to JSON and writes it to the config path via `IFileSystem`. This is the inverse of `readConfig`.

Add a capability interface `IPortfolioStore` (or extend `IPortfolioConfig`) in `Interfaces.fs`:

```fsharp
type IPortfolioStore =
    inherit IPortfolioConfig
    abstract SaveConfig: path: string -> portfolio: Portfolio -> Result<unit, PortfolioError>
```

Or alternatively, accept a raw `IFileSystem` write at the entry point - prefer the interface approach for testability.

### 4. CLI entry point (`Itr.Cli` / `Program.fs`)

Add new Argu argument DUs for a `profile add` subcommand:

```
itr profile add <name> [--git-name <name>] [--git-email <email>] [--default]
```

- `--default` sets the new profile as the portfolio's `defaultProfile`.

Wire the handler:
1. Bootstrap config if missing.
2. Call `Portfolio.addProfile` effect.
3. Serialize updated portfolio and write to config path.
4. Print confirmation or JSON output.

### 5. `AppDeps` (`Program.fs`)

Implement `IPortfolioStore` (or the save capability) in `AppDeps`.

---

## Dependencies / Prerequisites

- `settings-bootstrap` must be complete (it is; `bootstrapIfMissing` exists and is wired).
- `itr.json` serialization round-trip must be lossless - verify `saveConfig` preserves existing profiles.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `Interfaces.fs` | Add `IPortfolioStore` or extend `IPortfolioConfig` with save |
| `PortfolioAdapter.fs` | Add `saveConfig` function + adapter method |
| `PortfolioUsecase.fs` | Add `addProfile` usecase function |
| `Program.fs` | Add `ProfileArgs` / `ProfileAddArgs` Argu DUs, `AppDeps` extension, handler |

No existing usecases or domain functions are modified.

---

## Acceptance Criteria

- `itr profile add <name>` writes the new profile to `itr.json`.
- `--default` sets `defaultProfile` to the new profile name in `itr.json`.
- Profile includes optional `gitIdentity` fields.
- Re-running the same command with the same name returns a clear error (e.g. `Profile 'work' already exists`).
- The new profile can be selected via `--profile <name>` in subsequent commands.
- Running `itr profile add` with missing required args prints usage help.

---

## Testing Strategy

### Communication tests (`tests/communication/PortfolioDomainTests.fs`)

- `addProfile` with a new name returns updated `Portfolio` containing the new profile.
- `addProfile` with a duplicate name returns `DuplicateProfileName` error.

### Acceptance tests (`tests/acceptance/PortfolioAcceptanceTests.fs`)

- End-to-end: write `itr.json` with one profile, call `addProfile`, read back file, assert second profile is present.
- Round-trip: existing profiles are not altered by the add.
- Duplicate rejected: second add with same name returns error; file unchanged.

---

## Risks / Challenges

| Risk | Mitigation |
|---|---|
| JSON serialization loses `defaultProfile` or existing entries | Write a round-trip test before implementing `saveConfig` |
| CLI optional args drift from the persisted JSON shape | Keep docs and tests aligned to `gitIdentity` and per-profile product registration |

---

## Open Questions

~~1. Should `profile add` also accept `--set-default` to mark the new profile as the portfolio default?~~ **Resolved: yes, via `--default` flag.**
