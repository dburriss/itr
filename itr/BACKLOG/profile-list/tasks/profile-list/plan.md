Let me explore the codebase to understand the existing patterns and structure.
# List profiles

**Task ID:** profile-list
**Backlog Item:** profile-list
**Repo:** itr

## Description

List the profiles registerd in itr.json. Should return return git name, git details, and number of products.

## Scope

**Included:**
- New `profile list` subcommand under existing `profile` command
- Output format options: table (default), json, text
- Display profile name (with `*` marker for default), git identity (name/email), and product count
- Integration with existing portfolio loading infrastructure

**Excluded:**
- Profile creation/modification (handled by `profile add`)
- Filtering or sorting options (future enhancement)
- Profile deletion (handled by `profile remove`)
- Interactive selection of profiles

## Steps

1. Add `ProfileListArgs` discriminated union case to `ProfileArgs` in `src/cli/Program.fs`
2. Create `ProfileListArgs` type with `Output` option for format selection
3. Add handler function `handleProfileList` that:
   - Loads portfolio using existing infrastructure
   - Maps profiles to display format (name, git identity, product count)
   - Handles the three output formats (table, json, text)
4. Add dispatch case in profile command handler to route to `handleProfileList`
5. Add integration test for profile list command
6. Run `dotnet build` and `dotnet test` to verify

## Dependencies

- none

## Acceptance Criteria

- Should support json, text, and table(default) outputs
- `itr profile list` displays all profiles with name, git identity, and product count
- Default profile marked with `*` in table and text output
- Empty portfolio shows empty list (no error)
- Invalid format option falls back to table output

## Impact

**Files Changed:**
- `src/cli/Program.fs` - Add ProfileListArgs, handleProfileList, dispatch case
- `tests/acceptance/PortfolioAcceptanceTests.fs` - Add acceptance test

**Interfaces Affected:**
- CLI surface: new `profile list` subcommand
- No API changes

**Data Migrations:**
- None

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Profile with no git identity | Low | Display "None" or empty string | Handle `None` case in formatting |
| Empty portfolio | Low | Should show empty list, not error | Already handles via Portfolio.loadPortfolio |
| Format argument parsing | Low | Wrong format falls back to table | Already handles via parseOutputFormat pattern |

## Open Questions

- Should the table output include columns for all git identity fields (name, email) or just a combined "git identity" column? Both.
- Should product count be the only product detail, or should we also show a truncated product list? Only count.
- Should there be a `--quiet` or `--format-only` option to output just the profile names for scripting? No, can be done with `--output text` and parsing.