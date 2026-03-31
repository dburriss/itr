Now I have a complete understanding of the codebase. Let me write the plan document.
# Rename CLI subcommand profiles to profile

**Task ID:** profile-command-rename
**Backlog Item:** profile-command-rename
**Repo:** itr

## Description

Rename the `profiles` CLI subcommand to `profile` so all management subcommands use consistent singular noun form (e.g. `itr profile add`).

## Scope

**Included:**
- Rename CLI type definitions in `src/cli/Program.fs`:
  - `ProfilesAddArgs` → `ProfileAddArgs`
  - `ProfilesArgs` → `ProfileArgs`
  - `Profiles of ParseResults<ProfilesArgs>` → `Profile of ParseResults<ProfileArgs>` (in `CliArgs` DU)
- Update all references in `Program.fs` handler code
- Update CLI documentation in `docs/cli-reference.md` and `README.md`
- Update test names and comments in `tests/acceptance/PortfolioAcceptanceTests.fs`
- Update `openspec/specs/profile-add/spec.md` references

**Explicitly excluded:**
- `portfolio.Profiles` data structure in domain and adapters (config data, not CLI)
- `docs/config-files.md` references to `profiles` in itr.json structure
- Other specs and archived change documents

## Steps

1. Update `src/cli/Program.fs`:
   - Rename `ProfilesAddArgs` type to `ProfileAddArgs`
   - Rename `ProfilesArgs` type to `ProfileArgs`
   - Rename `Profiles of ParseResults<ProfilesArgs>` case in `CliArgs` to `Profile of ParseResults<ProfileArgs>`
   - Update all references in handler code (lines ~1484-1491)
   - Update `IArgParserTemplate` usage strings
   - Update the help text match case (`"profile management commands"`)

2. Update `README.md`:
   - Change `itr profiles add` to `itr profile add` in examples

3. Update `docs/cli-reference.md`:
   - Rename `### profiles` section heading to `### profile`
   - Change `#### profiles add` to `#### profile add`
   - Update command examples from `itr profiles add` to `itr profile add`

4. Update `tests/acceptance/PortfolioAcceptanceTests.fs`:
   - Update test function names from `profiles add...` to `profile add...`
   - Update section comments referencing `profiles add acceptance tests`

5. Update `openspec/specs/profile-add/spec.md`:
   - Replace all `itr profiles add` references with `itr profile add`

6. Build and run tests to verify changes

## Dependencies

- profile-add

## Acceptance Criteria

- The `profiles` Argu DU and all associated types are renamed to `profile` (singular)
- CLI surface changes from `itr profiles add` to `itr profile add`
- All tests, docs, and specs are updated to reflect the new command name
- Existing behavior is unchanged; only the command name changes

## Impact

**Files changed:**
- `src/cli/Program.fs` - Renamed 3 types/cases and updated ~10 references
- `README.md` - Updated 2 command examples
- `docs/cli-reference.md` - Updated section headings and command examples
- `tests/acceptance/PortfolioAcceptanceTests.fs` - Updated 6 test names and 2 comments
- `openspec/specs/profile-add/spec.md` - Updated ~10 `itr profiles add` references

**No data migrations required** - this is a pure rename with no persistence format changes.

## Risks

1. **Naming collision with existing `Profile of string` flag** - The `CliArgs` DU already has `Profile of string` for the `-p`/`--profile` option. However, since the new `Profile` case uses `CliPrefix(CliPrefix.None)` and takes `ParseResults<ProfileArgs>` (not a string), Argu will distinguish them by type signature. This is the same pattern already proven working with `Profiles`.

2. **OpenSpec spec updates** - The spec file is the canonical contract. Updating it ensures downstream consumers understand the new command surface.

## Open Questions

1. Should archived OpenSpec change documents (`openspec/changes/archive/...`) be updated? They are historical but may cause confusion. Recommendation: leave archived docs unchanged.