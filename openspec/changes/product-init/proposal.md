## Why

Users need a fast, guided way to scaffold a new product from scratch — creating `product.yaml`, starter docs, and the coordination directory in one command, without having to manually compose YAML or remember the required file layout. The existing `product register` command only handles pre-existing products; there is no creation path today.

## What Changes

- New `itr product init` CLI subcommand added to `ProductArgs`.
- New `InitProductInput` type and `initProduct` usecase in `PortfolioUsecase.fs`.
- Interactive prompts (Spectre.Console) for missing `id` and `repo-id`, and optional registration profile.
- On success: writes `product.yaml`, `PRODUCT.md`, `ARCHITECTURE.md`, and creates the coordination directory at the target path.
- Delegates registration to the existing `registerProduct` logic when a profile is supplied.

## Capabilities

### New Capabilities
- `product-init`: Scaffolds a new product on disk (writes `product.yaml`, `PRODUCT.md`, `ARCHITECTURE.md`, creates coordination directory) and optionally registers the new product root in `itr.json`.

### Modified Capabilities
<!-- No existing spec-level requirements are changing. -->

## Impact

- `src/features/Portfolio/PortfolioUsecase.fs` — new types and usecase function.
- `src/cli/Program.fs` — extended `ProductArgs` DU and new CLI handler.
- `tests/communication/PortfolioDomainTests.fs` — new usecase unit tests.
- `tests/acceptance/PortfolioAcceptanceTests.fs` — new end-to-end acceptance tests.
- Depends on `product-register` being in place (provides `ProductArgs`, `registerProduct`, `formatPortfolioError`).
