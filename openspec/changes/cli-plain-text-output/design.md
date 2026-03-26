## Context

The CLI currently has two output modes for list and info commands: `table` (default) and `json`. These modes are driven by an `outputJson: bool` value derived from the `--output` argument. There is no plain-text mode, meaning shell scripts that want to extract field values must parse JSON (requiring `jq` or similar) or screen-scrape the decorated table output.

All changes are contained to `src/cli/Program.fs`. No domain types, adapters, or infrastructure are involved.

## Goals / Non-Goals

**Goals:**
- Add `--output text` as an accepted value on `backlog list`, `backlog info`, `task list`, and `task info`.
- Emit tab-separated, decoration-free output suitable for UNIX pipelines.
- Replace the `outputJson: bool` derivation in the four list/info handlers with a typed `OutputFormat` discriminated union.

**Non-Goals:**
- Adding text mode to write commands (`backlog take`, `backlog add`, `product register`).
- Adding a header row to text output.
- Exposing prose fields (`summary`, `acceptanceCriteria`) in text mode.
- Changing the JSON or table output formats.

## Decisions

**Use a discriminated union instead of a string or bool**
- `type OutputFormat = Table | Json | Text` replaces the `outputJson: bool` pattern.
- Rationale: adding a third case to `bool` is not possible; a DU makes the pattern-match exhaustive and self-documenting. A plain `string` would require guards everywhere.
- Alternative considered: keep `bool` for `outputJson` and add a second `bool` for `outputText`. Rejected: combinatorial explosion, no exhaustiveness checking.

**Keep write-command handlers using `outputJson: bool`**
- `handleBacklogTake`, `handleBacklogAdd`, `handleProductRegister` are not modified.
- Rationale: minimise diff noise; these handlers are not scripting targets. The asymmetry is acceptable at MVP and will be noted in a comment.

**No header row in text mode**
- Consistent with UNIX tool conventions (`ps`, `ls -1`). Scripts can address columns by index; a header would require `tail -n +2` to skip.

**Omit prose fields from text mode**
- `summary` and `acceptanceCriteria` are free-form text that may contain tabs or newlines, breaking the one-field-per-line contract. `--output json` is the authoritative full-content format.

**Unknown `--output` values fall back to `table`**
- Existing behaviour preserved. `parseOutputFormat` returns `Table` for any unrecognised string.

## Risks / Trade-offs

- **Merge conflicts** → Changes are isolated to `Program.fs`. Coordinate with any in-flight branches touching that file.
- **Tab characters in field values** → itr ids and states are controlled strings (no tabs possible); title fields are not emitted in list text mode. Accepted risk for MVP.
- **Inconsistency between list/info handlers (DU) and write handlers (bool)** → Documented with a comment; acceptable for MVP.
