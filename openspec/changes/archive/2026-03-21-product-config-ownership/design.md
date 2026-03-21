## Context

`itr.json` currently stores each product as `{ id, root: { mode, dir/repoDir } }`, duplicating identity and coordination configuration that belongs in `product.yaml`. The code trusts those persisted values rather than loading the canonical definition from the product root. The existing `portfolio-config` and `product-resolution` specs describe this old shape, and the `IProductConfig` interface takes a `coordRoot` (`.itr` path) instead of a product root directory.

The `docs/config-files.md` already documents the target model — `itr.json` products become bare path strings and `product.yaml` becomes the single source of truth — but nothing in the implementation reflects this yet.

## Goals / Non-Goals

**Goals:**
- Change the persisted `itr.json` product entry from `{ id, root }` to a root directory path string
- Parse `id`, `repos`, `docs`, and `coordination` from `product.yaml` at the product root
- Derive `CoordinationRoot` from the `coordination` section in `product.yaml`
- Detect duplicate product registrations by canonical id loaded from `product.yaml`
- Update domain types, adapters, use-cases, CLI, tests, and docs atomically
- Provide reusable save/load helpers that future `product-register` and `product-init` commands can adopt

**Non-Goals:**
- Implementing `product-register` or `product-init` commands
- Backward-compatible reading of the old `itr.json` shape (clean break; fixtures updated atomically)
- Changing backlog, task, or git functionality

## Decisions

### 1. `itr.json` product entry becomes a bare path string

Current: `"products": [{ "id": "foo", "root": { "mode": "standalone", "dir": "~/foo" } }]`  
New: `"products": ["~/foo"]`

**Rationale**: The id and coordination mode belong in `product.yaml`. Storing them again in `itr.json` creates two sources of truth. A plain string is the simplest representation of "a registered location".  
**Alternative considered**: Keep the id but drop the root — rejected because even an id duplication is friction when `product.yaml` is already authoritative.

### 2. `product.yaml` lives at the product root, not the coordination root

The adapter will load `<product-root>/product.yaml`. The `coordination` block inside it (mode + repo + path) is used to derive `CoordinationRoot` programmatically.  
**Rationale**: Aligns with the documented model and makes the product root the single registration unit.

### 3. `ProductConfigDto` is expanded to cover `id`, `repos`, `docs`, and `coordination`

The existing DTO only maps `id` and `repos`. It gains `docs` and `coordination` fields, matching what the real `itr/product.yaml` already has.  
**Rationale**: Enables deriving `CoordinationRoot` from the canonical file without separate wiring.

### 4. Duplicate detection moves to the portfolio use-case, keyed on canonical id

After loading `product.yaml` for each registered root, the use-case compares canonical ids. A second root that resolves to the same id is rejected with `DuplicateProductId`.  
**Rationale**: The id is no longer persisted, so detection must happen at resolution time. The use-case is the right boundary because it orchestrates the full load → resolve pipeline.

### 5. `IProductConfig` signature changes from `coordRoot` to `productRoot`

`LoadProductConfig` will accept the product root directory and return a richer domain type that includes coordination metadata.  
**Rationale**: All callers currently derive `coordRoot` before calling this; with the new model the coordination root is an output of loading product config, not an input.

### 6. Clean break — no backward-compatible parsing of old `itr.json` shape

The old and new shapes are structurally incompatible (object vs. string in the products array). Fixtures and docs are updated atomically. Users with existing `itr.json` files will need to migrate manually or re-bootstrap.  
**Rationale**: The project has no GA release; a compatibility shim adds code complexity with no current user benefit.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Coordination root derivation may regress for all three modes | Add fixture coverage for `standalone`, `primary-repo`, and `control-repo` in communication tests before wiring the CLI |
| Acceptance tests for task-taking depend on `product.yaml` being at the coordination root | Move `product.yaml` to the product root in fixtures and update the loader; keep `.itr/BACKLOG/` layout unchanged |
| CLI product-selection and backlog commands will break mid-refactor | Stage the change: domain types first, then adapters, then use-cases, then CLI |
| `product.yaml` `profile` field in the real `itr/product.yaml` has no mapping | Leave it unmapped for now; remove the field from `itr/product.yaml` as part of the cleanup scope item |

## Migration Plan

1. Update `Domain.fs` — introduce `ProductRoot` (path wrapper), expand `ProductConfig` with `docs` and `coordination`, revise `ProductRef` to hold only `ProductRoot`, update `ResolvedProduct`
2. Update `Interfaces.fs` — change `IProductConfig.LoadProductConfig` to accept `productRoot: string`
3. Update `PortfolioAdapter.fs` — new DTO with products as `string array`, new `CoordinationRootConfigConverter`-free path, update `IPortfolioConfig`
4. Update `YamlAdapter.fs` — expand `ProductConfigDto`, load from product root, compute `CoordinationRoot` from `coordination` section
5. Update `PortfolioUsecase.fs` — `resolveProduct` loads `product.yaml` via the new `IProductConfig`, builds `ResolvedProduct` with derived coordination root
6. Update `Program.fs` — pass product root to resolution pipeline
7. Update `tests/` — rewrite fixtures for new shapes; add communication tests for duplicate detection and all three coordination modes
8. Update `itr/product.yaml` and `docs/` — remove stale fields, confirm examples match

**Rollback**: revert all files atomically; there are no database migrations or external deployments to undo.

## Open Questions

1. Should `ProductConfig` in `Domain.fs` be extended in place to carry `docs` and `coordination`, or should a new richer product-definition type be introduced alongside a narrower `ProductConfig` projection used by backlog-taking?
2. Should `docs` in `product.yaml` map to a domain type or be kept as a raw `Map<string, string>`?
