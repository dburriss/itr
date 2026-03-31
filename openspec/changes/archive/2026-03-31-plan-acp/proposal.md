## Why

The `task plan --ai` command is locked to a single agent (OpenCode HTTP), preventing use with other ACP-compatible agents such as GitHub Copilot, Cursor, Goose, or Gemini CLI. Adding ACP (Agent Client Protocol) stdio support makes the planning capability agent-agnostic while keeping OpenCode HTTP as the default.

## What Changes

- Add an `AgentConfig` domain type (`protocol`, `command`, `args`) to represent the configured AI harness
- Add an `agent` section to the global `~/.config/itr/itr.json` profile config
- Support a per-project local `itr.json` at the product root (gitignored) that overrides the global agent config
- Add a new `AcpHarnessAdapter` that launches an agent subprocess via ACP (JSON-RPC 2.0 over stdin/stdout)
- Wire adapter selection in `task plan --ai`: read merged config, choose `AcpHarnessAdapter` or existing `OpenCodeHarnessAdapter`
- Stream `agent_message_chunk` text to stdout as chunks arrive during ACP sessions

## Capabilities

### New Capabilities
- `agent-config`: Configuration of the AI agent harness, including protocol selection, command, and args; stored in the global profile and overridable via a local project `itr.json`

### Modified Capabilities
- `task-plan`: The AI planning requirement changes from OpenCode-HTTP-only to supporting any ACP-compatible agent via configuration; protocol selection, per-project override, and ACP subprocess behaviour become requirements

## Impact

- `src/domain/Domain.fs`: new `AgentConfig` type
- `src/adapters/PortfolioAdapter.fs`: `AgentConfigDto`, `agent` field in profile DTO, `LoadLocalConfig` helper
- `src/adapters/AcpAdapter.fs`: new file — `AcpHarnessAdapter`
- `src/adapters/Itr.Adapters.fsproj`: compile order updated
- `src/cli/Program.fs`: adapter selection logic in `handleTaskPlan`
- `tests/unit/`: new tests for ACP message building and parsing
- No changes to `OpenCodeAdapter.fs`, feature usecases, or acceptance tests
