## Why

The `itr profile` command currently supports adding profiles but provides no way to list what profiles are configured. Users need to inspect the raw `itr.json` to see which profiles exist, making it difficult to identify the default profile or verify git identities and product counts.

## What Changes

- New `profile list` subcommand added to the existing `profile` command
- Outputs profile name (with `*` marker for default), git name, git email, and product count
- Supports `--output` flag with `table` (default), `json`, and `text` formats
- Empty portfolio shows an empty list without error

## Capabilities

### New Capabilities

- `profile-list`: Lists all profiles registered in the portfolio, showing name, git identity, product count, and default marker

### Modified Capabilities

<!-- none -->

## Impact

- `src/cli/Program.fs`: Add `ProfileListArgs` type, `handleProfileList` handler, dispatch case in profile command handler
- `tests/acceptance/PortfolioAcceptanceTests.fs`: Add acceptance test for profile list command
- No API changes; CLI surface only
