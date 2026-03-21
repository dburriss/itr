# Plan: product-init

**Status:** Draft  
**Backlog item:** `product-init`  
**Repo scope:** `itr`

---

## Description

Add an `itr product init` CLI command that interactively scaffolds a new product on disk: writes `product.yaml`, creates the coordination directory, and writes starter `PRODUCT.md` and `ARCHITECTURE.md` files. After scaffolding, the command optionally registers the new product root in `itr.json` under the active or a named profile.

---

## Context

`product-register` (a dependency) adds an existing product to `itr.json` via `itr product register <path>`. This task builds on top of that: `product-init` creates the product files first and then delegates registration to the same `registerProduct` usecase. The `ProductArgs` / `ProductRegisterArgs` Argu DUs introduced by `product-register` are extended here with an `Init` subcommand.

All other dependencies (`settings-bootstrap`, `profile-add`, `profile-selection`) are already satisfied.

---

## Scope

### 1. Feature usecase (`Itr.Features` / `PortfolioUsecase.fs`)

Add:

```fsharp
type InitProductInput =
    { Path: string
      Id: string
      RepoId: string
      CoordinationMode: string   // "primary-repo" | "standalone" | "control-repo"; default: "primary-repo"
      CoordPath: string          // default: ".itr"
      RegisterProfile: string option  // None = skip registration
      SetAsDefault: bool }

let initProduct
    (configPath: string)
    (input: InitProductInput)
    : EffectResult<#IPortfolioConfig & #IProductConfig & #IFileSystem, Portfolio option, PortfolioError>
```

