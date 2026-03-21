# Plan: product-register

**Status:** Draft  
**Backlog item:** `product-register`  
**Repo scope:** `itr`

---

## Description

Add an `itr product register <path>` CLI command that appends an existing product root directory to the active profile's product list in `itr.json`. The product id is read from `<path>/product.yaml`; duplicate ids within the profile are rejected. No product files are created.

---

## Context

`profile-add` (dependency satisfied) established `SaveConfig` on `IPortfolioConfig` and the path-based product registration shape in `itr.json`. `product-config-ownership` (dependency satisfied) established canonical `product.yaml` loading via `IProductConfig.LoadProductConfig` and duplicate detection in `loadAllDefinitions`. The domain already models `ProductRef` as a bare `{ Root: ProductRoot }`.

The `resolveProduct` pipeline already loads and validates `product.yaml` from registered roots, so registering a path and later resolving a product are consistent by construction.

The `backlog take` handler currently takes the **first** product in the profile unconditionally; this task does not change that behavior.

---

## Scope

### 1. Feature usecase (`Itr.Features` / `PortfolioUsecase.fs`)

Add:

```fsharp
type RegisterProductInput = { Path: string; Profile: string option }

let registerProduct<'deps when 'deps :> IPortfolioConfig and 'deps :> IProductConfig and 'deps :> IFileSystem>
    (configPath: string)
    (input: RegisterProductInput)
    : EffectResult<'deps, Portfolio, PortfolioError>
```

The `IFileSystem` constraint is needed for the early directory existence check (step 3).

Steps:
1. Load portfolio via `IPortfolioConfig.LoadConfig`.
2. Resolve active profile by name (use `input.Profile` if provided; otherwise use `portfolio.DefaultProfile`).
3. Validate the path is non-empty and the directory exists via `IFileSystem.DirectoryExists`; return `ProductConfigError` if absent.
4. Load `product.yaml` from the given path via `IProductConfig.LoadProductConfig` (surfaces `ProductConfigError` on missing/invalid file).
5. Load all existing definitions for the profile via `loadAllDefinitions` to detect duplicate canonical ids (`DuplicateProductId`).
6. Append a new `ProductRef { Root = ProductRoot path }` to the profile's product list. The path is stored as supplied by the caller; `expandPath` is applied at read time by the adapter.
7. Return updated `Portfolio`; caller persists via `SaveConfig`.

### 2. CLI entry point (`Itr.Cli` / `Program.fs`)

#### Argu DU design

Add sibling subcommand under a new `Product` top-level command:

```fsharp
[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductRegisterArgs =
    | [<MainCommand; Mandatory>] Path of path: string

[<CliPrefix(CliPrefix.None)>]
type ProductArgs =
    | Register of ParseResults<ProductRegisterArgs>

type CliArgs =
    | ...
    | [<CliPrefix(CliPrefix.None)>] Product of ParseResults<ProductArgs>
```

CLI surface:

```
itr product register <path>
itr -p <profile> product register <path>
```

#### Handler

1. Resolve config path and run bootstrap.
2. Parse `Path`.
3. Call `Portfolio.registerProduct configPath { Path = path; Profile = profile }` effect.
4. On success, persist via `SaveConfig` and print `"Registered product '<id>' from '<path>'."` (id read from returned/loaded definition) or JSON `{"ok": true, "productId": "<id>", "path": "<path>"}`.
5. On error, format with `formatPortfolioError` (existing cases cover all new error codes).

### 3. Error formatting (`Program.fs`)

Add missing `formatPortfolioError` cases used by this path:

```fsharp
| ProductNotFound id -> $"Product '{id}' not found."
| CoordRootNotFound(id, path) -> $"Coordination root for '{id}' not found at: {path}"
| ProductConfigError(root, msg) -> $"Cannot load product.yaml at '{root}': {msg}"
| DuplicateProductId(profile, id) -> $"Product '{id}' is already registered in profile '{profile}'."
```

---

## Dependencies / Prerequisites

- `settings-bootstrap` — satisfied.
- `profile-add` — satisfied (`SaveConfig` and profile management in place).
- `product-config-ownership` — satisfied (path-based product shape and `IProductConfig` in place).
- `profile-selection` — satisfied (active profile resolution via flag / env / default is in place).

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/features/Portfolio/PortfolioUsecase.fs` | Add `RegisterProductInput` type and `registerProduct` usecase |
| `src/cli/Program.fs` | Add `ProductRegisterArgs`, `ProductArgs`, `Product` DUs; add handler; extend `formatPortfolioError` |
| `tests/communication/PortfolioDomainTests.fs` | Add `registerProduct` usecase unit tests |
| `tests/acceptance/PortfolioAcceptanceTests.fs` | Add end-to-end acceptance tests for the register command |

No changes to `Domain.fs`, `Interfaces.fs`, or any adapter.

---

## Acceptance Criteria

- `itr product register <path>` appends the product root to the active profile in `itr.json`.
- `-p <profile>` targets an explicit profile instead of the default.
- The product id is read from `<path>/product.yaml`; no id is accepted as a CLI argument.
- Registering a path whose `product.yaml` id already exists in the profile returns `DuplicateProductId` and leaves `itr.json` unchanged.
- Registering a path that does not exist on disk returns `ProductConfigError` and leaves `itr.json` unchanged.
- Existing profiles and products in `itr.json` are unchanged after a successful registration (round-trip lossless).
- The registered product can subsequently be resolved via `itr backlog take`.

---

## Testing Strategy

### Communication tests (`tests/communication/PortfolioDomainTests.fs`)

- `registerProduct` with a valid path adds a `ProductRef` to the active profile and returns the updated `Portfolio`.
- `registerProduct` with a duplicate canonical id returns `DuplicateProductId`; portfolio unchanged.
- `registerProduct` with a path whose directory does not exist returns `ProductConfigError`; portfolio unchanged.
- `registerProduct` with a missing `product.yaml` propagates `ProductConfigError`.
- `registerProduct` when the named profile doesn't exist returns `ProfileNotFound`.

### Acceptance tests (`tests/acceptance/PortfolioAcceptanceTests.fs`)

- End-to-end: write `itr.json` with one profile (no products), call `registerProduct`, read back file, assert product root is present.
- Duplicate registration: second call with a root sharing the same canonical id returns `DuplicateProductId`; file unchanged.
- Round-trip: existing profiles and other products are not altered by a new registration.

### Verification

- `dotnet build`
- `dotnet test`

---

## Risks / Challenges

| Risk | Mitigation |
|---|---|
| Path normalisation / `~` expansion inconsistency between write and read | Resolved: store as-is; `expandPath` applied at read time in the adapter |
| `loadAllDefinitions` does IO for every registered root on each register call | Acceptable for MVP; no optimization needed |
| `formatPortfolioError` catch-all `%A{other}` may mask new error cases | Enumerate all new cases explicitly before wiring the handler |

---

## Resolved Decisions

1. **Path storage** — store the path as supplied by the caller. `expandPath` (tilde + env var expansion) is applied at read time by the adapter, consistent with existing convention.
2. **Directory existence check** — reject early in the usecase: if the supplied path does not exist on disk, return `ProductConfigError` before attempting to load `product.yaml`. This keeps `itr.json` consistent and surfaces the error at the point of registration.
