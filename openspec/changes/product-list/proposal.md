## Why

Users need to inspect which products are registered in a profile to understand the current portfolio state. Currently there is no way to list products from the CLI, making it difficult to verify registrations or use products as input to other commands.

## What Changes

- New `product list` CLI subcommand under `profile`
- Loads product definitions from `product.yaml` for each registered product root in the profile
- Outputs product id, repo count, and absolute path to coordination directory
- Supports `--profile` flag to target a specific profile (defaults to active profile)
- Supports `--output` flag for json, text, and table formats (table is default)
- `PortfolioUsecase.loadAllDefinitions` is exposed publicly (or a public wrapper is added)

## Capabilities

### New Capabilities
- `product-list`: Lists products registered in a profile, showing id, repo count, and coord root path; supports json/text/table output formats

### Modified Capabilities
<!-- No existing spec-level requirement changes -->

## Impact

- `src/cli/Program.fs` — new Argu types (`ProfileProductsListArgs`), handler (`handleProfileProductsList`), dispatch wiring
- `src/features/Portfolio/PortfolioUsecase.fs` — expose `loadAllDefinitions` as public (or add public wrapper)
- `tests/acceptance/PortfolioAcceptanceTests.fs` — new acceptance tests covering all scenarios and output formats
