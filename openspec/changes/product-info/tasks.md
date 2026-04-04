## 1. Domain Types

- [ ] 1.1 Add `Description: string` field to `ProductConfigDto` in `src/adapters/YamlAdapter.fs` (with `[<YamlMember(Alias = "description")>]`)
- [ ] 1.2 Add `Description: string option` field to `ProductDefinition` in `src/domain/Domain.fs`
- [ ] 1.3 Map `dto.Description` to `Option` in `LoadProductConfig` in `src/adapters/YamlAdapter.fs` (null/empty → `None`, otherwise `Some`)

## 2. Argument Types

- [ ] 2.1 Define `ProductInfoArgs` discriminated union with optional `Product_Id` (main command) and `Output` arguments
- [ ] 2.2 Add `Info` case to `ProductArgs` union type with `ParseResults<ProductInfoArgs>`
- [ ] 2.3 Add usage strings for both `ProductInfoArgs` cases and the `Info` case in `IArgParserTemplate.Usage`

## 3. Handler Implementation

- [ ] 3.1 Create `handleProductInfo` function signature accepting `configPath`, `deps`, and `ParseResults<ProductInfoArgs>`
- [ ] 3.2 Implement product lookup by ID: load portfolio, resolve active profile, find product in profile by ID, load `ProductDefinition` via `IProductConfig.LoadProductConfig`
- [ ] 3.3 Implement directory auto-detection: traverse up from `Directory.GetCurrentDirectory()` looking for `product.yaml`; return clear error if not found
- [ ] 3.4 Implement table output: display id, description, docs (key + absolute path per row), repos (key + absolute path per row), coord mode, coord repo, coord path
- [ ] 3.5 Implement JSON output: emit object with `id`, `description`, `docs` (object), `repos` (object with `path` and optional `url`), `coordMode`, `coordRepo`, `coordPath`
- [ ] 3.6 Implement text output: one tab-delimited line per scalar field (including description); one line per doc/repo entry in format `<field>\t<key>\t<value>`

## 4. Routing

- [ ] 4.1 Add routing for `ProductArgs.Info` in the CLI dispatch logic (around line 1838-1841 in `Program.fs`), before the existing `None` error fallback

## 5. Build and Verify

- [ ] 5.1 Run `dotnet build` and fix any compilation errors
- [ ] 5.2 Run `dotnet test` and verify all tests pass
- [ ] 5.3 Manually test `itr product info itr` (by ID) and `itr product info` (from a product directory) in both table and json output modes