Steps:
1. Validate `input.Id` via `ProductId.tryCreate`; return `InvalidProductId` on failure.
2. Validate `input.Path` is non-empty; return `ProductConfigError` if the directory does not exist via `IFileSystem.DirectoryExists`.
3. Build `product.yaml` content and write to `<path>/product.yaml` via `IFileSystem.WriteFile` (fail if the file already exists to avoid overwriting).
4. Create coordination directory `<path>/<coordPath>/` (adapter's `WriteFile` already creates parent dirs; write a `.gitkeep` or `BACKLOG/` sentinel if needed).
5. Write starter `PRODUCT.md` and `ARCHITECTURE.md` at `<path>/`.
6. If `input.RegisterProfile` is `Some profile` (or non-skip): delegate to `registerProduct` logic (re-use or inline) to append the new root to `itr.json`. Return the updated `Portfolio`.
7. If registration is skipped, return `None`.

### 2. Starter document content

`PRODUCT.md` template (written verbatim):

```markdown
# Product: <id>

## Purpose

TODO: describe what this product does.
```

`ARCHITECTURE.md` template:

```markdown
# Architecture: <id>

## Technology Stack

TODO: list languages, frameworks, and tools.
```

Keep templates minimal — users fill them in.

### 3. `product.yaml` scaffolding

Generated content based on inputs:

```yaml
id: <id>

docs:
  product: PRODUCT.md
  architecture: ARCHITECTURE.md

repos:
  <repoId>:
    path: .

coordination:
  mode: primary-repo
  repo: <repoId>
  path: <coordPath>
```

For `standalone` mode, `coordination.repo` is omitted.

### 4. CLI entry point (`Itr.Cli` / `Program.fs`)

Extend `ProductArgs` (introduced by `product-register`) with an `Init` subcommand:

```fsharp
[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductInitArgs =
    | [<MainCommand; Mandatory>] Path of path: string
    | [<MainCommand>] Id of id: string
    | Repo_Id of repo_id: string
    | Coord_Mode of coord_mode: string   // default: "primary-repo"
    | Coord_Path of coord_path: string   // default: ".itr"
    | [<AltCommandLine("--no-register")>] Skip_Register
    | Register_Profile of profile: string

type ProductArgs =
    | Register of ParseResults<ProductRegisterArgs>   // existing
    | Init of ParseResults<ProductInitArgs>            // new
```

Interactive prompts (Spectre.Console `AnsiConsole.Ask`):
- If `id` positional is absent: prompt "Product id:".
- If `--repo-id` is absent: prompt "Repo id (default: same as id):".
- After scaffolding: prompt "Register in which profile? (leave blank to skip):" — unless `--skip-register` or `--register-profile` are provided.

CLI surface:

```
itr product init <path> <id> [--repo-id <id>] [--coord-mode <mode>]
                              [--coord-path <path>] [--register-profile <name>]
                              [--no-register]
itr -p <profile> product init <path> <id>
```

Handler:
1. Collect required inputs (prompt if absent).
2. Determine registration target: `--register-profile` → use that profile; `--no-register` → skip; neither → prompt.
3. Run `Portfolio.initProduct configPath input` effect.
4. On success, print `"Initialized product '<id>' at <path>."` and, if registered, `"Registered in profile '<profile>'."`.
5. On error, format with `formatPortfolioError`.

### 5. Error formatting (`Program.fs`)

Ensure `formatPortfolioError` covers:

```fsharp
| InvalidProductId -> "Invalid product id: must match [a-z0-9][a-z0-9-]*"
| ProductConfigError(root, msg) -> $"Cannot load/write product at '{root}': {msg}"
```

(These should already exist from `product-register`; verify coverage.)

---

## Dependencies / Prerequisites

| Dependency | Status |
|---|---|
| `settings-bootstrap` | Satisfied |
| `profile-add` | Satisfied |
| `profile-selection` | Satisfied |
| `product-register` | **Must land first** — provides `ProductArgs`, `registerProduct` usecase, and `formatPortfolioError` cases this plan reuses |

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/features/Portfolio/PortfolioUsecase.fs` | Add `InitProductInput` type and `initProduct` usecase |
| `src/cli/Program.fs` | Extend `ProductArgs` with `Init`; add `ProductInitArgs` DU; add handler with interactive prompts |
| `tests/communication/PortfolioDomainTests.fs` | Add `initProduct` usecase unit tests |
| `tests/acceptance/PortfolioAcceptanceTests.fs` | Add end-to-end acceptance tests for the init command |

No changes to `Domain.fs`, `Interfaces.fs`, or adapters (file creation uses existing `IFileSystem.WriteFile`).

---

## Acceptance Criteria

- `itr product init <path>` creates `product.yaml`, `PRODUCT.md`, `ARCHITECTURE.md`, and the coordination directory at `<path>`.
- Prompts for missing `--id` and `--repo-id` when running interactively.
- After scaffolding, prompts whether to register the product root in a profile (skippable via `--no-register`).
- `--register-profile <name>` skips the registration prompt and targets the named profile directly.
- Running init on a path that already contains a `product.yaml` returns a clear error and leaves existing files unchanged.
- Running init on a non-existent directory returns a clear error.
- Starter `PRODUCT.md` and `ARCHITECTURE.md` contain a minimal template with the product id.
- The initialized product can subsequently be resolved via `itr backlog take`.

---

## Testing Strategy

### Communication tests (`tests/communication/PortfolioDomainTests.fs`)

- `initProduct` with valid inputs writes the expected files and returns `Some updatedPortfolio` when registration is requested.
- `initProduct` with `RegisterProfile = None` writes files and returns `None`.
- `initProduct` when `product.yaml` already exists returns `ProductConfigError`; no files written.
- `initProduct` when path directory does not exist returns `ProductConfigError`.
- `initProduct` with an invalid id returns `InvalidProductId`.

### Acceptance tests (`tests/acceptance/PortfolioAcceptanceTests.fs`)

- End-to-end: call `initProduct`, verify all expected files exist on disk with correct content, verify `itr.json` updated when registration requested.
- Skip registration: verify `itr.json` unchanged when `RegisterProfile = None`.
- Duplicate id: init a second product with the same id in the same profile returns `DuplicateProductId`; `itr.json` unchanged.

### Verification

- `dotnet build`
- `dotnet test`

---

## Risks / Challenges

| Risk | Mitigation |
|---|---|
| `product-register` not yet implemented | Block on it; do not attempt to implement `product-init` in parallel |
| Interactive prompts are hard to test in acceptance tests | Separate prompt logic from usecase; acceptance tests supply all inputs directly without prompting |
| Starter doc templates may drift from checked-in `PRODUCT.md`/`ARCHITECTURE.md` | Keep templates in a `Templates` module or inline constants; document they are intentionally minimal |
| `IFileSystem.WriteFile` overwrites existing files silently | Add an existence check via `IFileSystem.FileExists` before writing `product.yaml` |

---

## Resolved Decisions

1. **Default coordination mode** — `primary-repo`, with the single repo id set as `coordination.repo`. `standalone` and `control-repo` remain available via `--coord-mode`.
2. **Coordination directory contents** — init creates the coordination directory only (empty); the BACKLOG subdirectory is created on first `backlog item create` or `backlog take`.
3. **Product id input** — `id` is a positional `MainCommand` argument alongside `path`, not a named flag. Both are mandatory positionals; the CLI prompts if either is absent.
