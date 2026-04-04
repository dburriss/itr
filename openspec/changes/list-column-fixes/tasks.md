## 1. task list - Remove backlog field

- [x] 1.1 Remove `backlog` field from `task list` JSON output in `Program.fs` (line 564)
- [x] 1.2 Remove `backlog` column from `task list` text output in `Program.fs` (line 579)
- [x] 1.3 Remove `Backlog` column from `task list` table output in `Program.fs` (line 583)

## 2. profile list - Fix text delimiter

- [x] 2.1 Change `profile list --output text` delimiter from `|` to `\t` in `Program.fs` (line 1138)

## 3. Update tests

- [x] 3.1 Update `task list text output contains tab-separated fields` test in `tests/acceptance/OutputFormatTests.fs` (line 74) — remove `backlog` field, update column count from 5 to 4 (now 5 fields not 6), update field index assertions
- [x] 3.2 Update `text output has no ANSI sequences` test in `tests/acceptance/OutputFormatTests.fs` (line 292) — remove `backlog` from the format string used in that test

## 4. Verify

- [x] 4.1 Run `dotnet build` and confirm no errors
- [x] 4.2 Run `dotnet test` and confirm all tests pass
