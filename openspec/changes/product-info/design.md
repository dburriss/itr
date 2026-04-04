## Context

The `itr` CLI has commands for managing products (`init`, `register`, `list`) but no way to inspect the details of a single product. Users who want to see a product's documentation paths, repos, or coordination settings must manually inspect `product.yaml`. A `product info` subcommand fills this gap, matching the existing patterns set by `backlog info` and `task info`.

The command is implemented entirely within `src/cli/Program.fs`, following the established CLI architecture: Argu discriminated unions for argument parsing, a dedicated handler function, and output format branching (table/json/text).

## Goals / Non-Goals

**Goals:**
- Add `itr product info [productId]` subcommand
- Auto-detect product from current working directory (traverse up to find `product.yaml`) when no ID is provided
- Display: id, description, docs (absolute paths), repos (absolute paths), coord type, coord details
- Support `--output table | json | text` (text is tab-delimited)

**Non-Goals:**
- No editing capability — read-only
- No validation of whether repo paths actually exist on disk
- No changes to other commands or test files

## Decisions

### Decision: Add description field to domain types

`ProductConfigDto` and `ProductDefinition` do not currently have a `description` field, and `product.yaml` does not include one. To display description in `product info`, the field must be added across three files:

1. **`src/adapters/YamlAdapter.fs`** — add `Description: string` to `ProductConfigDto` (CLIMutable, YAML alias `description`)
2. **`src/domain/Domain.fs`** — add `Description: string option` to `ProductDefinition`
3. **`src/adapters/YamlAdapter.fs`** — map `dto.Description` to `Option` in `LoadProductConfig` (null/empty → `None`, otherwise `Some`)

The field is optional in both the YAML and the domain type — `product.yaml` files that omit it remain valid. The `YamlDotNet` deserializer already uses `IgnoreUnmatchedProperties` but will populate the field when present because `CLIMutable` records default string fields to `null`.

### Decision: Directory auto-detection

When no product ID is provided, traverse up from `Directory.GetCurrentDirectory()` looking for `product.yaml`. Derive the product root as the directory containing the file. Load the `ProductDefinition` directly via `IProductConfig.LoadProductConfig`. This avoids needing the product to be registered in the portfolio.

### Decision: Output format for multi-value fields

Docs and repos are multi-entry maps. For table output, use one row per entry. For JSON, emit arrays. For text, one tab-delimited line per entry in the format `id\tkey\tvalue`.

## Risks / Trade-offs

- [Directory detection: not in a product tree and no ID provided] → Return clear error: "No product ID provided and no product.yaml found in current directory or any parent directory."
- [Product ID provided but not in active profile] → Reuse `Portfolio.loadAllDefinitions` pattern and match on ID; return error if not found
