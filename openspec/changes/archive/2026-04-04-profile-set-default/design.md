## Context

The portfolio config (`itr.json`) already supports a `defaultProfile` field, and `profile add --set-default` can set it at creation time. There is no standalone command to update `defaultProfile` for an existing profile. Users must either re-create the profile or hand-edit `itr.json`.

Config can live in two locations:
- **Global**: `~/.config/itr/itr.json` (or `$ITR_HOME/itr.json`)
- **Local**: `<productRoot>/itr.json` — read by `LoadLocalConfig`; written by the `--local` path in this change

The existing `ProfileNotFound` error case in `formatPortfolioError` falls through to the debug catch-all (`%A{other}`), which is a pre-existing defect that should be fixed as part of this work.

## Goals / Non-Goals

**Goals:**
- Add `itr profile set-default <name>` subcommand
- Support `--local` (product-root `itr.json`) and `--global` (home-dir `itr.json`) flags
- Auto-detect the right config when no flag given (local exists → local, else global)
- Create `<productRoot>/itr.json` when `--local` is specified and it doesn't exist yet (merge, don't overwrite)
- Add `setDefaultProfile` usecase function following the existing `addProfile` pattern
- Fix `ProfileNotFound` in `formatPortfolioError`
- Add unit and acceptance tests

**Non-Goals:**
- Creating new profiles
- Shell rc file integration
- `ITR_PROFILE` env-var write-back (child processes cannot set parent-shell env)

## Decisions

### 1. Usecase signature — returns updated Portfolio, caller saves

All existing usecases (`addProfile`, `registerProduct`) return the updated `Portfolio` and leave saving to the CLI handler. `setDefaultProfile` will follow the same pattern:

```fsharp
type SetDefaultProfileInput = { Name: string }

let setDefaultProfile<'deps when 'deps :> IPortfolioConfig>
    (configPath: string)
    (input: SetDefaultProfileInput)
    : EffectResult<'deps, Portfolio, PortfolioError>
```

Alternatives considered:
- *Save inside the usecase*: Rejected — inconsistent with every other usecase; caller would have no chance to inspect the updated value before commit.

### 2. Auto-detection logic (no flag)

When neither `--local` nor `--global` is specified:
1. Attempt to resolve product root (same pattern used by other commands)
2. If a local `itr.json` exists at that root → update it
3. Otherwise → update the global config

This mirrors the read-time precedence described in `profile-resolution` spec.

Alternative considered:
- *Always default to global when no flag*: Rejected — if the user is already inside a product directory whose local config is the effective one, silently writing to global would be confusing.

### 3. --local requires a resolvable product context

If `--local` is specified but no product root can be resolved, error with:
`"--local flag requires a product context. Run this command from within a product directory or specify --global instead."`

### 4. Profile lookup is case-insensitive

Reuse `Portfolio.tryFindProfileCaseInsensitive`. If not found, return `ProfileNotFound`.

### 5. Output message includes config file path

- `--local` or auto→local: `"Profile '{name}' set as default. (<productRoot>/itr.json)"`
- `--global` or auto→global: `"Profile '{name}' set as default. (~/.config/itr/itr.json)"`

### 6. Local itr.json creation

When `--local` is specified and no local `itr.json` exists, create a minimal one containing only `defaultProfile`. If one already exists with `agent` config, merge by reading-then-writing the `DefaultProfile` field only (using existing `readConfig`/`writeConfig` helpers from `PortfolioAdapter.fs`).

## Risks / Trade-offs

- **Local itr.json content collision**: A product may already have an `itr.json` with `agent` config written by `task plan --ai`. Mitigation: read-then-update (set `DefaultProfile` on existing `Portfolio` value) rather than overwrite.
- **Product root resolution varies by command**: Some commands take `--profile` to select a profile first, then find a product inside it. `set-default` does NOT require a profile to be active — it only needs the product root for `--local` path. Mitigation: call `(deps :> IProductConfig).LoadProductConfig` only for local-path resolution, not profile resolution.
