# Task List Filters Plan

## Scope

**Included:**

- Add `--exclude <status>` flag to `task-list` command to allow excluding specific task states (e.g., `--exclude archived`)
- Add `--order-by <field>` flag to `task-list` command supporting `created` and `state` values
- Remove implicit exclusion of archived tasks - by default `task-list` now shows all tasks including archived
- Modify `TaskUsecase.filterTasks` to support exclude filter logic
- Add ordering logic for task list results (by created date or by state/priority)
- Update acceptance tests for new filtering behavior

**Excluded:**

- Changes to other commands (backlog-list, backlog-add, etc.)
- Changes to internal task state transitions or domain logic
- Changes to the YamlAdapter or storage layer

## Steps

1. **Add new CLI arguments** in `src/cli/Program.fs`:
   - Add `Exclude` argument to `TaskListArgs` type (line ~225)
   - Add `Order_By` argument to `TaskListArgs` type
   - Update usage strings for both new arguments

2. **Update handleTaskList** in `src/cli/Program.fs`:
   - Parse the new `--exclude` and `--order-by` arguments
   - Remove the implicit archived exclusion (lines 562-566)
   - Pass exclude filter to the filtering logic
   - Apply ordering to filtered results before output

3. **Update TaskUsecase.filterTasks** in `src/features/Task/TaskUsecase.fs`:
   - Add `exclude: TaskState list` parameter to filterTasks function
   - Add logic to exclude tasks with states in the exclude list

4. **Add ordering logic**:
   - For `--order-by created`: sort by `CreatedAt` date
   - For `--order-by state`: sort by priority (using existing `priorityOrder` pattern from backlog usecase)

5. **Update acceptance tests** in `tests/acceptance/TaskListAcceptanceTests.fs`:
   - Add test for default behavior showing archived tasks
   - Add test for `--exclude archived` flag
   - Add test for `--order-by created` sorting
   - Add test for `--order-by state` sorting

6. **Run verification**: `dotnet build && dotnet test`

## Impact

- **Files Changed:**
  - `src/cli/Program.fs` - TaskListArgs type, handleTaskList function
  - `src/features/Task/TaskUsecase.fs` - filterTasks function signature and logic
  - `tests/acceptance/TaskListAcceptanceTests.fs` - new test cases

- **Interface Changes:**
  - CLI: adds `--exclude` and `--order-by` flags to `task list` command
  - API: `filterTasks` function signature changes to accept exclude list

- **Data Migrations:** None - no persistent data changes

- **Breaking Change:** Default behavior change - archived tasks now shown by default. Users who relied on hidden archived tasks need to use `--exclude archived` to get previous behavior.

## Risks

1. **Breaking change to default behavior** - Users expect archived to be hidden, but now it's shown. Mitigation: Document the change, migration path is explicit `--exclude archived` flag.

2. **Order-by state vs priority confusion** - The acceptance criteria mentions "order by state" should order by "priority regardless of type". This is ambiguous - clarify that ordering by state uses priority ordering (not alphabetical state order).

3. **Conflicting filters** - What happens if user provides both `--state planning` and `--exclude planning`? Should this be an error or allow the user to get empty results? Decision: Allow, it's explicit user intent.

## Open Questions (Resolved)

1. **Order-by state interpretation**: `--order-by state` sorts by priority order (highest to lowest), same as backlog-list - not alphabetically by state name.

2. **State values for --exclude**: `--exclude` accepts any valid task state (planning, planned, approved, in_progress, implemented, validated, archived), matching existing `--state` behavior.

3. **Default ordering**: Default order when no `--order-by` is specified is by `created_at`, oldest to youngest (ascending).