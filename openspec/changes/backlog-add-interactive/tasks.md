## 1. Prepare AddArgs

- [x] 1.1 Remove `[<Mandatory>]` attribute from `Backlog_Id` in `AddArgs` DU in `src/cli/Program.fs`
- [x] 1.2 Remove `[<Mandatory>]` attribute from `Title` in `AddArgs` DU in `src/cli/Program.fs`
- [x] 1.3 Add `| Interactive` case to `AddArgs` with `[<AltCommandLine("-i")>]` and appropriate CLI help text

## 2. Runtime Validation

- [x] 2.1 In `handleBacklogAdd`, add a guard that checks: if `--interactive` is NOT set and `Backlog_Id` is missing, return a clear error message
- [x] 2.2 In `handleBacklogAdd`, add a guard that checks: if `--interactive` is NOT set and `Title` is missing, return a clear error message

## 3. InteractivePrompts Module

- [x] 3.1 Create `src/cli/InteractivePrompts.fs` with module `InteractivePrompts`
- [x] 3.2 Add `InteractivePrompts.fs` to the `<Compile>` list in `src/cli/Itr.Cli.fsproj` before `Program.fs`
- [x] 3.3 Implement non-TTY detection: check `Console.IsInputRedirected` and return `Error` with a clear message when true
- [x] 3.4 Implement `backlog-id` prompt using `TextPrompt<string>` with slug validation (non-empty, no spaces)
- [x] 3.5 Implement `title` prompt using `TextPrompt<string>` (required, non-empty)
- [x] 3.6 Implement `type` prompt using `SelectionPrompt` with choices derived from `BacklogItemType` cases (`feature`, `bug`, `chore`, `spike`)
- [x] 3.7 Implement `priority` prompt using `SelectionPrompt` with choices `["low"; "medium"; "high"]`
- [x] 3.8 Implement `summary` prompt using `TextPrompt<string>` (optional, allow empty)
- [x] 3.9 Implement `repo` logic: auto-fill for single-repo products; `SelectionPrompt` from product repos for multi-repo products
- [x] 3.10 Implement `dependencies` prompt using `MultiSelectionPrompt` populated from `IBacklogStore.ListBacklogItems` sorted by id, allowing zero selections
- [x] 3.11 Implement `acceptance_criteria` loop: prompt for entries one at a time until user submits an empty entry
- [x] 3.12 Implement confirmation summary: display all collected field values and ask yes/no before returning
- [x] 3.13 Return `Ok CreateBacklogItemInput` on confirmation, `Error "Cancelled"` on rejection

## 4. Wire Interactive Flow into handleBacklogAdd

- [x] 4.1 In `handleBacklogAdd`, detect `--interactive` flag and call `InteractivePrompts.promptBacklogAdd`
- [x] 4.2 Merge explicit CLI args as pre-filled defaults (skip prompts for fields already provided)
- [x] 4.3 On `Error` from prompt function, print error message and exit with non-zero code
- [x] 4.4 On `Ok input`, pass the merged `CreateBacklogItemInput` to the existing create use case

## 5. Tests

- [x] 5.1 Add unit tests for the argument-merging logic (explicit args override interactive defaults)
- [x] 5.2 Add unit tests for non-TTY guard returning an appropriate error
- [x] 5.3 Add unit tests for missing `Backlog_Id` without `--interactive` returning an error
- [x] 5.4 Add unit tests for missing `Title` without `--interactive` returning an error

## 6. Verify

- [x] 6.1 Run `dotnet build` and confirm zero errors
- [x] 6.2 Run `dotnet test` and confirm all tests pass
