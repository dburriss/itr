## Context

The `itr` CLI manages a portfolio of products across multiple repositories. Products are registered in profiles stored in `itr.json`. Currently there is no CLI command to list the products registered in a profile — users have no programmatic way to enumerate products without inspecting `itr.json` directly.

The `loadAllDefinitions` function in `PortfolioUsecase.fs` already performs the full load (reads each registered product root, parses `product.yaml`, detects duplicates), but is currently `private`. The new `product list` command needs this capability exposed.

## Goals / Non-Goals

**Goals:**
- Add `product list` as a subcommand under the profile group (`itr product list` or via profile context)
- Expose `loadAllDefinitions` in `PortfolioUsecase.fs` (or a thin public wrapper)
- Support `--profile` flag to target a specific profile, falling back to active/default profile
- Support `--output table|text|json` (table is default)
- Output: product id, repo count, and absolute path to coord directory
- Return a meaningful error if no profiles exist in the portfolio

**Non-Goals:**
- Modifying product registration or `itr.json`
- Loading backlog items or tasks
- Filtering or sorting products
- Interactive product selection

## Decisions

1. **Expose `loadAllDefinitions` as public** rather than adding a separate function. The existing implementation already does exactly what is needed (loads all `product.yaml` files, detects duplicate ids, returns `(ProductRef * ProductDefinition) list`). Wrapping it would be indirection without benefit.

2. **Add `Products` case to `ProfileArgs`** (not a separate top-level command). The data is profile-scoped so `itr profile products list` is the natural grouping. However, based on the plan, a `ProfileProductsListArgs` type is added with optional `--profile` and `--output` arguments and wired into a `Products` case on `ProfileArgs`.

3. **Fail-fast on load errors** (consistent with `resolveProduct`, `backlog list`, `task list`). If any `product.yaml` is unreadable or malformed, the entire command returns an error. This is the established pattern — partial results would be misleading.

4. **Text format**: Tab-delimited `id\trepoCount\tcoordRoot` per line, matching `backlog list --output text` and `task list --output text`. Required for the `itr-products` tv channel which splits on `\t`.

5. **Empty list is success**: A profile with no registered products returns an empty table/list, not an error.

## Risks / Trade-offs

- **Product root no longer exists** → `loadAllDefinitions` will call `LoadProductConfig` which returns an error for missing paths; the command fails with a clear error including the product id. Acceptable.
- **`product.yaml` malformed** → Entire command fails (fail-fast). Consistent with existing behaviour.
- **Coord root directory missing** → The product still appears in the list; `CoordRoot.AbsolutePath` is derived from `product.yaml` config, not filesystem presence. Acceptable for listing — shows the configured state.
