## Why

`Program.fs` has grown to contain significant non-wiring logic — inline JSON serialization, string parsing helpers, AI harness selection, and filesystem traversal — violating the entry point purity principle. Centralizing these concerns into adapter and formatter layers improves testability, reduces coupling, and makes the dispatch function legible.

## What Changes

- Extract JSON/text/table output formatting from all handlers into a dedicated `OutputFormatter` module (per-type: `TaskFormatter`, `BacklogFormatter`, etc.)
- Move string parsing helpers (`tryParseTaskState`, `tryParseBacklogItemStatus`, `backlogItemStatusToString`, `taskStateToDisplayString`) to the adapter/CLI layer
- Move AI harness selection logic (acp vs opencode-http protocol dispatch) from `handleTaskPlan` into a new `AgentHarnessSelector` adapter
- Move filesystem traversal (walk-up to find `product.yaml`) from `handleProductInfo` into a new `ProductLocator` adapter
- Simplify the `dispatch` function (~550 lines, 4–6 level `TryGetResult` nesting) using partial active patterns and a shared `resolvePortfolio` helper
- `Program.fs` retains only wiring, dependency composition, and dispatch logic

## Capabilities

### New Capabilities

- `output-formatter`: Centralized output formatting (JSON/text/table) for all read handlers via per-type formatter modules
- `product-locator`: Adapter that walks up the directory tree to locate `product.yaml`
- `agent-harness-selector`: Adapter that selects the appropriate `IAgentHarness` implementation based on configured protocol

### Modified Capabilities

<!-- No spec-level requirement changes — this is a code organisation refactor. Existing capabilities behave identically after the change. -->

## Impact

- `src/cli/Program.fs` — simplified; inline logic removed, handlers call external modules
- `src/adapters/OutputFormatter.fs` — new file
- `src/adapters/ProductLocator.fs` — new file
- `src/adapters/AgentHarnessSelector.fs` — new file
- `src/domain/Task.fs`, `src/domain/Backlog.fs` — additive helpers (no breaking changes)
- Test files may require import path updates
