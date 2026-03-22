## 1. Usecase (`PortfolioUsecase.fs`)

- [ ] 1.1 Update `RegisterProductInput` to use `Profile: string option` (was `string`)
- [ ] 1.2 Add `IProductConfig` and `IFileSystem` constraints to `registerProduct`
- [ ] 1.3 Replace stub body: resolve active profile using `input.Profile` or `portfolio.DefaultProfile`
- [ ] 1.4 Validate path is non-empty and directory exists via `IFileSystem.DirectoryExists`; return `ProductConfigError` if absent
- [ ] 1.5 Load `product.yaml` via `IProductConfig.LoadProductConfig`; propagate `ProductConfigError` on failure
- [ ] 1.6 Call `loadAllDefinitions` to detect duplicate canonical ids; return `DuplicateProductId` on collision
- [ ] 1.7 Append `ProductRef { Root = ProductRoot path }` to profile and return updated `Portfolio` (no save — caller persists)
- [ ] 1.8 Update `initProduct` call site: pass `Some profile` to match new `string option` signature

## 2. CLI DUs (`Program.fs`)

- [ ] 2.1 Add `ProductRegisterArgs` DU with `[<MainCommand; Mandatory>] Path of path: string`
- [ ] 2.2 Add `Register of ParseResults<ProductRegisterArgs>` case to `ProductArgs`
- [ ] 2.3 Update `IArgParserTemplate` usage strings for `ProductRegisterArgs` and `ProductArgs.Register`

## 3. Handler (`Program.fs`)

- [ ] 3.1 Add `handleProductRegister` function: resolve config path, bootstrap, parse `Path`, call `registerProduct`
- [ ] 3.2 On success: persist via `SaveConfig` and print `"Registered product '<id>' from '<path>'."` (or JSON equivalent)
- [ ] 3.3 On error: format with `formatPortfolioError`
- [ ] 3.4 Wire handler into main dispatch: match `Product(Register _)` branch

## 4. Error Formatting (`Program.fs`)

- [ ] 4.1 Add `ProductNotFound id -> $"Product '{id}' not found."` to `formatPortfolioError`
- [ ] 4.2 Add `CoordRootNotFound(id, path) -> $"Coordination root for '{id}' not found at: {path}"` to `formatPortfolioError`
- [ ] 4.3 Add `DuplicateProductId(profile, id) -> $"Product '{id}' is already registered in profile '{profile}'."` to `formatPortfolioError`

## 5. Communication Tests (`tests/communication/PortfolioDomainTests.fs`)

- [ ] 5.1 Test: valid path adds `ProductRef` to active profile and returns updated `Portfolio`
- [ ] 5.2 Test: duplicate canonical id returns `DuplicateProductId`; portfolio unchanged
- [ ] 5.3 Test: non-existent directory returns `ProductConfigError`; portfolio unchanged
- [ ] 5.4 Test: missing `product.yaml` propagates `ProductConfigError`
- [ ] 5.5 Test: named profile not found returns `ProfileNotFound`

## 6. Acceptance Tests (`tests/acceptance/PortfolioAcceptanceTests.fs`)

- [ ] 6.1 Test: end-to-end — write `itr.json` with one profile (no products), call `registerProduct`, read back file, assert product root present
- [ ] 6.2 Test: duplicate registration — second call with same canonical id returns `DuplicateProductId`; file unchanged
- [ ] 6.3 Test: round-trip — existing profiles and other products are not altered by new registration

## 7. Verification

- [ ] 7.1 `dotnet build` passes with no errors
- [ ] 7.2 `dotnet test` passes with all new tests green
