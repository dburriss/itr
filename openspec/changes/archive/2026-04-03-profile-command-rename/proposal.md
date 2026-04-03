## Why

The CLI subcommand `profiles` uses a plural noun, while all other management subcommands use singular form. Renaming it to `profile` makes the CLI surface consistent and predictable (e.g., `itr profile add` aligns with similar patterns in the tool).

## What Changes

- CLI subcommand `itr profiles add` renamed to `itr profile add`
- Argu discriminated union type `Profiles` renamed to `Profile` in `CliArgs`
- Supporting types `ProfilesArgs` and `ProfilesAddArgs` renamed to `ProfileArgs` and `ProfileAddArgs`
- Documentation and test references updated to reflect the new command name

## Capabilities

### New Capabilities

<!-- None - this is a pure rename with no new capabilities introduced -->

### Modified Capabilities

- `profile-add`: Command surface changes from `itr profiles add` to `itr profile add`

## Impact

- `src/cli/Program.fs` - 3 type/case renames and ~10 handler reference updates
- `README.md` - Command examples updated
- `docs/cli-reference.md` - Section headings and command examples updated
- `tests/acceptance/PortfolioAcceptanceTests.fs` - Test names and comments updated
- `openspec/specs/profile-add/spec.md` - Command references updated
