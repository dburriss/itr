## Why

Users need a way to change the default profile in their portfolio config without editing `itr.json` directly. The `profile add` command supports `--set-default` at creation time, but there is no dedicated command to update the default for an existing profile.

## What Changes

- Add `itr profile set-default <name>` subcommand
- Add `--local` flag to write `defaultProfile` to `<productRoot>/itr.json` (creates the file if it does not exist)
- Add `--global` flag to write `defaultProfile` to `~/.config/itr/itr.json`
- Auto-detect the appropriate config file when neither flag is given (local precedence over global)
- Add `setDefaultProfile` usecase function in the Portfolio module
- Fix existing `ProfileNotFound` case in `formatPortfolioError` which currently falls through to debug format

## Capabilities

### New Capabilities
- `profile-set-default`: Set an existing named profile as the default via `itr profile set-default <name>`, with `--local`, `--global`, or smart auto-detection flags

### Modified Capabilities
- `profile-resolution`: The `defaultProfile` field can now be written by a dedicated command; no requirement changes, implementation only

## Impact

- `src/cli/Program.fs` — new CLI arg types and command dispatch
- `src/features/Portfolio/PortfolioUsecase.fs` — new `setDefaultProfile` usecase function
- `tests/communication/PortfolioDomainTests.fs` — new unit tests
- `tests/acceptance/PortfolioAcceptanceTests.fs` — new acceptance tests
