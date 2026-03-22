## Why

There is no way to track existing product directories in the portfolio config. Users need to register pre-existing product roots so that `itr` commands (like `backlog take`) can resolve them without manual config edits.

## What Changes

- New `itr product register <path>` CLI command appends a product root to the active profile's product list in `itr.json`
- The product id is read from `<path>/product.yaml`; duplicate ids within the profile are rejected
- No product files are created or modified; only `itr.json` is updated
- A new `RegisterProductInput` type and `registerProduct` usecase added to `PortfolioUsecase.fs`
- New Argu DU hierarchy: `ProductRegisterArgs` → `ProductArgs` → added to top-level `CliArgs`
- Missing `formatPortfolioError` cases added for `ProductNotFound`, `CoordRootNotFound`, `ProductConfigError`, `DuplicateProductId`

## Capabilities

### New Capabilities

- `product-register`: Register an existing product root directory into the active profile by appending it to `itr.json`; validates directory existence and `product.yaml` presence; rejects duplicate product ids

### Modified Capabilities

<!-- No existing spec-level requirements are changing -->

## Impact

- `src/features/Portfolio/PortfolioUsecase.fs` — new type and usecase function
- `src/cli/Program.fs` — new CLI DUs, handler, and error formatting cases
- `tests/communication/PortfolioDomainTests.fs` — new unit tests
- `tests/acceptance/PortfolioAcceptanceTests.fs` — new acceptance tests
- No changes to `Domain.fs`, `Interfaces.fs`, or any adapter
