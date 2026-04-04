# List profile products

**Task ID:** product-list
**Backlog Item:** product-list
**Repo:** itr

## Description

Lists the products in the specified profile. If no profile is specified, uses the default set profile from itr.json

## Scope

**Included:**
- New `product list` CLI subcommand
- Profile lookup (case-insensitive, with default profile fallback)
- Loading product definitions from `product.yaml` for each registered product root
- Output of product id, repo count, and absolute path to coordination directory
- Support for json, text, and table output formats

**Explicitly excluded:**
- Any modification of itr.json or product registration
- Loading backlog items or tasks
- Interactive product selection
- Filtering or sorting options (future enhancement)

## Steps

1. **Add `ProfileProductsListArgs` Argu type** in `src/cli/Program.fs`:
   - Add optional `Name` argument for profile name (defaults to active/default profile)
   - Add optional `Output` argument for format selection
   - Implement `IArgParserTemplate` interface

2. **Update `ProfileArgs` discriminated union** in `src/cli/Program.fs`:
   - Add `Products` case with `ParseResults<ProfileProductsListArgs>`
   - Update usage documentation

3. **Implement `handleProfileProductsList` handler function** in `src/cli/Program.fs`:
   - Accept `configPath`, `portfolio`, `deps`, and `listArgs` parameters
   - Resolve active profile using `Portfolio.resolveActiveProfile` (supports `--profile` flag)
   - Return meaningful error if no profiles exist in portfolio
   - Load product definitions using a public wrapper around `PortfolioUsecase.loadAllDefinitions` (currently `private` — must be exposed or a new public function added to `PortfolioUsecase`)
   - Format output in 3 modes:
     - **Table**: columns for Id, Repo Count, Coord Root
     - **Text**: tab-delimited values: `id\trepoCount\tcoordRoot` (one record per line, consistent with `backlog list` and `task list`)
     - **JSON**: array of objects with `id`, `repoCount`, `coordRoot` fields

4. **Wire up dispatch case** in `dispatch` function in `src/cli/Program.fs`:
   - Add case for `ProfileArgs.Products` in the profile command handling
   - Call `handleProfileProductsList` with appropriate parameters

5. **Add error case for empty portfolio**:
   - If portfolio has no profiles, return error: "No profiles found. Run 'itr profile add <name>' to create one."

6. **Write acceptance tests** in `tests/acceptance/PortfolioAcceptanceTests.fs`:
   - Test listing products with explicit profile name
   - Test listing products using default profile
   - Test error when profile not found
   - Test error when no profiles exist
   - Test empty product list is expected behavior
   - Test all three output formats

## Dependencies

- none

## Acceptance Criteria

- Lists products from specified profile
- If no profile supplied, use default profile
- Errors with meaningful message if no profiles
- Empty list with no products is expected behaviour
- Supports json, text, and table(default) outputs
- Returns product id, repo count, and absolute path to coord directory

## Impact

**Files changed:**
- `src/cli/Program.fs` - Add Argu types, handler function, and dispatch case
- `src/features/Portfolio/PortfolioUsecase.fs` - Expose `loadAllDefinitions` (make public or add a public wrapper)

**Interfaces affected:**
- No new interfaces; uses existing `IProductConfig`, `IPortfolioConfig`, `IFileSystem` capabilities

**Data flow:**
- Reads `itr.json` to get portfolio
- Reads `product.yaml` for each registered product root
- No data migrations or persistent changes

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|-----------|
| Product root path no longer exists | Low | Handler returns error with product id | Already handled by existing `loadAllDefinitions` logic |
| product.yaml is malformed | Low | Entire command fails | Consistent with existing `loadAllDefinitions` fail-fast pattern |
| Coord root directory doesn't exist | Low | Product appears but coord root missing | Acceptable for listing - shows current state |

## Decisions

1. **Load failure behavior**: The entire command fails on any product load error. This is consistent with the existing fail-fast pattern used throughout the codebase — `loadAllDefinitions` uses `Result.bind` chaining which short-circuits on the first `Error`, and all other commands (backlog list, task list, etc.) follow the same approach.

2. **Product identifier**: Output uses the `id` field from `product.yaml` (stored as `ProductDefinition.Id`), not the directory basename. This is the canonical identifier used consistently across the codebase for product lookup, display, and registration confirmation.

3. **Text output format**: Tab-delimited (`id\trepoCount\tcoordRoot`), consistent with `backlog list --output text` and `task list --output text`. The `repoCount` is derived from `def.Repos.Count`. The `coordRoot` is `def.CoordRoot.AbsolutePath`. This format is required by the `itr-products` tv channel which uses `'{split:\t:0}'` to extract the product id.
