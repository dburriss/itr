## 1. Domain types

- [ ] 1.1 Add `ProductRoot` wrapper type (`ProductRoot of string`) to `Domain.fs`
- [ ] 1.2 Introduce `ProductDefinition` type in `Domain.fs` with fields `Id`, `Repos`, `Docs`, and `Coordination`
- [ ] 1.3 Add `CoordinationConfig` type to `Domain.fs` covering `mode`, `repo`, and `path` fields matching `product.yaml`
- [ ] 1.4 Replace `ProductRef.Root: CoordinationRootConfig` with `ProductRef.Root: ProductRoot` in `Domain.fs`
- [ ] 1.5 Update `ResolvedProduct` in `Domain.fs` to include the loaded `ProductDefinition` alongside the existing `CoordRoot`
- [ ] 1.6 Update `PortfolioError` to include a `ProductConfigError` case for `product.yaml` parse failures
- [ ] 1.7 Remove `CoordinationRootConfig` DU from `Domain.fs` (no longer persisted or needed as a domain type)

## 2. Interfaces

- [ ] 2.1 Change `IProductConfig.LoadProductConfig` signature in `Interfaces.fs` to accept `productRoot: string` instead of `coordRoot: string` and return `Result<ProductDefinition, PortfolioError>`

## 3. PortfolioAdapter

- [ ] 3.1 Replace `ProductRefDto` in `PortfolioAdapter.fs` — product entries become `string` (root directory path) rather than `{ id; root }`
- [ ] 3.2 Remove `CoordinationRootConfigConverter` from `PortfolioAdapter.fs`
- [ ] 3.3 Update `mapProduct` to produce a `ProductRef` containing only a `ProductRoot` (no id, no coordination config)
- [ ] 3.4 Update `readConfig` and `PortfolioConfigAdapter` to reflect new DTO shape
- [ ] 3.5 Add `writeConfig` / save function to `PortfolioAdapter.fs` using the new path-string shape (for future use by `product-register`)

## 4. YamlAdapter

- [ ] 4.1 Expand `ProductConfigDto` in `YamlAdapter.fs` to include `docs` and `coordination` fields
- [ ] 4.2 Add `CoordinationConfigDto` in `YamlAdapter.fs` for the `coordination` block (`mode`, `repo`, `path`)
- [ ] 4.3 Update `ProductConfigAdapter.LoadProductConfig` to accept `productRoot: string` and load `<productRoot>/product.yaml`
- [ ] 4.4 Implement coordination root derivation logic: compute `CoordinationRoot` from `CoordinationConfig` + repo paths + product root
- [ ] 4.5 Map the derived `CoordinationRoot` into the returned `ProductDefinition`
- [ ] 4.6 Add slug validation for `id` in `YamlAdapter.fs` (matches pattern `[a-z0-9][a-z0-9\-]*`)

## 5. PortfolioUsecase

- [ ] 5.1 Update `resolveProduct` in `PortfolioUsecase.fs` to iterate registered root paths, call `LoadProductConfig` per root, and match canonical id
- [ ] 5.2 Add duplicate product registration detection in `PortfolioUsecase.fs` — reject if two roots yield the same canonical id
- [ ] 5.3 Update `ResolvedProduct` construction to use the `CoordinationRoot` derived from `product.yaml`

## 6. CLI

- [ ] 6.1 Update `Program.fs` to pass product root path to the resolution pipeline instead of constructing a coordination root from `itr.json`
- [ ] 6.2 Verify backlog and product-selection commands continue to receive a valid `coordRoot` derived through the new pipeline

## 7. Communication tests

- [ ] 7.1 Add `PortfolioDomainTests.fs` (or update existing) — test duplicate detection by canonical id
- [ ] 7.2 Add resolution tests proving a registered product root loads `product.yaml` and produces the correct coordination root for each of the three modes (`standalone`, `primary-repo`, `control-repo`)
- [ ] 7.3 Add tests for slug validation in `product.yaml` parsing

## 8. Acceptance tests

- [ ] 8.1 Rewrite `PortfolioAcceptanceTests.fs` fixtures — `itr.json` stores path strings, each root contains a `product.yaml`
- [ ] 8.2 Add acceptance case for duplicate registration (two paths, same canonical id)
- [ ] 8.3 Update `TaskAcceptanceTests.fs` fixtures — `product.yaml` lives at product root; verify coordination root and task execution still work

## 9. Checked-in metadata and docs

- [ ] 9.1 Update `itr/product.yaml` — remove `profile` field; confirm `id`, `repos`, `docs`, and `coordination` match canonical schema
- [ ] 9.2 Update `docs/config-files.md` — confirm all `itr.json` examples use path-string product entries; confirm `product.yaml` examples reflect canonical fields
- [ ] 9.3 Update `docs/lifecycles.md` (if present) — confirm product root registration and resolution lifecycle is accurate

## 10. Verification

- [ ] 10.1 Run `dotnet build` and fix any compilation errors
- [ ] 10.2 Run `dotnet test` and fix any test failures
- [ ] 10.3 Run `mise run format` if any formatting changes are needed
