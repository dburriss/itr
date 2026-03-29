## Context

The `itr backlog add` command currently requires users to supply all fields as CLI arguments in a single invocation. This works well for scripted or experienced use, but is friction-heavy for exploratory or first-time use. The change introduces an `--interactive` flag that guides users through each field one at a time using Spectre.Console prompts.

The CLI project (`src/cli/Program.fs`) uses Argu for argument parsing and Spectre.Console for terminal output. The `AddArgs` discriminated union drives the `handleBacklogAdd` handler. Currently `Backlog_Id` and `Title` are `Mandatory` in Argu, which means they are validated at parse time—before we can check whether `--interactive` was passed.

## Goals / Non-Goals

**Goals:**
- Add `--interactive` / `-i` flag to `backlog add`
- When flag is set, prompt user for each field using Spectre.Console (TextPrompt, SelectionPrompt, MultiSelectionPrompt)
- Allow CLI args to act as pre-filled defaults (skip those prompts)
- Show confirmation summary before creating the item
- Return a clear error in non-TTY environments (piped input, CI)
- Keep non-interactive flow entirely unchanged

**Non-Goals:**
- No changes to domain model, feature layer, or storage adapters
- No TUI changes
- No MCP or server changes
- No bulk/batch interactive creation

## Decisions

### Decision: Remove `Mandatory` from `Backlog_Id` and `Title`

**Choice**: Remove `[<Mandatory>]` from both fields in `AddArgs` and enforce their presence at runtime in `handleBacklogAdd`.

**Rationale**: Argu validates mandatory fields before the handler runs, so there is no way to opt out of the check when `--interactive` is provided. Moving validation to the handler allows the interactive path to supply missing values from prompts, while the non-interactive path fails with a clear message.

**Alternative considered**: Keep `Mandatory` and add a separate `InteractiveArgs` subcommand. Rejected — separate subcommand fragments discoverability and duplicates the `handleBacklogAdd` dispatch logic.

### Decision: New `InteractivePrompts.fs` module

**Choice**: Extract all Spectre.Console prompt logic into a dedicated `src/cli/InteractivePrompts.fs` file exposing a single function `promptBacklogAdd`.

**Rationale**: Keeps `Program.fs` focused on dispatch and argument resolution. The prompting logic can be unit-tested in isolation (the function signature accepts an `IBacklogStore` and returns a `CreateBacklogItemInput`).

**Alternative considered**: Inline the prompts in `handleBacklogAdd`. Rejected — the handler is already large; mixing prompt logic makes it harder to read and test.

### Decision: Confirm-before-create summary

**Choice**: After all prompts are answered, display a summary table of all field values and ask for confirmation before invoking the create use case.

**Rationale**: Plan explicitly requires this. Reduces risk of accidental creation with wrong values.

### Decision: Non-TTY produces an error

**Choice**: When `--interactive` is used but `Console.IsInputRedirected` is true (or equivalent), return an error exit code with a message directing the user to use CLI arguments.

**Rationale**: Silently falling back to non-interactive mode would be confusing if required fields are missing. An explicit error is safer and matches the plan's decision.

## Risks / Trade-offs

- **Removing `Mandatory` widens the non-interactive error surface** → Mitigation: guard in `handleBacklogAdd` with a clear error before any I/O.
- **Spectre.Console prompts are untestable end-to-end in CI (no TTY)** → Mitigation: `promptBacklogAdd` returns a pure `CreateBacklogItemInput` record; test the mapping logic without a real terminal using dependency injection for the prompt functions or by stubbing.
- **Large `ListBacklogItems` result makes multi-select unwieldy** → Mitigation: sort items by ID; accept this limitation for now.
- **`BacklogItemType` values could diverge from prompt choices** → Mitigation: derive choices from the DU cases rather than hardcoding strings.
