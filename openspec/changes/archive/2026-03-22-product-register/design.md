## Context

The portfolio config (`itr.json`) tracks product roots per profile. Users need a CLI command to register pre-existing product directories. A partial `registerProduct` implementation exists in `PortfolioUsecase.fs` but does not match the plan spec: it checks for duplicate roots (not duplicate canonical ids), does not perform a directory existence check via `IFileSystem`, and does not load `product.yaml` to read the canonical id.

The `ProductArgs` DU in `Program.fs` exists with only `Init` subcommand. `formatPortfolioError` is missing cases for `ProductNotFound`, `CoordRootNotFound`, and `DuplicateProductId`.

## Goals / Non-Goals

**Goals:**
- Replace the existing stub `registerProduct` in `PortfolioUsecase.fs` with the full spec-compliant implementation
- Add `itr product register <path>` CLI surface under the existing `ProductArgs` hierarchy
- Add missing `formatPortfolioError` cases
- Cover the new usecase and CLI with communication and acceptance tests

**Non-Goals:**
- Creating new product files (product.yaml, PRODUCT.md, etc.)
- Changing path storage convention (paths stored as-supplied; `expandPath` applied at read time)
- Optimizing `loadAllDefinitions` IO cost
- Changing `backlog take` behavior (still uses first product)

## Decisions

**1. Replace stub vs. incremental patch**
The existing `registerProduct` stub checks for duplicate roots instead of duplicate canonical ids and lacks the `IFileSystem`/`IProductConfig` constraints. The plan-specified implementation is different enough that the cleanest approach is to rewrite the function body and update the type signature rather than patching incrementally.

Alternative considered: keep stub, add separate path — rejected because two functions with same name would conflict.

**2. `Profile` field type: `string option` in the domain, resolved at CLI**
The plan specifies `RegisterProductInput.Profile: string option`. The CLI resolves the active profile name via the `-p` flag or default, and passes it as `Some name` or `None`. The usecase then falls back to `portfolio.DefaultProfile` when `None`.

**3. `IFileSystem` constraint added to usecase**
Directory existence must be validated before loading `product.yaml` to give a clear error. This requires `IFileSystem.DirectoryExists`. The constraint is added to `registerProduct`'s type parameter.

**4. Error cases to add to `formatPortfolioError`**
`ProductNotFound`, `CoordRootNotFound`, and `DuplicateProductId` are all reachable through the `product register` handler. They must be enumerated explicitly before the catch-all `%A{other}`.

## Risks / Trade-offs

- **Existing `registerProduct` used by `initProduct`** — The stub is called by `initProduct`. Updating the signature (Profile from `string` to `string option`) will require updating the call site in `initProduct` as well. → Pass `Some profile` from `initProduct`.
- **`loadAllDefinitions` does IO on every register** — Acceptable for MVP; flagged in the plan.
- **Catch-all mask** — Enumerating error cases explicitly before committing avoids silent regressions from the `%A{other}` branch.
