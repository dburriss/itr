# Plan: cli-plain-text-output

**Status:** Draft

---

## Description

Add `--output text` as a valid output mode alongside the existing `table` and `json` modes. Text mode emits one item per line with fields separated by a tab character, with no decoration. This makes all list and info commands scriptable via `awk`, `cut`, `grep`, etc.

---

## Scope

### Commands receiving text mode

All five commands that currently advertise `--output table|json` get text mode:

| Command | Handler |
|---|---|
| `itr backlog list` | `handleBacklogList` |
| `itr backlog info <id>` | `handleBacklogInfo` |
| `itr task list` | `handleTaskList` |
| `itr task info <id>` | `handleTaskInfo` |
| `itr backlog take` / `backlog add` / `product register` | existing `outputJson: bool` handlers — **excluded from text mode** (they are write commands, not list/info; their output is a confirmation, not a queryable record set) |

The write commands (`take`, `add`, `product register`) already have limited output and are not the target of scripting workflows. Keep them out of scope for this task.

### 1. Introduce `OutputFormat` DU (Core or CLI)

Add a discriminated union to `src/cli/Program.fs` (top of file, before any Argu DUs):

```fsharp
type OutputFormat = Table | Json | Text
```

Add a helper:

```fsharp
let parseOutputFormat (s: string option) =
    match s with
    | Some "json"  -> Json
    | Some "text"  -> Text
    | _            -> Table
```

This replaces all `outputJson: bool` derived values for the four list/info commands.

### 2. Update Argu help text

For each of `ListArgs`, `InfoArgs`, `TaskListArgs`, `TaskInfoArgs`, update the `Output` case help string from:

```
"output mode: table (default) | json"
```
to:

```
"output mode: table (default) | json | text"
```

### 3. Text output format per command

**`task list` text output** — one task per line, tab-separated:

```
<id>\t<state>\t<repo>\t<backlog-id>
```

**`task info` text output** — one field per line, tab-separated `<key>\t<value>`:

```
id\t<id>
state\t<state>
repo\t<repo>
backlog\t<backlog-id>
branch\t<branch or ->
```

**`backlog list` text output** — one item per line, tab-separated:

```
<id>\t<type>\t<status>\t<priority>\t<title>
```

**`backlog info` text output** — one field per line, tab-separated:

```
id\t<id>
type\t<type>
status\t<status>
priority\t<priority or ->
view\t<view or ->
repos\t<repo1>,<repo2>
createdAt\t<yyyy-MM-dd>
title\t<title>
taskCount\t<n>
dependencies\t<dep1>,<dep2>
dependedOnBy\t<rev1>,<rev2>
```

Multi-value fields (`repos`, `dependencies`, `dependedOnBy`) are comma-joined on a single line. No section headers, no empty lines, no borders.

### 4. Refactor `outputJson: bool` to `OutputFormat` in handlers

Replace the `let outputJson = ...` pattern in each of the four handlers:

```fsharp
// before
let outputJson = listArgs.TryGetResult TaskListArgs.Output |> Option.exists (fun v -> v = "json")

// after
let format = listArgs.TryGetResult TaskListArgs.Output |> parseOutputFormat
```

Then replace each `if outputJson then ... else ...` with a `match format with Json -> ... | Text -> ... | Table -> ...`.

The three write-command handlers (`handleBacklogTake`, `handleBacklogAdd`, `handleProductRegister`) continue to receive `outputJson: bool` unchanged.

---

## Dependencies / Prerequisites

- `backlog-info` — complete (the `backlog info` command exists).
- `backlog-list`, `task-list`, `task-info` — all complete and in production.

No new domain types, interfaces, or adapter changes required.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/cli/Program.fs` | Add `OutputFormat` DU and `parseOutputFormat`; update 4 handlers; update 4 help strings |
| No other files | All changes are contained to the CLI entry point |

The `outputJson: bool` parameter on the three write-command handlers is intentionally left unchanged to minimise diff noise.

---

## Acceptance Criteria

- [ ] `--output text` is accepted without error on `backlog list`, `backlog info`, `task list`, `task info`.
- [ ] Text output is tab-separated; each item/field is one line.
- [ ] Text output contains no ANSI codes, borders, or alignment padding.
- [ ] `--output table` (or omitting `--output`) still produces the existing table output.
- [ ] `--output json` still produces the existing JSON output.
- [ ] An unknown value (e.g. `--output csv`) silently falls back to table (existing behaviour preserved).
- [ ] Multi-value fields in text mode are comma-joined on a single line.
- [ ] Output is suitable for piping: `itr task list --output text | awk -F'\t' '{print $1}'` works.

---

## Testing Strategy

### Acceptance tests

Add to existing acceptance test files:

1. `task list text output contains tab-separated fields` — run `itr task list --output text` against a fixture with known tasks; assert each line matches `<id>\t<state>\t<repo>\t<backlog>`.
2. `backlog list text output contains tab-separated fields` — analogous.
3. `task info text output contains key-value lines` — assert `id\t<id>` present, `state\t<state>` present, etc.
4. `backlog info text output contains key-value lines` — analogous.
5. `text output has no ANSI sequences` — assert no ESC character in output string.

No new communication or building tests required; the logic is entirely in the CLI output layer.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Renaming `outputJson` to `format` in all four handlers introduces merge conflicts | Changes are isolated to `Program.fs`; coordinate with any in-flight branches |
| Write-command handlers left as `bool` while list/info handlers use DU creates inconsistency | Acceptable for MVP; write-command output format is not a scripting target. Document the asymmetry in a comment |
| Tab characters in field values would corrupt the format | itr ids and states are controlled strings (no tabs possible); title/summary fields are not emitted in list text mode |

---

## Decisions

- **No header row** in text mode. Consistent with UNIX tool conventions (`ps`, `ls`); avoids the need to skip with `tail -n +2` in scripts.
- **Prose fields omitted from text mode.** `summary` and `acceptanceCriteria` are free-form text that breaks the one-field-per-line contract. Use `--output json` to retrieve full content. No `--verbose` or `--detail` flag is needed; `--output json` is the authoritative full-content format. This follows the CLIG guideline: use `--plain` (or equivalent) for scriptable output and `--json` for structured full-content output.
