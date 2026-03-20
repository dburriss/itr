## Why

When a user runs any `itr` command on a clean machine with no config file present, the tool crashes with a raw F# `ConfigNotFound` error and exits with code 1. This degrades first-run experience and makes the tool appear broken. Fixing this now unblocks onboarding new users and aligns the config filename with the documented standard (`itr.json`).

## What Changes

- Auto-create a minimal default `itr.json` at the resolved global config path when it is absent, before any command runs.
- Rename the config file from `portfolio.json` → `itr.json` throughout the codebase to match `docs/config-files.md`.
- Parent directory (`~/.config/itr/`) is created automatically if it does not exist.
- Bootstrap is idempotent: running twice does not overwrite an existing file.
- A new structured error case `BootstrapWriteError` surfaces when the write fails instead of an unhandled exception.
- An informational message is printed only when a new file is created, directing users to run `itr init`.

## Capabilities

### New Capabilities

- `settings-bootstrap`: Auto-create a minimal default `itr.json` at first run when the config file is absent, print a one-time informational message, and surface a structured error if the write fails.

### Modified Capabilities

- `portfolio-config`: The config filename changes from `portfolio.json` to `itr.json`; the resolved path logic is otherwise unchanged.

## Impact

- `src/domain/Domain.fs` — new `BootstrapWriteError` case on `PortfolioError` DU
- `src/features/Portfolio/PortfolioUsecase.fs` — new `bootstrapIfMissing` function
- `src/adapters/PortfolioAdapter.fs` — hardcoded filename updated
- `src/adapters/Library.fs` — `WriteFile` ensures parent directory exists
- `src/cli/Program.fs` — bootstrap wired before `loadPortfolio`; error formatter extended
- `tests/acceptance/PortfolioAcceptanceTests.fs` — fixture strings updated from `portfolio.json` to `itr.json`
