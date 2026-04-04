

# Add a product-info command

**Task ID:** product-info
**Backlog Item:** product-info
**Repo:** itr

## Description

Adds a product info command that takes in a product id.
If no id and in a directory or subdirectory of an existing product. 
Shows the info for that product: id, description(new), docs, repos, coord type, coord details.
Expected: `itr product info [productId]`

## Scope

- **Included:**
  - Add `product info` subcommand with optional product ID argument
  - Auto-detect product from current working directory if no ID provided (traverse up to find product.yaml)
  - Display product info: id, description, docs (absolute paths), repos (absolute paths), coord type, coord details
  - Support output formats: table (default), json, text

- **Excluded:**
  - No editing capability (read-only command)
  - No validation of repo existence
  - No changes to domain types (description field already exists in ProductConfigDto)

## Steps

1. Define `ProductInfoArgs` discriminated union type with `Product_Id` (optional) and `Output` arguments
2. Add `Info` case to `ProductArgs` union type with `ParseResults<ProductInfoArgs>`
3. Add usage string for Info case in `IArgParserTemplate.Usage`
4. Create `handleProductInfo` function that:
   - Loads portfolio and resolves active profile
   - If product ID provided, finds matching product in profile
   - If no ID, traverses up from current directory to find product.yaml and derive product root
   - Loads ProductDefinition via IProductConfig.LoadProductConfig
   - Formats output in table/json/text based on Output argument
5. Add routing for `ProductArgs.Info` in the CLI dispatch logic (around line 1838-1841)
6. Build and test the implementation

## Dependencies

- none

## Acceptance Criteria

- Prints in table format by default
- Can print as json
- Can print as text
- Contains absolute paths to documents
- Contains absolute paths to repos
- Output --text is tab-delimited for easy parsing

## Impact

- **Files changed:** `src/cli/Program.fs` only
- **Interfaces affected:** None - uses existing `IProductConfig.LoadProductConfig` and output format patterns
- **Data migrations:** None required

## Risks

- **Directory detection edge case:** If not in a product directory and no ID provided, should show clear error message
- **Mitigation:** Reuse existing pattern from `product list` to find products and show helpful error if product not found in active profile

## Open Questions

- None - the description and existing code patterns provide sufficient clarity