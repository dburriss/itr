# Set a profile as default

**Task ID:** profile-set-default
**Backlog Item:** profile-set-default
**Repo:** itr

## Description

Set the given profile as the default. Flags control whether local or global config is updated.

## Scope

**Included:**
- Add `profile set-default <name>` subcommand with `--local`, `--global` flags
- Add `setDefaultProfile` usecase function in Portfolio module
- Handle smart "auto" detection when no flag specified (local > global)
- Create local itr.json if `--local` specified and file doesn't exist

**Explicitly Excluded:**
- Creating new profiles (handled by `profile add`)
- Deleting or modifying profile properties (products, git identity, agent config)
- Shell profile integration (setting bash/zsh rc files)
- Setting `ITR_PROFILE` environment variable (child processes cannot modify parent shell environment)

## Steps

1. **Add `ProfileSetDefaultArgs` Argu type** (`src/cli/Program.fs`)
   - Add `--local` flag (sets defaultProfile in local itr.json)
   - Add `--global` flag (sets defaultProfile in global itr.json)

2. **Add `SetDefault` to `ProfileArgs`** (`src/cli/Program.fs`)
   - Add subcommand: `| [<CliPrefix(CliPrefix.None)>] SetDefault of ParseResults<ProfileSetDefaultArgs>`

3. **Add `setDefaultProfile` usecase** (`src/features/Portfolio/PortfolioUsecase.fs`)
   - Function to update `DefaultProfile` in a loaded portfolio
   - Support detecting which config file to update (auto mode: local > global)

4. **Add command dispatch for `profile set-default`** (`src/cli/Program.fs`)
   - Load portfolio from appropriate location based on flags
   - Validate profile exists (case-insensitive)
   - Save updated portfolio

5. **Fix `ProfileNotFound` formatting** (`src/cli/Program.fs`)
   - Add explicit case in `formatPortfolioError` instead of falling through to debug format

6. **Add tests** (`tests/communication/PortfolioDomainTests.fs` and `tests/acceptance/PortfolioAcceptanceTests.fs`)
   - Test set-default with `--local` and `--global` flags
   - Test auto-detection behavior

7. **Build and verify** - Run `dotnet build` and `dotnet test`

## Dependencies

- none

## Acceptance Criteria

- [x] If no flags it will set wherever the profile is coming from (local itr.json > global itr.json)
- [x] If --local, defaultProfile in a local itr.json (creates if does not exist)
- [x] If --global, defaultProfile in global itr.json

## Impact

**Files changed:**
- `src/cli/Program.fs` - Add CLI args and command dispatch
- `src/features/Portfolio/PortfolioUsecase.fs` - Add `setDefaultProfile` function
- `tests/communication/PortfolioDomainTests.fs` - Add unit tests
- `tests/acceptance/PortfolioAcceptanceTests.fs` - Add acceptance tests

**Interfaces affected:**
- None

**No data migrations required.** This is a new command that writes to existing config files.

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Local itr.json at product root may conflict with existing agent-only itr.json | Low | Low | Merge existing config rather than overwrite |
| Profile name validation differs between add and set-default | Low | Low | Reuse `ProfileName.tryCreate` for validation |

## Decisions

1. **What constitutes "local" for `--local`?** Use `<productRoot>/itr.json`. This is consistent with `LoadLocalConfig` in `PortfolioAdapter.fs` which already uses product root, not current working directory.

2. **Should `--local` auto-detect product root?** Error out if no product context can be resolved. Error message: `"Error: --local flag requires a product context. Run this command from within a product directory or specify --global instead."` This matches how other commands require a resolved product.

3. **Error handling when profile doesn't exist?** Always search case-insensitively using the existing `Portfolio.tryFindProfileCaseInsensitive` (`Domain.fs`). If still not found, error with: `"Profile '{name}' not found. Run 'profile add {name}' to create it."` Also fix the existing `ProfileNotFound` case in `formatPortfolioError` (`Program.fs`) which currently falls through to debug format.

4. **Output message?** Include the location in the success message to be explicit about what was changed:
   - `--local`: `"Profile '{name}' set as default. ({productRoot}/itr.json)"`
   - `--global`: `"Profile '{name}' set as default. (~/.config/itr/itr.json)"`
   - Auto (no flag): Same format, showing whichever location was updated based on precedence resolution.