## 1. Usecase Types and Function

- [x] 1.1 Add `InitProductInput` record type to `PortfolioUsecase.fs`
- [x] 1.2 Add `initProduct` function signature and stub to `PortfolioUsecase.fs`
- [x] 1.3 Implement `InvalidProductId` validation via `ProductId.tryCreate`
- [x] 1.4 Implement path existence check via `IFileSystem.DirectoryExists`
- [x] 1.5 Implement `product.yaml` existence guard via `IFileSystem.FileExists`
- [x] 1.6 Implement `product.yaml` content generation (primary-repo and standalone modes)
- [x] 1.7 Write `product.yaml` via `IFileSystem.WriteFile`
- [x] 1.8 Write coordination directory sentinel (`<coordPath>/.gitkeep`) via `IFileSystem.WriteFile`
- [x] 1.9 Write `PRODUCT.md` template with product id substituted
- [x] 1.10 Write `ARCHITECTURE.md` template with product id substituted
- [x] 1.11 Delegate to `registerProduct` when `RegisterProfile` is `Some`; return `Some updatedPortfolio`
- [x] 1.12 Return `None` when `RegisterProfile` is `None`

## 2. CLI Extension

- [x] 2.1 Add `ProductInitArgs` Argu DU to `Program.fs`
- [x] 2.2 Extend `ProductArgs` DU with `Init of ParseResults<ProductInitArgs>` case
- [x] 2.3 Implement CLI handler for `Init` case: collect inputs, resolve registration target
- [x] 2.4 Add interactive prompt for missing `id` (`AnsiConsole.Ask "Product id:"`)
- [x] 2.5 Add interactive prompt for missing `repo-id` (`AnsiConsole.Ask "Repo id (default: same as id):"`)
- [x] 2.6 Add interactive prompt for registration profile when neither `--register-profile` nor `--no-register` supplied
- [x] 2.7 Print success message `"Initialized product '<id>' at <path>."` on success
- [x] 2.8 Print `"Registered in profile '<profile>'."` when registration occurred

## 3. Error Formatting

- [x] 3.1 Verify `formatPortfolioError` covers `InvalidProductId` case
- [x] 3.2 Verify `formatPortfolioError` covers `ProductConfigError(root, msg)` case

## 4. Communication Tests

- [x] 4.1 Test: `initProduct` with valid inputs and `RegisterProfile = Some` writes all files and returns `Some updatedPortfolio`
- [x] 4.2 Test: `initProduct` with `RegisterProfile = None` writes files and returns `None`
- [x] 4.3 Test: `initProduct` when `product.yaml` already exists returns `ProductConfigError`; no files written
- [x] 4.4 Test: `initProduct` when path directory does not exist returns `ProductConfigError`
- [x] 4.5 Test: `initProduct` with invalid id returns `InvalidProductId`
- [x] 4.6 Test: `initProduct` with `CoordinationMode = "standalone"` writes yaml without `coordination.repo`

## 5. Acceptance Tests

- [x] 5.1 End-to-end: `initProduct` creates all expected files with correct content; `itr.json` updated when registered
- [x] 5.2 Skip registration: `itr.json` unchanged when `RegisterProfile = None`
- [x] 5.3 Duplicate product root: second init with same profile returns error; `itr.json` unchanged

## 6. Verification

- [x] 6.1 Run `dotnet build` — no errors
- [x] 6.2 Run `dotnet test` — all tests pass
