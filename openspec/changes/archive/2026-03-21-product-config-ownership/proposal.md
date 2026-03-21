## Why

`itr.json` currently stores duplicated product identity and coordination layout that already belong in `product.yaml`, causing data drift and making `product.yaml` redundant as a canonical source. Consolidating ownership into `product.yaml` eliminates the duplication and provides a single source of truth for product metadata.

## What Changes

- `itr.json` profile products change from objects with embedded `id` and `root` config to a simple list of product root directory paths
- `product.yaml` becomes the canonical source for `id`, `repos`, `docs`, and `coordination` layout
- Product loading reads `<product-root>/product.yaml` instead of deriving everything from `itr.json`
- Duplicate product registration is detected using the canonical `id` loaded from `product.yaml`, not a persisted `id` in `itr.json`
- The checked-in `itr/product.yaml` is updated to match the canonical schema
- Docs are reconciled with the implemented ownership model

## Capabilities

### New Capabilities
- `product-config`: Canonical product definition read from `product.yaml` at a product root directory, covering `id`, `repos`, `docs`, and `coordination` fields

### Modified Capabilities
- `portfolio-config`: **BREAKING** — product entries in `itr.json` change from `{id, root}` objects to bare root directory path strings; `CoordinationRootConfig` is no longer persisted in `itr.json`
- `product-resolution`: Products are now resolved by loading `product.yaml` from each registered root path; `CoordinationRoot` is derived from the canonical `coordination` section in `product.yaml`, not from `itr.json`

## Impact

- `src/domain/Domain.fs` — replace `ProductRefDto`/`CoordinationRootConfig` with path-based registration types and a richer canonical product definition
- `src/domain/Interfaces.fs` — update product-loading capability signatures to accept a product root directory
- `src/adapters/PortfolioAdapter.fs` — read/write the new path-only product entry shape in `itr.json`
- `src/adapters/YamlAdapter.fs` — parse canonical `product.yaml` fields and derive coordination metadata from product root and repo layout
- `src/features/Portfolio/PortfolioUsecase.fs` — resolve products from registered root paths via `product.yaml`; detect duplicates by canonical id
- `src/cli/Program.fs` — route product and backlog commands through the new resolution pipeline
- `tests/acceptance/PortfolioAcceptanceTests.fs` — update fixtures to register product root paths
- `tests/acceptance/TaskAcceptanceTests.fs` — update fixtures to place `product.yaml` at product root
- `itr/product.yaml` — remove stale `profile` field, reflect canonical schema
- `docs/config-files.md` — confirm examples match the implemented ownership model
