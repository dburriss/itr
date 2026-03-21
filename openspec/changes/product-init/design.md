## Context

The `itr` CLI supports managing a portfolio of products via `itr.json`. Products are registered as root paths in a profile. Today, `itr product register <path>` can add an existing product root to a profile, but there is no command to scaffold a new product from nothing. Users must manually create `product.yaml`, docs, and the coordination directory and know the required layout.

The `product-register` task introduces `ProductArgs`, `registerProduct`, and `formatPortfolioError` in `PortfolioUsecase.fs` and `Program.fs`. This change extends those surfaces.

## Goals / Non-Goals

**Goals:**
- Add `itr product init <path> [id]` that creates `product.yaml`, `PRODUCT.md`, `ARCHITECTURE.md`, and the coordination directory.
- Delegate post-scaffold registration to the existing `registerProduct` usecase.
- Provide interactive prompts for missing required inputs (`id`, `repo-id`, registration profile).
- Guard against overwriting an existing `product.yaml`.

**Non-Goals:**
- Populating BACKLOG or any backlog structure (handled on first `backlog take`).
- Validating git repo presence at the target path.
- Supporting multi-repo products at init time (single repo only; more repos can be added manually).

## Decisions

### Decision 1: Re-use `registerProduct` for registration

**Chosen**: Call the existing `registerProduct` logic directly after writing files, rather than duplicating portfolio update logic.

**Why**: Keeps a single source of truth for portfolio mutation. `registerProduct` already handles config load, duplicate detection, and save.

**Alternative considered**: Inline portfolio mutation in `initProduct`. Rejected — duplication risk and drift.

### Decision 2: `initProduct` returns `Portfolio option`

**Chosen**: Returns `Some updatedPortfolio` when registration occurs, `None` when skipped.

**Why**: Caller (CLI handler) needs to know if registration happened to print the confirmation message. Returning `unit` or an enriched type both had worse ergonomics.

### Decision 3: Existence guard on `product.yaml` only

**Chosen**: Check `IFileSystem.FileExists` for `product.yaml` before writing; fail with `ProductConfigError` if present.

**Why**: Prevents silent overwrite of an existing product. `PRODUCT.md` and `ARCHITECTURE.md` are starter docs — less critical — but the same guard is applied for consistency. If any of the three target files already exist, return an error before writing any file.

**Alternative considered**: Only guard `product.yaml`. Rejected — partial scaffold state is confusing.

### Decision 4: Coordination directory via `.gitkeep`

**Chosen**: Create `<coordPath>/.gitkeep` to ensure the directory exists without adding meaningful content.

**Why**: The coordination directory needs to be present but empty at init time. Writing `.gitkeep` through `IFileSystem.WriteFile` ensures parent-dir creation with no adapter changes needed.

## Risks / Trade-offs

- **`product-register` must land first** → `initProduct` depends on `registerProduct` being available. Block implementation until that task is done.
- **Interactive prompts are untestable in acceptance tests** → Separate prompt logic from usecase. Acceptance tests pass all inputs directly; CLI handler owns prompting.
- **`IFileSystem.WriteFile` may silently overwrite** → Explicit `FileExists` check before any write call resolves this; confirmed in plan.
