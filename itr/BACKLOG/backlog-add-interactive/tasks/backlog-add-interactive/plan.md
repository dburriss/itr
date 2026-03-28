Now I have all the context needed. Here is the completed plan document:

# Adds --interactive flag to adding a backlog item

**Task ID:** backlog-add-interactive
**Backlog Item:** backlog-add-interactive
**Repo:** itr

## Description

When adding a backlog item, allow users to use an --interactive flag that prompts them for the necessary fields, 
including (title, description, priority, type, dependencies) in a guided manner. 
This would be an alternative to providing all fields as command-line arguments and would help users who prefer a more conversational input method.

## Scope

**Included:**

- Add an `--interactive` flag to the `AddArgs` Argu discriminated union
- When `--interactive` is passed, prompt the user for each field using Spectre.Console prompts (`TextPrompt`, `SelectionPrompt`, `MultiSelectionPrompt`)
- Pre-fill `repos` with the current repo when the product has a single repo
- Present `type` as a selection from valid `BacklogItemType` values (feature, bug, chore, spike)
- Present `priority` as a selection from valid priorities (low, medium, high)
- Present `dependencies` as a multi-select from existing backlog items (loaded via `IBacklogStore.ListBacklogItems`)
- Prompt for `backlog-id`, `title`, and `summary` as free-text inputs
- Prompt for `acceptance_criteria` with text input, with an option to add multiple criteria in a loop until the user indicates they are done
- Validate all inputs inline and display error messages before re-prompting or failing
- Allow `--interactive` to be combined with explicit CLI arguments (explicit args act as pre-filled defaults, skipping those prompts)

**Excluded:**

- No changes to the `backlog add` non-interactive flow (existing CLI argument behavior is preserved)
- No changes to the domain model, feature/use-case layer, or storage adapters
- No TUI (`src/tui/`) changes — this is a CLI-only feature
- No changes to MCP or server entry points

## Steps

1. Add `Interactive` flag to the `AddArgs` discriminated union in `src/cli/Program.fs` as `| Interactive` with `AltCommandLine("-i")` and usage text
2. Remove the `Mandatory` attribute from `Backlog_Id` and `Title` in `AddArgs` so they become optional when `--interactive` is used
3. Create a helper module or function (e.g., `InteractivePrompts.promptBacklogAdd`) in the CLI project that uses Spectre.Console to prompt for each field:
   - `backlog-id`: `TextPrompt<string>` with slug validation
   - `title`: `TextPrompt<string>` (required, non-empty)
   - `repo`: auto-filled for single-repo products; `SelectionPrompt` for multi-repo products
   - `type`: `SelectionPrompt` with choices `["feature"; "bug"; "chore"; "spike"]`
   - `priority`: `SelectionPrompt` with choices `["low"; "medium"; "high"]`
   - `summary`: `TextPrompt<string>` (optional, allow empty)
   - `dependencies`: `MultiSelectionPrompt` populated from `IBacklogStore.ListBacklogItems`, allow selecting zero or more
4. Update `handleBacklogAdd` to detect `--interactive` flag, and when present, call the interactive prompt function to collect missing fields, merging with any explicitly provided CLI arguments
5. Ensure validation errors (invalid backlog id, duplicate id, unknown repo, invalid type) are displayed as friendly messages and the user is re-prompted where appropriate
6. Add acceptance tests for the interactive flow (or at minimum, unit tests for the prompt-to-input mapping logic, since Spectre.Console interactive prompts are difficult to test end-to-end)
7. Run `dotnet build` and `dotnet test` to verify no regressions

## Dependencies

- backlog-item-create

## Acceptance Criteria

- repos field is pre-filled with current repo by default
- select type from a list of valid types (e.g., feature, bug, chore)
- select priority from a list of valid priorities (e.g., low, medium, high)
- allow entering dependencies as a multi-select from existing backlog items
- validate inputs and provide error messages for invalid entries

## Impact

**Files changed:**

- `src/cli/Program.fs` — Add `Interactive` case to `AddArgs` DU (around line 40); update `handleBacklogAdd` (around line 1146) to branch on `--interactive` and invoke interactive prompts; potentially adjust argument validation to allow missing mandatory args when `--interactive` is set
- `src/cli/InteractivePrompts.fs` (new file) — Module containing Spectre.Console prompt logic for the guided backlog-add flow; this keeps the interactive concerns separated from the main dispatch logic
- `src/cli/Itr.Cli.fsproj` — Add `InteractivePrompts.fs` to the compile list
- `tests/acceptance/BacklogAcceptanceTests.fs` — Add tests for the interactive prompt-to-input mapping logic (testing the pure parts; full interactive prompt testing may be limited)

**Interfaces affected:**

- `AddArgs` Argu DU gains a new case (`Interactive`), which changes CLI help output — `itr backlog add --help` will show the new `--interactive` / `-i` flag
- `Backlog_Id` and `Title` lose `Mandatory` attribute, so Argu no longer enforces their presence at parse time — the `handleBacklogAdd` function must validate their presence (either from CLI args or interactive prompts)

**Data migrations:**

- None. No changes to domain model, storage format, or YAML structure.

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Removing `Mandatory` from `Backlog_Id`/`Title` could allow non-interactive invocations without required fields | Medium | High | Add explicit validation in `handleBacklogAdd` that returns a clear error when `--interactive` is not set and required fields are missing |
| Spectre.Console interactive prompts are difficult to test in CI (no TTY) | High | Medium | Extract prompt logic into a function that returns a `CreateBacklogItemInput`; test the pure mapping/validation separately; gate interactive tests with a CI-skip attribute if needed |
| `ListBacklogItems` could return a large list, making the multi-select unwieldy | Low | Low | Consider limiting or sorting the dependency list; for now, display items sorted by ID |
| `BacklogItemType` or priority values could change without updating the prompt choices | Low | Medium | Derive prompt choices from `BacklogItemType` cases and a shared priority list rather than hardcoding strings |
| Piped/non-TTY input environments will fail on interactive prompts | Medium | Medium | Detect non-interactive terminal (check `Console.IsInputRedirected`) and return a clear error message suggesting the use of CLI arguments instead |

## Open Questions

1. Should there be a confirmation step at the end of the interactive flow showing a summary of all entered values before creating the item? yes, show a summary and ask for confirmation before proceeding with creation
. Should non-TTY environments (piped input, CI) produce an error when `--interactive` is used, or silently fall back to non-interactive mode? yes, produce an error to avoid confusion