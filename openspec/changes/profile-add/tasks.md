## 1. Domain Layer

- [x] 1.1 Add `InvalidProfileName of value: string * rules: string` case to `PortfolioError` in `Domain.fs`
- [x] 1.2 Add `ProfileName` module with `tryCreate` function validating slug rule `[a-z0-9][a-z0-9-]*` in `Domain.fs`
- [x] 1.3 Add `ProfileName.tryCreate` unit tests (valid slugs, blank, uppercase, spaces) in `PortfolioDomainTests.fs`

## 2. Interface Extension

- [x] 2.1 Add `SaveConfig: path: string -> portfolio: Portfolio -> Result<unit, PortfolioError>` to `IPortfolioConfig` in `Interfaces.fs`

## 3. Adapter Migration

- [x] 3.1 Migrate `readConfig` in `PortfolioAdapter.fs` to accept `IFileSystem` instead of calling `System.IO` directly
- [x] 3.2 Migrate `writeConfig` in `PortfolioAdapter.fs` to accept `IFileSystem` instead of calling `System.IO` directly
- [x] 3.3 Update `PortfolioConfigAdapter` constructor to accept both `IEnvironment` and `IFileSystem`
- [x] 3.4 Implement `SaveConfig` on `PortfolioConfigAdapter` delegating to the migrated `writeConfig`
- [x] 3.5 Run `dotnet test` to confirm existing tests pass after migration

## 4. Acceptance Test Guard

- [x] 4.1 Update `TestDeps` in `PortfolioAcceptanceTests.fs` to pass `IFileSystem` to `PortfolioConfigAdapter`
- [x] 4.2 Add round-trip acceptance test: `writeConfig` then `readConfig` preserves `defaultProfile` and all profiles

## 5. Usecase

- [x] 5.1 Add `addProfile` function to `PortfolioUsecase.fs` with signature `EffectResult<#IPortfolioConfig, Portfolio, PortfolioError>`
- [x] 5.2 Add `addProfile` unit tests: new name returns updated portfolio; duplicate returns `DuplicateProfileName`; `setAsDefault = true` updates `DefaultProfile`; invalid name returns `InvalidProfileName` in `PortfolioDomainTests.fs`

## 6. CLI Wiring

- [x] 6.1 Add `ProfilesAddArgs` DU with `Name`, `Git_Name`, `Git_Email`, `Set_Default` cases to `Program.fs`
- [x] 6.2 Add `ProfilesArgs` DU with `Add of ParseResults<ProfilesAddArgs>` to `Program.fs`
- [x] 6.3 Add `Profiles of ParseResults<ProfilesArgs>` case to `CliArgs` DU in `Program.fs`
- [x] 6.4 Update `AppDeps` construction to pass `IFileSystem` to `PortfolioConfigAdapter` and delegate `SaveConfig`
- [x] 6.5 Add `profiles add` dispatch handler in `Program.fs` (bootstrap, parse args, validate git-email/git-name, call usecase, persist, print result)
- [x] 6.6 Update `formatPortfolioError` in `Program.fs` to handle `DuplicateProfileName` and `InvalidProfileName`

## 7. End-to-End Acceptance Tests

- [x] 7.1 Add acceptance test: `profiles add` writes new profile to `itr.json`
- [x] 7.2 Add acceptance test: `--set-default` updates `defaultProfile`
- [x] 7.3 Add acceptance test: duplicate name returns error and file is unchanged
- [x] 7.4 Add acceptance test: existing profiles are not altered after add
- [x] 7.5 Add acceptance test: `--git-email` without `--git-name` returns validation error

## 8. Verification

- [x] 8.1 Run `dotnet build` and confirm no errors
- [x] 8.2 Run `dotnet test` and confirm all tests pass
