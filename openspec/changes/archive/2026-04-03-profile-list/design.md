## Context

The `itr` CLI has an existing `profile` command group with a single `add` subcommand. The portfolio is loaded via `Portfolio.loadPortfolio` which returns a `Portfolio` record containing a `DefaultProfile` field and a `Profiles` map (`Map<ProfileName, Profile>`). Each `Profile` has a `Name`, `Products` list, optional `GitIdentity` (with `Name` and optional `Email`), and `AgentConfig`. Output formatting follows the established `OutputFormat` discriminated union (`TableOutput | JsonOutput | TextOutput`) with the shared `parseOutputFormat` helper. The pattern is already used in `handleBacklogList`, `handleTaskList`, etc.

## Goals / Non-Goals

**Goals:**
- Add a `profile list` subcommand following the existing CLI patterns
- Display all profiles with name (default marker `*`), git identity, and product count
- Support `table` (default), `json`, and `text` output formats
- Return an empty list for an empty portfolio without error

**Non-Goals:**
- Profile filtering, sorting, or search
- Displaying product names (only count)
- Interactive profile selection
- Any write operations

## Decisions

**Reuse `OutputFormat` / `parseOutputFormat`**
All other list commands use this shared type and parser. Consistency over inventing a new approach.

**No new domain types needed**
The display data (name, default marker, git name, git email, product count) can be composed inline in the handler from existing `Profile` and `Portfolio` types. A local record or tuple suffices; no module-level type needed.

**Dispatch via pattern match on `ProfileArgs`**
The `| None ->` branch in the profile command handler currently returns an error. Adding a `List` case to `ProfileArgs` and a `ProfileListArgs` type follows the same shape as `ProfileAddArgs`. The handler `handleProfileList` will match `ProfileArgs.List`.

**Spectre.Console table for table output**
Consistent with other list commands that render `Table` via `AnsiConsole.Write`.

## Risks / Trade-offs

- `Profile.GitIdentity` is optional → display "—" or empty string for missing fields; handled inline
- `Portfolio.DefaultProfile` is `ProfileName option` → compare by value to mark default
- JSON output should use `System.Text.Json` serialization of an anonymous record or a local type for predictable field names
