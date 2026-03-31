## 1. Domain

- [x] 1.1 Add `AgentConfig` type to `src/domain/Domain.fs` with fields `Protocol: string`, `Command: string`, `Args: string list`

## 2. Portfolio Config

- [x] 2.1 Add `AgentConfigDto` record to `src/adapters/PortfolioAdapter.fs` with `protocol`, `command`, `args` fields
- [x] 2.2 Add optional `agent` field to the profile DTO in `PortfolioAdapter.fs`
- [x] 2.3 Map `AgentConfigDto` to/from `AgentConfig` in load/save, applying defaults when absent
- [x] 2.4 Add `LoadLocalConfig: productRoot: string -> AgentConfig option` helper in `PortfolioAdapter.fs` that reads `<productRoot>/itr.json` and extracts the `agent` section if present

## 3. ACP Adapter

- [x] 3.1 Create `src/adapters/AcpAdapter.fs` with pure functions for JSON-RPC message construction: `buildInitialize`, `buildSessionNew`, `buildSessionPrompt`
- [x] 3.2 Add pure parsing functions: `extractSessionId` (from `session/new` response) and `extractChunkText` (from `session/update` messages)
- [x] 3.3 Implement `AcpHarnessAdapter(command, args, cwd)` that launches a subprocess with redirected stdin/stdout/stderr
- [x] 3.4 Implement the ACP message exchange loop: `initialize` → `session/new` → `session/prompt` → read chunks until final response → kill process
- [x] 3.5 Stream `agent_message_chunk` text to stdout as chunks arrive; accumulate full response
- [x] 3.6 Return `Ok accumulatedText` on success or `Error message` on timeout/subprocess failure
- [x] 3.7 Log stderr and unparseable stdout lines at debug level

## 4. Project Registration

- [x] 4.1 Add `AcpAdapter.fs` to `src/adapters/Itr.Adapters.fsproj` after `OpenCodeAdapter.fs` in compile order

## 5. CLI Wiring

- [x] 5.1 In `handleTaskPlan` in `src/cli/Program.fs`, load global `AgentConfig` from the active profile after `productDef` is resolved
- [x] 5.2 Call `LoadLocalConfig` with the product root and merge: local config wins over global when present
- [x] 5.3 Add adapter selection: `"acp"` → `AcpHarnessAdapter(config.Command, config.Args, coordRoot)`; default → existing `OpenCodeHarnessAdapter()`

## 6. Unit Tests

- [x] 6.1 Add unit tests for `buildInitialize` — verify JSON-RPC shape (method, id, jsonrpc fields)
- [x] 6.2 Add unit tests for `buildSessionNew` — verify `cwd` is included correctly
- [x] 6.3 Add unit tests for `buildSessionPrompt` — verify `sessionId` and prompt content block are present
- [x] 6.4 Add unit tests for `extractChunkText` — verify extraction from a valid `session/update` message and graceful handling of malformed input
- [x] 6.5 Add unit tests for `extractSessionId` — verify extraction from a valid `session/new` response and error on malformed input
- [x] 6.6 Add unit tests for `LoadLocalConfig` — file present with agent section returns `Some AgentConfig`; file absent returns `None`; file present but no agent section returns `None`

## 7. Verification

- [x] 7.1 Run `dotnet build` and confirm zero errors
- [x] 7.2 Run `dotnet test` and confirm all existing and new tests pass
- [x] 7.3 Add `itr.json` to `.gitignore` in the itr repo root
