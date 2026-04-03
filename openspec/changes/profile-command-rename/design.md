## Context

The `itr` CLI uses Argu for argument parsing. Management subcommands are modeled as discriminated union cases in `CliArgs`. The subcommand for profile management was named `profiles` (plural), inconsistent with other subcommands which use singular nouns. This is a pure rename with no behavior change.

## Goals / Non-Goals

**Goals:**
- Rename the CLI surface from `itr profiles add` to `itr profile add`
- Rename the Argu types to match (`ProfilesArgs` → `ProfileArgs`, `ProfilesAddArgs` → `ProfileAddArgs`, `Profiles` case → `Profile`)
- Update all documentation and test references
- Keep the underlying domain model (`portfolio.Profiles`) unchanged

**Non-Goals:**
- Changing any profile behavior or business logic
- Modifying the `itr.json` data format
- Updating archived OpenSpec change documents

## Decisions

**Decision: Rename DU case from `Profiles` to `Profile`**
- The `CliArgs` DU already has `Profile of string` for the `-p`/`--profile` flag. Argu differentiates cases by type signature, not just name, so `Profile of ParseResults<ProfileArgs>` is distinct from `Profile of string`. This is safe.

**Decision: Leave domain model (`portfolio.Profiles`) unchanged**
- The `Profiles` property on the portfolio data model is unrelated to the CLI subcommand name. Renaming it would be a broader, riskier change with no user-facing benefit.

## Risks / Trade-offs

- **Naming collision in DU** → Argu resolves by type, not name. Both `Profile of string` and `Profile of ParseResults<ProfileArgs>` can coexist. Verified as safe.
- **User-facing breaking change** → Any scripts using `itr profiles add` will break. Acceptable: this is an early-stage tool with no known external consumers.

## Migration Plan

1. Rename types in `src/cli/Program.fs`
2. Update docs, tests, and specs
3. Build and test to verify no regressions
4. No rollback strategy needed - pure rename, no data changes
