## Why

The `BacklogItemType` discriminated union currently supports Feature, Bug, Chore, and Spike but lacks a first-class `Refactor` type. Users working on refactoring tasks must misuse existing types, leading to inaccurate categorization and misleading backlogs. Adding `Refactor` as a proper type makes the taxonomy complete and accurate.

## What Changes

- Add `Refactor` case to `BacklogItemType` DU in `Domain.fs`
- Update `tryParse` to map `"refactor"` string to `Refactor` case
- Update `toString` to serialize `Refactor` to `"refactor"` string
- Add `"refactor"` to error messages listing valid types in `Program.fs`
- Add `"refactor"` to CLI argument descriptions (`--item-type`, `--type`)
- Add `"refactor"` to TUI type selection list in `InteractivePrompts.fs`
- Add `Refactor` to sort ordering in `BacklogUsecase.fs` (after Chore, before Spike)
- Fix all three YAML read paths in `YamlAdapter.fs` to fail explicitly on unknown types instead of silently falling back to `Feature`
- Add unit tests for parse, stringify, and error message coverage

## Capabilities

### New Capabilities

- `backlog-item-type-refactor`: The `refactor` value is a valid backlog item type that can be set when creating/editing items, appears in filters and TUI selection, and is properly serialized to/from YAML

### Modified Capabilities

- `backlog-item-create`: The `--item-type` argument now accepts `refactor` as a valid value
- `backlog-list`: The `--type` filter now accepts `refactor` as a valid value

## Impact

- `src/domain/Domain.fs` — BacklogItemType DU and associated functions
- `src/cli/Program.fs` — Error messages and CLI argument descriptions
- `src/cli/InteractivePrompts.fs` — TUI type selection list
- `src/features/Backlog/BacklogUsecase.fs` — Sort ordering
- `src/adapters/YamlAdapter.fs` — Three YAML parsing paths
- `tests/` — New unit tests for the Refactor type
