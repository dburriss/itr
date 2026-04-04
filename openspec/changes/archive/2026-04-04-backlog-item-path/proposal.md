## Why

Tooling and scripts that consume `backlog list` or `backlog info` output must independently reconstruct the path to `item.yaml` in order to open or process the file. Exposing the absolute path directly removes this fragile coupling and enables direct file access from CLI output.

## What Changes

- Add `BacklogItem` module to `Domain.fs` with `itemDir` and `itemFile` path-construction helpers
- Add `ItrTask` module to `Domain.fs` with `taskDir`, `taskFile`, and `planFile` path-construction helpers
- All four `IBacklogStore` read methods return the resolved `item.yaml` path alongside the item (tuple return)
- Replace all inline `Path.Combine` call sites in `YamlAdapter.fs` and `Program.fs` with the new module helpers
- Add `Path: string` field to `BacklogItemSummary` and `BacklogItemDetail` domain types
- `backlog list` (text, JSON, table) exposes the absolute path as a new column/field
- `backlog info` (text, JSON, table) exposes the absolute path as a new field/row

## Capabilities

### New Capabilities

- None

### Modified Capabilities

- `backlog-list`: adds `path` field to all output formats (text 9th tab-separated column, JSON field, table column)
- `backlog-info`: adds `path` field to all output formats (text key-value line, JSON field, table row)

## Impact

- `src/domain/Domain.fs` — new modules and new fields on summary/detail types
- `src/domain/Interfaces.fs` — all four `IBacklogStore` read method signatures change (**BREAKING** internal interface)
- `src/adapters/YamlAdapter.fs` — path construction refactored; return types updated
- `src/features/Backlog/BacklogUsecase.fs` — destructure tuples from store calls; populate `Path` field
- `src/cli/Program.fs` — `AppDeps` delegators updated; inline path sites replaced; list/info output extended
- `src/cli/InteractivePrompts.fs` — discard path from `ListBacklogItems` result
- `tests/acceptance/TaskAcceptanceTests.fs` — update `LoadBacklogItem` call site to ignore path
