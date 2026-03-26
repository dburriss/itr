## 1. OutputFormat discriminated union

- [ ] 1.1 Add `type OutputFormat = Table | Json | Text` DU to `src/cli/Program.fs` (before Argu DUs)
- [ ] 1.2 Add `parseOutputFormat` helper that maps `Some "json"` → `Json`, `Some "text"` → `Text`, and any other value → `Table`

## 2. Update Argu help strings

- [ ] 2.1 Update `ListArgs.Output` help text to `"output mode: table (default) | json | text"`
- [ ] 2.2 Update `InfoArgs.Output` help text to `"output mode: table (default) | json | text"`
- [ ] 2.3 Update `TaskListArgs.Output` help text to `"output mode: table (default) | json | text"`
- [ ] 2.4 Update `TaskInfoArgs.Output` help text to `"output mode: table (default) | json | text"`

## 3. Refactor handlers to use OutputFormat

- [ ] 3.1 In `handleBacklogList`: replace `let outputJson = ...` with `let format = ... |> parseOutputFormat`; replace `if outputJson` branch with `match format with`
- [ ] 3.2 In `handleBacklogInfo`: same refactor as 3.1
- [ ] 3.3 In `handleTaskList`: same refactor as 3.1
- [ ] 3.4 In `handleTaskInfo`: same refactor as 3.1
- [ ] 3.5 Add a comment noting that write-command handlers (`handleBacklogTake`, `handleBacklogAdd`, `handleProductRegister`) intentionally retain `outputJson: bool`

## 4. Implement text output — task list

- [ ] 4.1 In `handleTaskList` Text branch: emit one line per task as `<id>\t<state>\t<repo>\t<backlog-id>`
- [ ] 4.2 When no tasks match filters, emit empty output (no lines, no "No tasks found." message) in text mode

## 5. Implement text output — task info

- [ ] 5.1 In `handleTaskInfo` Text branch: emit key-value lines `id`, `state`, `repo`, `backlog`, `branch` (use `-` when branch absent)

## 6. Implement text output — backlog list

- [ ] 6.1 In `handleBacklogList` Text branch: emit one line per item as `<id>\t<type>\t<status>\t<priority>\t<title>` (use `-` for absent priority)

## 7. Implement text output — backlog info

- [ ] 7.1 In `handleBacklogInfo` Text branch: emit key-value lines `id`, `type`, `status`, `priority`, `view`, `repos`, `createdAt`, `title`, `taskCount`, `dependencies`, `dependedOnBy`
- [ ] 7.2 Multi-value fields (`repos`, `dependencies`, `dependedOnBy`) are comma-joined; absent single-value fields use `-`

## 8. Acceptance tests

- [ ] 8.1 Add test: `task list text output contains tab-separated fields`
- [ ] 8.2 Add test: `backlog list text output contains tab-separated fields`
- [ ] 8.3 Add test: `task info text output contains key-value lines`
- [ ] 8.4 Add test: `backlog info text output contains key-value lines`
- [ ] 8.5 Add test: `text output has no ANSI sequences` (assert no ESC character in output)

## 9. Verify

- [ ] 9.1 Run `dotnet build` — zero errors
- [ ] 9.2 Run `dotnet test` — all tests pass
