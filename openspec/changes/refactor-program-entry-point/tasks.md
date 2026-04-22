## 1. OutputFormat type and per-type formatters

- [ ] 1.1 Create `src/adapters/OutputFormat.fs` defining the `OutputFormat` DU (`Json | Text | Table`) and `parseOutputFormat` function
- [ ] 1.2 Create `src/adapters/TaskFormatter.fs` with `format` function covering Json, Text, and Table cases, matching existing inline output exactly
- [ ] 1.3 Create `src/adapters/BacklogFormatter.fs` with `format` function covering Json and Table cases, matching existing inline output exactly
- [ ] 1.4 Create `src/adapters/PortfolioFormatter.fs` with `format` function covering Json and Text cases, matching existing inline output exactly
- [ ] 1.5 Add new formatter files to `Itr.Adapters.fsproj` in correct order (OutputFormat first)

## 2. ProductLocator adapter

- [ ] 2.1 Create `src/adapters/ProductLocator.fs` with `locateProductRoot` that walks up directories to find `product.yaml`, accepting an `IFileSystem` abstraction
- [ ] 2.2 Add `ProductLocator.fs` to `Itr.Adapters.fsproj`

## 3. AgentHarnessSelector adapter

- [ ] 3.1 Create `src/adapters/AgentHarnessSelector.fs` with `selectHarness` that dispatches on protocol string (`"acp"` / `"opencode-http"`) and returns `IAgentHarness`
- [ ] 3.2 Add `AgentHarnessSelector.fs` to `Itr.Adapters.fsproj`

## 4. Move string parsing helpers out of Program.fs

- [ ] 4.1 Create `src/cli/CliParsers.fs` (or add to existing helpers file) with `tryParseTaskState`, `tryParseBacklogItemStatus`, `taskStateToDisplayString`, `backlogItemStatusToString`
- [ ] 4.2 Remove duplicates from `Program.fs` and update all call sites to use the new module

## 5. Simplify dispatch function

- [ ] 5.1 Define `resolvePortfolio` helper in `Program.fs` extracting the repeated `load → resolveActiveProfile` boilerplate
- [ ] 5.2 Define ~16 partial active patterns (e.g. `BacklogTake`, `TaskPlan`, `ProductInit`) each flattening one Argu command path to the leaf `ParseResults`
- [ ] 5.3 Replace the nested `TryGetResult` body of `dispatch` with a flat `match results with` of ~40 lines using the active patterns

## 6. Update handlers to use new adapters

- [ ] 6.1 Update `handleTaskList`, `handleTaskInfo`, `handleTaskPlan`, `handleTaskApprove` to call `TaskFormatter` instead of inline formatting
- [ ] 6.2 Update backlog read handlers to call `BacklogFormatter` instead of inline formatting
- [ ] 6.3 Update portfolio/profile handlers to call `PortfolioFormatter` instead of inline formatting
- [ ] 6.4 Replace inline `product.yaml` traversal in `handleProductInfo` with `ProductLocator.locateProductRoot`
- [ ] 6.5 Replace inline harness selection in `handleTaskPlan` with `AgentHarnessSelector.selectHarness`
- [ ] 6.6 Replace `outputJson: bool` parameters on write handlers with `OutputFormat` for consistency

## 7. Tests

- [ ] 7.1 Write golden tests (Verify) for `TaskFormatter` covering all output formats
- [ ] 7.2 Write golden tests (Verify) for `BacklogFormatter` covering all output formats
- [ ] 7.3 Write unit tests for `ProductLocator` using an in-memory `IFileSystem`
- [ ] 7.4 Write unit tests for `AgentHarnessSelector` covering acp, opencode-http, and unknown protocol
- [ ] 7.5 Fix any broken import paths in existing tests caused by helper moves

## 8. Verify

- [ ] 8.1 Run `dotnet build` — zero errors
- [ ] 8.2 Run `dotnet test` — all tests pass
