## 1. Usecase Types and Function

- [ ] 1.1 Add `InitProductInput` record type to `PortfolioUsecase.fs`
- [ ] 1.2 Add `initProduct` function signature and stub to `PortfolioUsecase.fs`
- [ ] 1.3 Implement `InvalidProductId` validation via `ProductId.tryCreate`
- [ ] 1.4 Implement path existence check via `IFileSystem.DirectoryExists`
- [ ] 1.5 Implement `product.yaml` existence guard via `IFileSystem.FileExists`
- [ ] 1.6 Implement `product.yaml` content generation (primary-repo and standalone modes)
- [ ] 1.7 Write `product.yaml` via `IFileSystem.WriteFile`
- [ ] 1.8 Write coordination directory sentinel (`<coordPath>/.gitkeep`) via `IFileSystem.WriteFile`
- [ ] 1.9 Write `PRODUCT.md` template with product id substituted
- [ ] 1.10 Write `ARCHITECTURE.md` template with product id substituted
- [ ] 1.11 Delegate to `registerProduct` when `RegisterProfile` is `Some`; return `Some updatedPortfolio`
- [ ] 1.12 Return `None` when `RegisterProfile` is `None`

## 2. CLI Extension

- [ ] 2.1 Add `ProductInitArgs` Argu DU to `Program.fs`
- [ ] 2.2 Extend `ProductArgs` DU with `Init of ParseResults<ProductInitArgs>` case
- [ ] 2.3 Implement CLI handler for `Init` case: collect inputs, resolve registration target
- [ ] 2.4 Add interactive prompt for missing `id` (`AnsiConsole.Ask "Product id:"`)
- [ ] 2.5 Add interactive prompt for missing `repo-id` (`AnsiConsole.Ask "Repo id (default: same as id):"`)
- [ ] 2.6 Add interactive prompt for registration profile when neither `--register-profile` nor `--no-register` supplied
- [ ] 2.7 Print success message `"Initialized product '<id>' at <path>."` on success
- [ ] 2.8 Print `"Registered in profile '<profile>'."` when registration occurred

## 3. Error Formatting

- [ ] 3.1 Verify `formatPortfolioError` covers `InvalidProductId` case
- [ ] 3.2 Verify `formatPortfolioError` covers `ProductConfigError(root, msg)` case

## 4. Communication Tests

- [ ] 4.1 Test: `initProduct` with valid inputs and `RegisterProfile = Some` writes all files and returns `Some updatedPortfolio`
- [ ] 4.2 Test: `initProduct` with `RegisterProfile = None` writes files and returns `None`
- [ ] 4.3 Test: `initProduct` when `product.yaml` already exists returns `ProductConfigError`; no files written
- [ ] 4.4 Test: `initProduct` when path directory does not exist returns `ProductConfigError`
- [ ] 4.5 Test: `initProduct` with invalid id returns `InvalidProductId`
- [ ] 4.6 Test: `initProduct` with `CoordinationMode = "standalone"` writes yaml without `coordination.repo`

## 5. Acceptance Tests

- [ ] 5.1 End-to-end: `initProduct` creates all expected files with correct content; `itr.json` updated when registered
- [ ] 5.2 Skip registration: `itr.json` unchanged when `RegisterProfile = None`
- [ ] 5.3 Duplicate product root: second init with same profile returns error; `itr.json` unchanged

## 6. Verification

- [ ] 6.1 Run `dotnet build` — no errors
- [ ] 6.2 Run `dotnet test` — all tests pass
