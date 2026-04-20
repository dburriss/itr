# Backlog item type: refactor

**Task ID:** backlog-type-refactor
**Backlog Item:** backlog-type-refactor
**Repo:** itr

## Description

Add a new "Refactor" case to the BacklogItemType discriminated union, making it a first-class type alongside Feature, Bug, Chore, and Spike. This involves updating the type definition, parsing/serialization logic, error messages, CLI, TUI, sort ordering, and YAML adapter to properly handle the new type.

## Scope

### Included
- Adding `Refactor` case to the BacklogItemType DU in Domain.fs
- Updating `tryParse` to map "refactor" string to Refactor case
- Updating `toString` to map Refactor case to "refactor" string
- Updating error messages in Program.fs that list valid types
- Updating CLI argument descriptions for --item-type and --type filter
- Updating InteractivePrompts.fs type selection list
- Updating sort ordering in BacklogUsecase.fs to position Refactor after Chore and before Spike
- Updating all three YAML read paths in YamlAdapter.fs to properly handle "refactor" (fail explicitly instead of silently falling back to Feature)
- Adding unit tests for parse, stringify, and error message validation

### Excluded
- No database or data migration needed (YAML files can be updated independently)
- No changes to portfolio or task domain models

## Steps

1. **Add Refactor case to Domain.fs** - Add `| Refactor` to the BacklogItemType discriminated union at line 162
2. **Update tryParse in Domain.fs** - Add `"refactor" -> Ok Refactor` case to the match expression (line 276)
3. **Update toString in Domain.fs** - Add `| Refactor -> "refactor"` case (line 284)
4. **Update error messages in Program.fs** - Add "refactor" to all three locations listing valid types (lines 56, 76, 443)
5. **Update InteractivePrompts.fs** - Add "refactor" to the type selection array (line 132)
6. **Update typeOrder in BacklogUsecase.fs** - Add `| Refactor -> 3` and shift Spike to 4 (line 216)
7. **Update YamlAdapter.fs path 1** - Change error handling at line 331 to not silently fall back to Feature
8. **Update YamlAdapter.fs path 2** - Change error handling at line 401
9. **Update YamlAdapter.fs path 3** - Change error handling at line 572
10. **Run build** - Verify compilation succeeds
11. **Run tests** - Verify all tests pass

## Dependencies

- none

## Acceptance Criteria

- BacklogItemType DU in Domain.fs gains a Refactor case alongside Feature | Bug | Chore | Spike
- BacklogItemType.tryParse maps "refactor" to Ok Refactor
- BacklogItemType.toString maps Refactor to "refactor"
- The error message in Program.fs lists refactor as a valid type
- The CLI --item-type and --type filter accept "refactor"
- The interactive TUI prompt in InteractivePrompts.fs includes "refactor" in the selection list
- Sort ordering in BacklogUsecase.fs assigns Refactor a position after Chore and before Spike
- All three YAML read paths in YamlAdapter.fs handle "refactor" without silently falling back to Feature
- Unit tests cover "refactor" to Refactor parse, Refactor to "refactor" stringify, and updated error message
- Build is clean and all existing tests pass

## Impact

### Files Changed
- `src/domain/Domain.fs` - BacklogItemType DU and associated functions
- `src/cli/Program.fs` - Error messages and CLI argument descriptions
- `src/cli/InteractivePrompts.fs` - TUI type selection list
- `src/features/Backlog/BacklogUsecase.fs` - Sort ordering function
- `src/adapters/YamlAdapter.fs` - Three YAML parsing paths

### Interfaces Affected
- BacklogItemType.tryParse: adds "refactor" mapping
- BacklogItemType.toString: adds Refactor serialization
- CLI: --item-type and --type accept "refactor"
- TUI: type selection includes "refactor"

### Data Migrations
- None required; YAML files with "refactor" type will be accepted after implementation

## Risks

1. **Silent fallback behavior in YamlAdapter** - The current implementation silently falls back to Feature on parse errors. Changing this to fail explicitly could break existing YAML files with invalid types that were previously "working" by accident. Mitigation: This is actually desired behavior - invalid types should fail explicitly rather than being silently miscategorized.

2. **Test coverage gaps** - If unit tests don't exist for the new type, edge cases could be missed. Mitigation: Add specific tests for "refactor" parsing and serialization as part of implementation.

3. **Sort ordering compatibility** - Changing the typeOrder values shifts positions. Mitigation: The acceptance criteria explicitly specifies the position (after Chore, before Spike), so this is intentional.

## Open Questions

1. Should existing YAML files with invalid types that were silently falling back to Feature now fail validation, or should there be a migration path?
   **Decision:** Fail explicitly — invalid types should never silently succeed.

2. Is there a preference for how YamlAdapter should handle invalid types - fail with a specific error message, or coerce to a default?
   **Decision:** Fail with a structured Result error, consistent with the domain layer.
