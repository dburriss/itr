# Plan: Replace OpenCode HTTP Adapter with ACP stdio Adapter

## Background

The current `OpenCodeHarnessAdapter` (`src/adapters/OpenCodeAdapter.fs`) calls OpenCode's
proprietary REST API — three synchronous HTTP calls to a running server at `127.0.0.1:4096`:

1. `GET /global/health` — liveness check
2. `POST /session` — creates a named session, extracts `id` from JSON response
3. `POST /session/{sessionId}/message` — sends the prompt, extracts text from the response

This is OpenCode-specific and won't work with any other agent.

ACP (Agent Client Protocol) is a JSON-RPC 2.0 over stdio protocol that OpenCode and ~25 other
agents support (GitHub Copilot, Cursor, Goose, Gemini CLI, Cline, etc.). Switching to ACP makes
`itr` harness-agnostic.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Transport | stdio (subprocess) | True ACP; works with any ACP agent; HTTP is still a draft spec |
| Agent command config | Global `itr.json` with per-project local `itr.json` override | Keeps AI tooling config out of committed `product.yaml` |
| Old adapter | Feature-flagged; kept as default | No breaking change; opt-in via config |
| JSON library | `System.Text.Json` | In-box with .NET 10; no extra NuGet dependency |
| Streaming | Stream `agent_message_chunk` to stdout as they arrive | Better UX than waiting for full response |

## ACP Flow (stdio)

```
launch agent subprocess
  → write: initialize (JSON-RPC id=0)
  ← read:  initialize response
  → write: session/new { cwd: <product root> }
  ← read:  session/new response (sessionId)
  → write: session/prompt { sessionId, prompt: [{ type: "text", text: <prompt> }] }
  ← read loop: session/update notifications (newline-delimited JSON-RPC)
      agent_message_chunk → print text to stdout (streaming)
      other update types  → ignore or debug-print
  ← read:  session/prompt response (stopReason)
  → kill subprocess
```

## Steps

### Step 1 — Extend domain types (`src/domain/Domain.fs`)

Add:
```fsharp
type AgentConfig =
    { Protocol: string          // "opencode-http" | "acp"
      Command: string           // e.g. "opencode"
      Args: string list }       // extra args to pass to subprocess
```

No change to `ProductDefinition` — agent config is not part of the product spec.

### Step 2 — Extend global config types (`src/adapters/PortfolioAdapter.fs`)

Add an `agent` section to the global `itr.json` portfolio/profile shape.
Default: `{ protocol: "opencode-http"; command: "opencode"; args: [] }`.
This provides the global default that the per-project local `itr.json` can override.

### Step 3 — Add per-project local `itr.json` override

A local `itr.json` file at the product root (e.g. `./itr.json`, gitignored) can override the
global agent config. This keeps AI tooling preferences out of the committed `product.yaml`.

```json
{
  "agent": {
    "protocol": "acp",
    "command": "opencode",
    "args": []
  }
}
```

This requires:
- A new `LocalConfig` type (or reuse the portfolio DTO shape) in `PortfolioAdapter.fs`
- A `ILocalConfig` interface or inline loading in `handleTaskPlan`
- `.gitignore` entry for `itr.json` at the product root (or document the convention)

### Step 4 — Add `AcpHarnessAdapter` (`src/adapters/AcpAdapter.fs`)

New file implementing `IAgentHarness` via ACP stdio:

- Constructor takes `command: string`, `args: string list`, `cwd: string`
- Uses `System.Diagnostics.Process` directly for bidirectional stdin/stdout I/O
  (not `simple-exec` which is fire-and-forget)
- Uses `System.Text.Json` for JSON-RPC serialisation/deserialisation
- Messages are newline-delimited on stdout; agent may log to stderr (capture for debug)
- Returns `Ok (accumulated text)` or `Error message`

### Step 5 — Feature-flag adapter selection in `handleTaskPlan` (`src/cli/Program.fs`)

After `productDef` is resolved in `handleTaskPlan`:

1. Load local `itr.json` from the product root (if present) and merge with global agent config
   (local overrides global; global provides defaults)
2. If `protocol = "acp"` → construct `AcpHarnessAdapter(command, args, coordRoot)`
3. If `protocol = "opencode-http"` (default) → construct `OpenCodeHarnessAdapter()` (unchanged)
4. Call `harness.Prompt renderedPrompt debug` as today

`OpenCodeAdapter.fs` is kept, untouched, as the default path.

### Step 6 — Register new file in project (`src/adapters/Itr.Adapters.fsproj`)

Add `AcpAdapter.fs` to the compile order (after `OpenCodeAdapter.fs`).

`System.Text.Json` is in-box with .NET 10 — no NuGet entry required.

### Step 7 — Tests

- New unit tests for `AcpHarnessAdapter` covering JSON-RPC message building and response parsing
  (extract pure serialisation/parsing functions to make them testable without a subprocess)
- Existing `TaskPlanAcceptanceTests` unchanged (`StubHarness` still works via `IAgentHarness`)

## Files Affected

| File | Change |
|---|---|
| `src/domain/Domain.fs` | Add `AgentConfig` type |
| `src/adapters/PortfolioAdapter.fs` | Add `agent` section to global `itr.json` DTO |
| `src/adapters/AcpAdapter.fs` | **New** — `AcpHarnessAdapter` |
| `src/adapters/OpenCodeAdapter.fs` | Unchanged (kept as default) |
| `src/adapters/Itr.Adapters.fsproj` | Add `AcpAdapter.fs` to compile list |
| `src/cli/Program.fs` | Load local `itr.json`, feature-flag adapter selection in `handleTaskPlan` |
| `tests/unit/` | New tests for `AcpHarnessAdapter` |

## References

- ACP Introduction: https://agentclientprotocol.com/get-started/introduction
- ACP Protocol Overview: https://agentclientprotocol.com/protocol/overview
- ACP Transports (stdio): https://agentclientprotocol.com/protocol/transports
- ACP Session Setup: https://agentclientprotocol.com/protocol/session-setup
- ACP Prompt Turn: https://agentclientprotocol.com/protocol/prompt-turn
- ACP Initialization: https://agentclientprotocol.com/protocol/initialization
- ACP Compatible Agents: https://agentclientprotocol.com/get-started/agents
