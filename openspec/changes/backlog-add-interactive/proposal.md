## Why

Users who want to add backlog items via `itr backlog add` must remember and provide all required fields as CLI arguments in one go. An `--interactive` flag would guide users through each field with prompts, making the command more accessible and reducing errors from forgotten or misspelled arguments.

## What Changes

- Add `--interactive` / `-i` flag to the `backlog add` command
- When `--interactive` is set, prompt the user for each field: backlog-id, title, type, priority, summary, repos, dependencies, and acceptance criteria
- Merge any explicitly provided CLI arguments as pre-filled defaults (skipping those prompts)
- Show a confirmation summary before creating the item
- Detect non-TTY environments and return a clear error when `--interactive` is used without a terminal
- `Backlog_Id` and `Title` in `AddArgs` become optional at parse time; validation moves to handler logic

## Capabilities

### New Capabilities

- `backlog-add-interactive`: Interactive guided prompting for `backlog add`, allowing users to fill in each field step-by-step via Spectre.Console prompts with validation and a confirmation summary before submission

### Modified Capabilities

- `backlog-item-create`: The `backlog add` command gains an optional `--interactive` flag; `Backlog_Id` and `Title` lose `Mandatory` attribute, requiring explicit runtime validation when not in interactive mode

## Impact

- `src/cli/Program.fs` — `AddArgs` DU, `handleBacklogAdd` function
- `src/cli/InteractivePrompts.fs` (new file) — Spectre.Console prompt logic
- `src/cli/Itr.Cli.fsproj` — compile list update
- `tests/acceptance/BacklogAcceptanceTests.fs` — new tests for prompt mapping
