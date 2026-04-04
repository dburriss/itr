## Context

Two CLI output commands have inconsistencies that affect TV integration consumers:

1. `task list` includes a `Backlog` column/field in table, text, and JSON output. This column is redundant for consumers who just want task-level data without the backlog grouping context.
2. `profile list --output text` uses `|` as a delimiter, inconsistent with the tab-delimited text output pattern used elsewhere and harder to process with standard Unix tools like `awk` and `cut`.

The changes are isolated to `src/cli/Program.fs` (output formatting) and `tests/acceptance/OutputFormatTests.fs` (test assertions).

## Goals / Non-Goals

**Goals:**
- Remove `backlog` field from `task list` output in all three formats (table, text, JSON)
- Change `profile list --output text` delimiter from `|` to `\t`
- Update tests to match new output formats

**Non-Goals:**
- No changes to `backlog list` command
- No changes to JSON schema versions or field names beyond removing `backlog`
- No changes to data model or domain types

## Decisions

### Remove backlog from all task list output formats
All three output formats (table, text, JSON) will drop the `backlog` field for consistency. A partial removal (e.g., only from table) would create confusing inconsistencies between formats.

### Tab delimiter for profile list text output
Change `printfn "%s | %s | %s | %s | %d"` to `printfn "%s\t%s\t%s\t%s\t%d"` at `Program.fs:1138`. Tab is already the convention for text output in `task list` and `backlog list`, making this consistent.

### Task list text output column order after removal
New order: `taskId\trepoId\tstate\ttaskYamlPath\tplanMdPath` (5 fields, down from 6). The `backlogId` field is simply omitted.

## Risks / Trade-offs

- **Breaking change for external scripts** → Consumers of `task list --output text` relying on column positions 2+ will break. Documented in proposal as an accepted breaking change.
- **Breaking change for profile list text consumers** → Scripts expecting `|` delimiter will need to update to tab. Documented as accepted.
- [Risk] Tests referencing column count or backlog field will fail → Mitigation: Update `OutputFormatTests.fs` assertions in the same PR.
