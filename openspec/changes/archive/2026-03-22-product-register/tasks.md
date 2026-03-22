## 1. Usecase (`PortfolioUsecase.fs`)

- [x] 1.1 Update `RegisterProductInput` to use `Profile: string option` (was `string`)
- [x] 1.2 Add `IProductConfig` and `IFileSystem` constraints to `registerProduct`
- [x] 1.3 Replace stub body: resolve active profile using `input.Profile` or `portfolio.DefaultProfile`
- [x] 1.4 Validate path is non-empty and directory exists via `IFileSystem.DirectoryExists`; return `ProductConfigError` if absent
- [x] 1.5 Load `product.yaml` via `IProductConfig.LoadProductConfig`; propagate `ProductConfigError` on failure
- [x] 1.6 Call `loadAllDefinitions` to detect duplicate canonical ids; return `DuplicateProductId` on collision
- [x] 1.7 Append `ProductRef { Root = ProductRoot path }` to profile and return updated `Portfolio` (no save — caller persists)
- [x] 1.8 Update `initProduct` call site: pass `Some profile` to match new `string option` signature

## 2. CLI DUs (`Program.fs`)

- [x] 2.1 Add `ProductRegisterArgs` DU with `[<MainCommand; Mandatory>] Path of path: string`
- [x] 2.2 Add `Register of ParseResults<ProductRegisterArgs>` case to `ProductArgs`
- [x] 2.3 Update `IArgParserTemplate` usage strings for `ProductRegisterArgs` and `ProductArgs.Register`

## 3. Handler (`Program.fs`)

- [x] 3.1 Add `handleProductRegister` function: resolve config path, bootstrap, parse `Path`, call `registerProduct`
- [x] 3.2 On success: persist via `SaveConfig` and print `"Registered product '<id>' from '<path>'."` (or JSON equivalent)
- [x] 3.3 On error: format with `formatPortfolioError`
- [x] 3.4 Wire handler into main dispatch: match `Product(Register _)` branch

## 4. Error Formatting (`Program.fs`)

- [x] 4.1 Add `ProductNotFound id -> $"Product '{id}' not found."` to `formatPortfolioError`
- [x] 4.2 Add `CoordRootNotFound(id, path) -> $"Coordination root for '{id}' not found at: {path}"` to `formatPortfolioError`
- [x] 4.3 Add `DuplicateProductId(profile, id) -> $"Product '{id}' is already registered in profile '{profile}'."` to `formatPortfolioError`

## 5. Communication Tests (`tests/communication/PortfolioDomainTests.fs`)

- [x] 5.1 Test: valid path adds `ProductRef` to active profile and returns updated `Portfolio`
- [x] 5.2 Test: duplicate canonical id returns `DuplicateProductId`; portfolio unchanged
- [x] 5.3 Test: non-existent directory returns `ProductConfigError`; portfolio unchanged
- [x] 5.4 Test: missing `product.yaml` propagates `ProductConfigError`
- [x] 5.5 Test: named profile not found returns `ProfileNotFound`

## 6. Acceptance Tests (`tests/acceptance/PortfolioAcceptanceTests.fs`)

- [x] 6.1 Test: end-to-end — write `itr.json` with one profile (no products), call `registerProduct`, read back file, assert product root present
- [x] 6.2 Test: duplicate registration — second call with same canonical id returns `DuplicateProductId`; file unchanged
- [x] 6.3 Test: round-trip — existing profiles and other products are not altered by new registration

## 7. Verification

- [x] 7.1 `dotnet build` passes with no errors
- [x] 7.2 `dotnet test` passes with all new tests green
