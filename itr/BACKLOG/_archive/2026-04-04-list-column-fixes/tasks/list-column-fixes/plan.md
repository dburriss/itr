

# Fixes to list commands for tv integration

**Task ID:** list-column-fixes
**Backlog Item:** list-column-fixes
**Repo:** itr

## Description

`task-list` has id and backlog columns. Backlog column should be removed. `profile list` uses | instead of tab as the delimeter.

## Scope

### Included
- Remove `Backlog` column from `task list` table output (text and JSON should also remove backlog)
- Change `profile list --output text` delimiter from `|` to `\t` (tab)

### Excluded
- No changes to `backlog list` command
- No changes to JSON schema versions

## Steps

1. Remove `Backlog` column from `task list` table output in `Program.fs` (line 583)
2. Remove `backlog` field from `task list` JSON output in `Program.fs` (line 564)
3. Remove `backlog` column from `task list` text output in `Program.fs` (line 579)
4. Change `profile list --output text` delimiter from `|` to `\t` in `Program.fs` (line 1138)
5. Update `task list text output contains tab-separated fields` test in `tests/acceptance/OutputFormatTests.fs` (line 74) — remove `backlog` field, update column count from 5 to 4, update field index assertions
6. Update `text output has no ANSI sequences` test in `tests/acceptance/OutputFormatTests.fs` (line 292) — remove `backlog` from the 5-field format used in that test
7. Build and test to verify changes

## Dependencies

- none

## Acceptance Criteria

- `task-list` has id column and not backlog column
- profile list --output text uses tab not |

## Impact

- **Files changed:** `src/cli/Program.fs`, `tests/acceptance/OutputFormatTests.fs`
- **Interfaces affected:** `task list` CLI output (table, text, JSON), `profile list --output text`
- **Breaking change:** Any scripts parsing `task list` text output that rely on column positions will break (backlog column removed). Any scripts parsing `profile list --output text` expecting `|` delimiter will need updating.

## Risks

- **Low risk:** Simple string/column changes with no data migration
- **Mitigation:** Verify with existing tests and manual testing

## Open Questions

- ~~Should the JSON output also remove `backlog` field, or just the table/text display?~~ **Resolved: remove from all outputs (table, text, JSON)**
- ~~Does any existing test validate `task list` text output column count?~~ **Resolved: yes — `OutputFormatTests.fs` line 74 and line 292 both need updating (column count 5→4, remove backlog field)**