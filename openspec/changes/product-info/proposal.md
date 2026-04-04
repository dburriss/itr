## Why

The `itr` CLI lacks a way to inspect details of a specific product, making it hard to quickly review product metadata like documentation paths, repos, and coordination details. Adding a `product info` subcommand fills this gap and aligns with the existing `backlog-info` and `task-info` command patterns.

## What Changes

- Add `product info [productId]` subcommand to the CLI
- Auto-detect current product from working directory when no ID is provided (traverse up to find `product.yaml`)
- Display product details: id, description, docs (absolute paths), repos (absolute paths), coord type, coord details
- Support output formats: table (default), json, text (tab-delimited)

## Capabilities

### New Capabilities
- `product-info`: Retrieve and display product information by ID or by auto-detecting the product from the current working directory

### Modified Capabilities

## Impact

- `src/cli/Program.fs`: Add `ProductInfoArgs` type, `Info` case to `ProductArgs`, usage strings, `handleProductInfo` function, and routing
- No changes to domain types or interfaces — uses existing `IProductConfig.LoadProductConfig` and output format patterns
