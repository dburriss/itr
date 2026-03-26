## Why

The CLI currently supports `--output table` and `--output json`, but there is no plain-text mode for scripting with standard UNIX tools (`awk`, `cut`, `grep`). Adding `--output text` makes list and info commands composable in shell pipelines without requiring JSON parsing.

## What Changes

- `--output text` becomes a valid value on `backlog list`, `backlog info`, `task list`, and `task info`.
- Text mode emits one record (or one field) per line with tab-separated values and no ANSI decoration.
- Introduce an `OutputFormat` discriminated union (`Table | Json | Text`) replacing the `outputJson: bool` derivation in the four list/info handlers.
- Update help strings for the four affected Argu `Output` cases.
- Write-command handlers (`take`, `add`, `product register`) are excluded and keep their existing `outputJson: bool` parameter unchanged.

## Capabilities

### New Capabilities

- `cli-plain-text-output`: Plain-text (`--output text`) output mode for all list and info commands, producing tab-separated lines suitable for UNIX pipeline processing.

### Modified Capabilities

- `backlog-list`: `--output` now accepts `text` in addition to `table` and `json`.
- `backlog-info`: `--output` now accepts `text` in addition to `table` and `json`.
- `task-list`: `--output` now accepts `text` in addition to `table` and `json`.
- `task-info`: `--output` now accepts `text` in addition to `table` and `json`.

## Impact

- `src/cli/Program.fs`: Only file modified. Add DU + helper; update 4 handlers; update 4 help strings.
- No domain types, adapters, or infrastructure changes required.
- Existing `--output table` and `--output json` behaviour is preserved.
- Unknown output values (e.g. `--output csv`) continue to fall back to `table`.
