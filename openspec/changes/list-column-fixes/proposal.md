## Why

The `task list` command includes a `Backlog` column that is redundant for TV integration consumers, and the `profile list --output text` format uses `|` as a delimiter instead of tab, making it inconsistent with other text output commands and harder to process with standard tools like `awk`.

## What Changes

- Remove `Backlog` column from `task list` table output
- Remove `backlog` field from `task list` JSON output
- Remove `backlog` field from `task list` text (tab-separated) output
- Change `profile list --output text` delimiter from `|` to `\t` (tab)

## Capabilities

### New Capabilities
<!-- None - no new capabilities are being introduced -->

### Modified Capabilities
- `task-list`: Remove `backlog` field from all output formats (table, JSON, text). Text output column order changes from `taskId\tbacklogId\trepoId\tstate\ttaskYamlPath\tplanMdPath` to `taskId\trepoId\tstate\ttaskYamlPath\tplanMdPath`.
- `profile-list`: Change `--output text` format delimiter from `|` to `\t` (tab).

## Impact

- **Files changed:** `src/cli/Program.fs`, `tests/acceptance/OutputFormatTests.fs`
- **Interfaces affected:** `task list` CLI output (table, text, JSON), `profile list --output text`
- **Breaking change:** Scripts parsing `task list` text output relying on column positions (backlog column removed). Scripts parsing `profile list --output text` expecting `|` delimiter must update to tab.
