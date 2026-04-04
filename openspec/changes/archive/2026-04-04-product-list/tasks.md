## 1. Expose loadAllDefinitions

- [x] 1.1 Change `loadAllDefinitions` in `src/features/Portfolio/PortfolioUsecase.fs` from `private` to `let` (public)

## 2. CLI Arg Types

- [x] 2.1 Add `ProfileProductsListArgs` Argu type in `src/cli/Program.fs` with optional `Profile` and `Output` arguments implementing `IArgParserTemplate`
- [x] 2.2 Add `Products` case to `ProfileArgs` discriminated union with `ParseResults<ProfileProductsListArgs>` and update `IArgParserTemplate` usage string

## 3. Handler Implementation

- [x] 3.1 Implement `handleProfileProductsList` function in `src/cli/Program.fs` accepting `configPath`, `portfolio`, `deps`, and `listArgs` parameters
- [x] 3.2 Resolve active profile using `Portfolio.resolveActiveProfile` (respecting `--profile` flag)
- [x] 3.3 Return error "No profiles found. Run 'itr profile add <name>' to create one." when portfolio has no profiles
- [x] 3.4 Load product definitions using the now-public `PortfolioUsecase.loadAllDefinitions`
- [x] 3.5 Implement table output with columns Id, Repo Count, Coord Root
- [x] 3.6 Implement text output: tab-delimited `id\trepoCount\tcoordRoot` per line
- [x] 3.7 Implement JSON output: array of objects with `id`, `repoCount`, `coordRoot` fields

## 4. Dispatch Wiring

- [x] 4.1 Add case for `ProfileArgs.Products` in the profile command dispatch in `src/cli/Program.fs` calling `handleProfileProductsList`

## 5. Acceptance Tests

- [x] 5.1 Add acceptance test: list products with explicit `--profile` flag
- [x] 5.2 Add acceptance test: list products using default profile (no flag)
- [x] 5.3 Add acceptance test: error when specified profile not found
- [x] 5.4 Add acceptance test: error when portfolio has no profiles
- [x] 5.5 Add acceptance test: empty product list is accepted without error
- [x] 5.6 Add acceptance test: `--output json` format
- [x] 5.7 Add acceptance test: `--output text` format
- [x] 5.8 Add acceptance test: `--output table` (default) format

## 6. Verification

- [x] 6.1 Run `dotnet build` and resolve any compile errors
- [x] 6.2 Run `dotnet test` and ensure all tests pass
