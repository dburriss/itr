## Context

`itr task plan --ai` currently calls `OpenCodeHarnessAdapter`, which connects to a running OpenCode server over HTTP (`http://127.0.0.1:4096`). This couples the AI planning feature to a single agent implementation. ACP (Agent Client Protocol) provides a JSON-RPC 2.0 over stdin/stdout interface that is emerging as a standard for AI agent interaction. Supporting ACP as an opt-in protocol allows users to use any ACP-compatible agent without changing the core planning workflow.

The existing `OpenCodeHarnessAdapter` is unchanged and remains the default.

## Goals / Non-Goals

**Goals:**
- Introduce `AgentConfig` to represent protocol, command, and args for agent harness selection
- Add `agent` section to the global portfolio config profile with sensible defaults
- Support a per-project local `itr.json` that overrides the global agent config
- Implement `AcpHarnessAdapter`: launch agent subprocess, exchange JSON-RPC messages, stream output
- Select adapter at runtime based on merged config in `handleTaskPlan`

**Non-Goals:**
- ACP HTTP transport (draft spec, out of scope)
- Changes to OpenCodeAdapter or any feature/domain usecases
- Shell-style quoting in `args` (string array is sufficient)
- Scaffolding local `itr.json` via a command (manual creation is sufficient)

## Decisions

### JSON-RPC over stdin/stdout (ACP stdio)
Use `System.Diagnostics.Process` with `RedirectStandardInput`, `RedirectStandardOutput`, and `RedirectStandardError`. Write newline-delimited JSON to stdin; read lines from stdout.

**Alternative considered:** Named pipes or sockets. Rejected — ACP stdio is the published spec and avoids port management.

### `System.Text.Json` for serialisation
Use `System.Text.Json` (in-box with .NET 10) with explicit options. Extract message construction and response parsing into pure functions to enable unit testing without a subprocess.

**Alternative considered:** `Newtonsoft.Json`. Rejected — no additional dependency needed; `System.Text.Json` is sufficient.

### Local `itr.json` overrides global
A `<productRoot>/itr.json` file (gitignored) containing only an `agent` section overrides the global profile agent config. The merge strategy is: local present → use local; otherwise use global.

**Alternative considered:** CLI flag `--agent-protocol`. Rejected — config file is more ergonomic for persistent per-project settings.

### Default protocol remains `opencode-http`
If the `agent` section is absent from the global config or the protocol is unrecognised, fall back to `OpenCodeHarnessAdapter`. This preserves backward compatibility without any migration.

### ACP message sequence
Follow the ACP spec: `initialize` (id=0, `protocolVersion: 1` integer) → `session/new { cwd, mcpServers: [] }` (extract `sessionId`) → `session/prompt { sessionId, prompt: [{type, text}] }` → read `session/update` notifications until final `session/prompt` response → kill process.

`session/update` chunk notifications have the shape:
```json
{"method":"session/update","params":{"sessionId":"...","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"..."}}}}
```
Extract text from `params.update.content.text` when `params.update.sessionUpdate == "agent_message_chunk"`.

### Timeout and error handling
Set a configurable (or fixed) timeout on the subprocess. Kill on timeout and return `Error`. Non-JSON stdout lines from the subprocess are logged to stderr in debug mode and skipped.

## Risks / Trade-offs

- **Agent subprocess hangs** → Set a read timeout; kill the process and surface `Error "ACP agent timed out"`.
- **Agent startup banners on stdout** → Validate each stdout line as JSON before parsing; skip and debug-log non-JSON lines.
- **Local `itr.json` accidentally committed** → Document convention; add `itr.json` to `.gitignore` in the itr repo as a living example.
- **`System.Text.Json` F# list serialisation** → Test exact wire format in unit tests before integrating with the live subprocess.
- **ACP spec drift** → The implementation targets the current `initialize`/`session/new`/`session/prompt`/`session/update` message set; future spec changes may require adapter updates.

## Migration Plan

No migration required. The change is purely additive:
- Existing users with no `agent` section in their config continue using `OpenCodeHarnessAdapter` unchanged.
- Opt-in: users create a local `itr.json` with `{ "agent": { "protocol": "acp", "command": "opencode", "args": [] } }` to switch.

## Open Questions

- Should the ACP timeout be configurable via `AgentConfig` or hardcoded (e.g. 120 s)? Lean toward hardcoded for now; add to `AgentConfig` if needed later.
