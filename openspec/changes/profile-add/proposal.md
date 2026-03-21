## Why

Users need to manage multiple named profiles (e.g., work, personal) in `itr.json` to track different git identities and product sets. Without a `profiles add` command, profiles cannot be created programmatically - there is no way to set up a profile without manually editing the config file.

## What Changes

- Add `itr profiles add <name>` CLI command
- Add `ProfileName.tryCreate` validation (slug rule: `[a-z0-9][a-z0-9-]*`)
- Add `InvalidProfileName` error case to `PortfolioError`
- Extend `IPortfolioConfig` with `SaveConfig` member
- Migrate `readConfig`/`writeConfig` in `PortfolioAdapter` to use `IFileSystem` instead of `System.IO` directly
- Add `addProfile` usecase in `PortfolioUsecase.fs`
- Add `ProfilesAddArgs`, `ProfilesArgs`, `Profiles` DU cases to CLI argument parser
- Optional `--git-name`, `--git-email`, `--set-default` flags

## Capabilities

### New Capabilities
- `profile-add`: Add a named profile to the portfolio config, with optional git identity and default flag

### Modified Capabilities
- `portfolio-config`: `IPortfolioConfig` gains a `SaveConfig` member; `PortfolioAdapter` migrates file I/O to `IFileSystem`

## Impact

- `Domain.fs`: new `ProfileName` module, new `InvalidProfileName` error case
- `Interfaces.fs`: `IPortfolioConfig` extended with `SaveConfig`
- `PortfolioAdapter.fs`: `readConfig`/`writeConfig` migrate to `IFileSystem`; `PortfolioConfigAdapter` constructor updated
- `PortfolioUsecase.fs`: new `addProfile` function
- `Program.fs`: new Argu DUs, updated `AppDeps`, new handler, updated error formatting
- Tests: new acceptance and unit/integration tests for `addProfile` and `ProfileName.tryCreate`
