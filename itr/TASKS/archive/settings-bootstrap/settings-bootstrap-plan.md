# Plan: settings-bootstrap

**Status:** In Progress

---

## Description

When a user runs any `itr` command on a clean machine, the tool currently returns a raw F# `ConfigNotFound` error and exits with code 1. This task fixes that by auto-creating a minimal default `itr.json` at the resolved global config path when the file is absent, then continuing normally. The operation is idempotent and surfaces a structured error if the write fails.

This task also renames the config file from `portfolio.json` → `itr.json` throughout the codebase to match `docs/config-files.md` and the backlog constraint.

Local config override is out of scope — tracked separately as a future backlog item.

---

## Scope

### 1. Rename `portfolio.json` → `itr.json`

- `PortfolioConfigAdapter.ConfigPath()` in `src/adapters/PortfolioAdapter.fs` — update the hardcoded filename string.
- All test fixtures and acceptance test helpers that reference `portfolio.json` by string.

### 2. New error case in `Domain.fs`

Add to the `PortfolioError` DU:

```fsharp
| BootstrapWriteError of path: string * message: string
```

### 3. New `bootstrapIfMissing` function in `Itr.Features.Portfolio`

```fsharp
val bootstrapIfMissing :
    configPath: string -> EffectResult<#IFileSystem, unit, PortfolioError>
```

Behaviour:

- If `IFileSystem.FileExists configPath` → `Ok ()` immediately (idempotent, no message).
- If absent:
  - Ensure parent directory exists — `IFileSystem` adapter must create it if needed.
  - Write minimal default content (see below).
  - Return `Ok ()` on success; map `IoError` → `BootstrapWriteError` on failure.

Default `itr.json` content:

```json
{
  "defaultProfile": null,
  "profiles": {}
}
```

### 4. Update `FileSystemAdapter.WriteFile` in `src/adapters/Library.fs`

Ensure `Directory.CreateDirectory(Path.GetDirectoryName(path))` is called before writing, so `~/.config/itr/` is created automatically if it does not exist.

### 5. Wire into `Itr.Cli.Program.dispatch`

Before calling `loadPortfolio`, resolve the config path and call `bootstrapIfMissing`:

```
let configPath = (deps :> IPortfolioConfig).ConfigPath()
bootstrapIfMissing configPath |> Effect.run deps
  |> Result.bind (fun bootstrapped ->
      if bootstrapped then printfn "No itr.json found. Created a default at %s. Run 'itr init' to configure profiles and products." configPath
      loadPortfolio ...)
```

Only print the informational message when a file is actually created (not on every run).

### 6. Extend error formatting in `Itr.Cli.Program`

```fsharp
| BootstrapWriteError(path, msg) -> $"Could not create itr.json at {path}: {msg}"
```

---

## Dependencies / Prerequisites

- No open tasks block this one.
- The rename must be applied atomically with the adapter change — tests will fail to compile otherwise.

---

## Impact on Existing Code

| File | Change |
|---|---|
| `src/domain/Domain.fs` | Add `BootstrapWriteError` to `PortfolioError` DU |
| `src/features/Portfolio/PortfolioUsecase.fs` | Add `bootstrapIfMissing` |
| `src/adapters/PortfolioAdapter.fs` | `portfolio.json` → `itr.json` in `ConfigPath()` |
| `src/adapters/Library.fs` | Ensure `WriteFile` creates parent directories |
| `src/cli/Program.fs` | Wire bootstrap call; extend error formatting |
| `tests/acceptance/PortfolioAcceptanceTests.fs` | Update `portfolio.json` fixture strings to `itr.json` |

---

## Acceptance Criteria

- [ ] Commands that require settings create a default `itr.json` if it is missing.
- [ ] Default file is written to the resolved global config path (`$ITR_HOME/itr.json` or `~/.config/itr/itr.json`).
- [ ] Parent directory (`~/.config/itr/`) is created if it does not exist.
- [ ] Bootstrap is idempotent: running twice does not overwrite an existing file.
- [ ] A structured `BootstrapWriteError` is returned and formatted when the write fails.
- [ ] An informational message mentioning `itr init` is printed only when a new file is created.
- [ ] No remaining `portfolio.json` references in production code.
- [ ] All existing tests pass after the rename.

---

## Testing Strategy

### New acceptance tests (new file or extend `PortfolioAcceptanceTests.fs`)

1. **Bootstrap creates file and parent dir when absent** — point config path at a non-existent nested path; run `bootstrapIfMissing`; assert file exists with valid JSON and directory was created.
2. **Bootstrap is idempotent** — write a custom `itr.json`; run `bootstrapIfMissing`; assert content is unchanged.
3. **Bootstrap returns `BootstrapWriteError` on write failure** — point at an unwritable path (e.g. a path inside a non-existent root with no create permissions); assert correct error case.

### Updated existing tests

- Rename `portfolio.json` → `itr.json` in all fixture strings in `PortfolioAcceptanceTests.fs`.
- Run `dotnet test` to confirm no regressions.

### Communication tests (optional)

- "Bootstrap does not overwrite an existing itr.json."
- "Bootstrap write failure returns BootstrapWriteError."

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Existing users have `portfolio.json` on disk | Document the rename; the directory stays the same, only filename changes |
| `bootstrapIfMissing` return type change needed | Function returns `bool` (was file created?) or two-case DU to allow conditional message printing — decide during implementation |
| Double `ConfigPath()` call (once for bootstrap, once for `loadPortfolio`) | Resolve once at the top of `dispatch` and pass down |

---

## Open Questions

- Should `bootstrapIfMissing` return `bool` (created or not) or `unit` with the caller inferring from a separate `FileExists` check? A `bool` return is simpler and avoids an extra IO call.
- Local `itr.json` override (project-level config that merges with or overrides the global file) is **explicitly out of scope** — tracked as a future backlog item `local-config-override`.
