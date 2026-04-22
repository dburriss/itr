# Refactor Program.fs entry point

**Task ID:** refactor-program-entry-point
**Backlog Item:** refactor-program-entry-point
**Repo:** itr

## Description

Program.fs currently contains significant non-wiring logic violating the entry point purity principle. This includes: JSON serialization built inline in each handler, string parsing helpers (tryParseTaskState, tryParseBacklogItemStatus), AI harness selection logic in handleTaskPlan, filesystem traversal in handleProductInfo, and output formatting (table/JSON/text) entirely inline in every handler. These concerns should be moved to appropriate adapter/formatter layers. See architecture-review-apr26.md Entry Point Purity section.

## Scope

**Included:**
- Extract JSON serialization logic from all handlers into a dedicated output formatter module/adapter
- Move string parsing helpers (tryParseTaskState, tryParseBacklogItemStatus, backlogItemStatusToString, taskStateToDisplayString) to appropriate types in domain or adapter
- Relocate AI harness selection logic (AcpAdapter vs OpenCodeAdapter protocol dispatch) from handleTaskPlan to an adapter layer
- Move filesystem traversal (walk up directories to find product.yaml) in handleProductInfo to an adapter
- Extract table/JSON/text output formatting from all handlers into a dedicated OutputFormatter module
- Program.fs contains only wiring and dispatch logic after refactor

**Explicitly Excluded:**
- Changes to Argu argument definitions (CliArgs, et al.) - these are CLI-specific and belong in entry point
- Changes to write-command handlers (handleBacklogTake, handleBacklogAdd, handleProductRegister) - noted as excluded from OutputFormat refactor at MVP in code comments
- Changes to error formatting functions (formatBacklogError, formatTaskError, formatPortfolioError) - these are entry-point concerns
- Refactoring composition root (AppDeps) - wiring stays in entry point
- Any changes to domain logic in src/domain/

## Steps

1. **Create OutputFormatter module** - New file `src/adapters/OutputFormatter.fs` with:
   - `OutputFormat` type defined there (move from Program.fs)
   - `parseOutputFormat` function
   - Formatters for each output type: `formatJson`, `formatText`, `formatTable`
   - Generic formatter functions that accept data and format option

2. **Move serialization helpers to domain/types** - Move to domain types:
   - `tryParseTaskState` -> Domain.Task or new Types module
   - `tryParseBacklogItemStatus` -> Domain.Backlog or new Types module  
   - `taskStateToDisplayString` -> Domain.Task
   - `backlogItemStatusToString` -> Domain.Backlog

3. **Create ProductLocator adapter** - New file `src/adapters/ProductLocator.fs` with:
   - Filesystem traversal logic (`tryFind` function currently in handleProductInfo)
   - Expose `locateProductRoot` function that walks up directories to find product.yaml

4. **Create AgentHarnessSelector adapter** - Refactor handleTaskPlan harness selection:
   - Move protocol dispatch logic (acp vs opencode-http) to adapter
   - Expose `selectHarness` function that returns appropriate IAgentHarness

5. **Simplify dispatch function using active patterns** - The `dispatch` function (`Program.fs:1798`, ~550 lines) uses deeply nested `TryGetResult` chains (4–6 levels). Replace with:
   - ~16 partial active patterns (e.g. `BacklogTake`, `TaskPlan`, `ProductInit`) each flattening one Argu command path from `ParseResults<CliArgs>` down to the leaf `ParseResults<XxxArgs>`
   - A shared `resolvePortfolio` helper extracting the repeated `load → resolveActiveProfile` boilerplate (~8 lines repeated 8+ times)
   - A flat `match results with | BacklogTake args -> ... | TaskPlan args -> ...` dispatch body (~40 lines)
   - Handler function bodies (`handleTaskList`, `handleBacklogTake`, etc.) remain untouched

6. **Update handlers to use new adapters** - Refactor each handler:
   - Replace inline JSON/text/table formatting with calls to OutputFormatter
   - Replace inline parsing with domain helper functions
   - Replace filesystem traversal with ProductLocator calls

7. **Verify build and tests pass** - Run `dotnet build` and `dotnet test`

## Dependencies

- none

## Acceptance Criteria

- JSON serialization is moved out of handlers into adapters or a formatter layer
- String parsing helpers (tryParseTaskState, tryParseBacklogItemStatus) are moved out of Program.fs
- AI harness selection logic (acp vs opencode-http protocol dispatch) is moved out of handleTaskPlan
- Filesystem traversal in handleProductInfo (walking up dirs to find product.yaml) is moved to an adapter
- Output formatting (table/JSON/text) is moved out of all handlers into a dedicated layer
- Program.fs contains only wiring and dispatch logic

## Impact

**Files changed:**
- `src/cli/Program.fs` - Refactored to remove inline logic, call external formatters
- `src/adapters/OutputFormatter.fs` - NEW FILE - Centralized output formatting
- `src/adapters/ProductLocator.fs` - NEW FILE - Filesystem traversal for product.yaml
- `src/domain/Task.fs` - Add TaskState parsing/display helpers
- `src/domain/Backlog.fs` - Add BacklogItemStatus parsing/display helpers
- Test files may need updates to match new module paths

**Interfaces affected:**
- Existing handler functions accept new formatter dependency or call global formatter
- Domain types get new module functions (non-breaking, additive)
- ProductLocator adds `locateProductRoot` to existing adapters

**No data migrations required** - This is refactoring of code organization, not data format changes.

## Risks

1. **Breaking existing tests** - Tests importing from Program.fs may break when helpers move. Mitigation: Re-export helpers from new locations or keep duplicates during transition.

2. **Circular dependencies** - Moving helpers to domain could create cycles if domain imports from adapters. Mitigation: Keep parsing helpers in adapters or create a shared Types module in domain with no inbound deps.

3. **Performance regression** - New formatter abstraction adds call overhead. Mitigation: Inline simple formatters or make them inline-able.

4. **Behavioral drift** - New formatters must produce identical output. Mitigation: Write golden tests comparing before/after output for each handler.

## Decisions

1. **Parsing helpers** — Stay in the adapter/CLI layer; they parse CLI input strings and don't belong in domain.

2. **OutputFormatter architecture** — Per-type formatters: separate `TaskFormatter`, `BacklogFormatter`, etc.

3. **Write handler backwards compatibility** — Replace `outputJson: bool` with `OutputFormat` enum for consistency, even though write handlers were initially out of scope.

4. **Testing strategy** — Golden tests using [Verify](https://github.com/VerifyTests/Verify) to compare before/after output for each handler.
