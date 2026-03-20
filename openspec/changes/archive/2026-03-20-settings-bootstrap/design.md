## Context

The `itr` CLI tool currently crashes with a raw F# `ConfigNotFound` error when no config file exists at the resolved global config path. This happens because `loadPortfolio` is called unconditionally before any check. The config file is also inconsistently named `portfolio.json` in code while documentation refers to `itr.json`.

The project uses an effect-system pattern (`EffectResult`) with a testable `IFileSystem` abstraction (`Testably.Abstractions`), which means file I/O is fully mockable in tests.

## Goals / Non-Goals

**Goals:**
- Auto-create a minimal default `itr.json` on first run if the file is absent
- Rename `portfolio.json` → `itr.json` in the adapter and all tests
- Ensure parent directory is created if absent
- Return a structured `BootstrapWriteError` on write failure
- Print a one-time informational message when a file is first created

**Non-Goals:**
- Local (project-level) config override — tracked as a future backlog item `local-config-override`
- Migration of existing `portfolio.json` files on disk
- Changes to the config schema or profile resolution logic

## Decisions

### Decision: `bootstrapIfMissing` returns `bool` (was file created?)

**Rationale**: Returning `bool` from `bootstrapIfMissing` lets the caller decide whether to print the informational message without a second `FileExists` round-trip. Alternatives:
- Return `unit` and have caller call `FileExists` again — extra IO, more complex.
- Two-case DU `BootstrapResult` — more expressive but over-engineered for a binary outcome.

**Chosen**: `bool` return. `true` = file was created; `false` = file already existed.

### Decision: `bootstrapIfMissing` called once; `configPath` resolved once at top of `dispatch`

**Rationale**: Avoids a double `ConfigPath()` call (once for bootstrap, once for `loadPortfolio`). Resolve the path once and thread it through.

### Decision: `WriteFile` in `FileSystemAdapter` ensures parent directory creation

**Rationale**: The adapter is the right layer to handle OS-level concerns like directory creation. Features should not need to know about directory structure. This also means `bootstrapIfMissing` doesn't need to call a separate `CreateDirectory` effect — it just calls `WriteFile`.

### Decision: Rename is applied atomically with adapter change

**Rationale**: Tests reference the filename string directly; a partial rename would cause compile failures. The rename must land in a single commit/PR with the adapter change.

## Risks / Trade-offs

- **Existing users have `portfolio.json` on disk** → The directory stays the same; only the filename changes. On next run, bootstrap will create `itr.json` alongside the old file. Users must migrate manually or use `itr init`. Document the rename in release notes.
- **`bootstrapIfMissing` bool return** → If future requirements need richer bootstrapping state (e.g., version tracking), bool will be too narrow. Acceptable risk for now; can be changed to a DU later.
- **Directory creation in `WriteFile`** → Creating directories as a side effect of every write is a small behavioral change to `FileSystemAdapter`. Could cause unexpected directory creation in edge cases, but aligns with common adapter behaviour (e.g., `File.WriteAllText` wrapped with `Directory.CreateDirectory`).
