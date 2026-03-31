# Plan: Use ACP Protocol for AI Planning

**Status:** Draft

---

## Description

Replace the OpenCode-specific HTTP adapter with an ACP (Agent Client Protocol) stdio adapter so
the `task plan --ai` command works with any ACP-compatible agent (OpenCode, GitHub Copilot,
Cursor, Goose, Gemini CLI, etc.). OpenCode HTTP remains the default; ACP is opt-in via config.

ACP uses JSON-RPC 2.0 over stdin/stdout. The client launches the agent as a subprocess, sends
`initialize` → `session/new` → `session/prompt`, reads streamed `session/update` chunks, and
terminates the process when done.

---

## Scope

### Included

- `AgentConfig` domain type (`protocol`, `command`, `args`)
- `agent` section in global `~/.config/itr/itr.json` (per-profile, provides default)
- Per-project local `itr.json` at the product root (gitignored, overrides global)
- New `AcpHarnessAdapter` implementing `IAgentHarness` via ACP stdio
- Feature-flag adapter selection in `handleTaskPlan`: reads merged config, constructs correct adapter
- Stream `agent_message_chunk` text to stdout as chunks arrive
- Unit tests for ACP message serialisation and response parsing

### Excluded

- No changes to `OpenCodeAdapter.fs` — kept as-is, remains the default
- No changes to `product.yaml` — agent config is not part of the committed product spec
- No changes to feature or domain usecases
- ACP HTTP transport (still a draft spec)

---

## Steps

1. **Add `AgentConfig` to `src/domain/Domain.fs`**
   ```fsharp
   type AgentConfig =
       { Protocol: string   // "opencode-http" | "acp"
         Command: string    // executable name, e.g. "opencode"
         Args: string list } // extra CLI args
   ```

2. **Extend global config in `src/adapters/PortfolioAdapter.fs`**  
   Add `AgentConfigDto` and an `agent` field to the profile DTO. Default:
   `{ protocol = "opencode-http"; command = "opencode"; args = [] }`.
   Map to/from `AgentConfig` in load/save.

3. **Add local `itr.json` loading in `src/adapters/PortfolioAdapter.fs`**  
   Add a `LoadLocalConfig: productRoot: string -> AgentConfig option` helper.  
   Reads `<productRoot>/itr.json` if present; returns `None` if absent.  
   Local config only contains the `agent` section; other fields ignored.

4. **Add `src/adapters/AcpAdapter.fs`**  
   `AcpHarnessAdapter(command, args, cwd)` implementing `IAgentHarness`:
   - Launch subprocess via `System.Diagnostics.Process` with stdin/stdout redirected
   - Write newline-delimited JSON-RPC messages to stdin using `System.Text.Json`
   - Send `initialize` (id=0, `protocolVersion: 1` integer), read response
   - Send `session/new { cwd, mcpServers: [] }`, read response → extract `sessionId`
   - Send `session/prompt { sessionId, prompt: [{type:"text", text: <prompt>}] }`
   - Read loop: on `session/update` where `params.update.sessionUpdate == "agent_message_chunk"` → print `params.update.content.text` to stdout; on final `session/prompt` response (has `id` + `result`) → stop
   - Kill subprocess; return `Ok (accumulated text)` or `Error message`
   - Capture stderr and emit on debug

5. **Register in `src/adapters/Itr.Adapters.fsproj`**  
   Add `AcpAdapter.fs` after `OpenCodeAdapter.fs` in compile order.

6. **Wire adapter selection in `src/cli/Program.fs` (`handleTaskPlan`)**  
   After `productDef` is resolved:
   - Load global `AgentConfig` from the active profile
   - Load local `itr.json` from the product root; merge (local wins)
   - `match config.Protocol with`
     - `"acp"` → `AcpHarnessAdapter(config.Command, config.Args, coordRoot)`
     - `_`     → `OpenCodeHarnessAdapter()` (default, unchanged)
   - Call `harness.Prompt renderedPrompt debug` as today

7. **Add unit tests**  
   Extract JSON-RPC serialisation and response parsing into pure functions.  
   Test: `initialize` message shape, `session/new` shape, `session/prompt` shape,  
   chunk extraction from `session/update`, and `sessionId` extraction from `session/new` response.

---

## Dependencies / Prerequisites

- None. `OpenCodeAdapter.fs` continues to function as the default path.
- `System.Text.Json` is in-box with .NET 10; no NuGet additions required.
- `System.Diagnostics.Process` is standard BCL; no new dependencies.

---

## Impact on Existing Code

| File | Change |
|---|---|
| `src/domain/Domain.fs` | Add `AgentConfig` type |
| `src/adapters/PortfolioAdapter.fs` | Add `AgentConfigDto`, `agent` field in profile DTO, `LoadLocalConfig` helper |
| `src/adapters/AcpAdapter.fs` | **New** — `AcpHarnessAdapter` |
| `src/adapters/OpenCodeAdapter.fs` | Unchanged |
| `src/adapters/Itr.Adapters.fsproj` | Add `AcpAdapter.fs` to compile list |
| `src/cli/Program.fs` | Adapter selection logic in `handleTaskPlan` |
| `tests/unit/` | New tests for ACP message building and parsing |

No changes to feature or domain usecases. `StubHarness` in acceptance tests is unaffected.

---

## Acceptance Criteria

- [ ] Global `itr.json` profile supports an `agent` section (`protocol`, `command`, `args`)
- [ ] A local `itr.json` at the product root overrides the global agent config when present
- [ ] `protocol: "opencode-http"` (or absent) uses the existing `OpenCodeHarnessAdapter` unchanged
- [ ] `protocol: "acp"` launches the agent as a subprocess via ACP stdio
- [ ] Agent `session/update` message chunks are printed to stdout as they arrive
- [ ] `task plan --ai` completes end-to-end with `protocol: "acp"` and `command: "opencode"`
- [ ] Build and all existing tests pass without modification

---

## Testing Strategy

### Unit tests (new)
Pure-function tests for JSON-RPC message construction and response parsing:
- `initialize` request: `protocolVersion` is integer `1`, not a string
- `session/new` request includes correct `cwd` and empty `mcpServers` array
- `session/prompt` request uses `prompt: [{type:"text", text:"..."}]` (not `messages`)
- `session/update` chunk extraction reads `params.update.content.text` when `params.update.sessionUpdate == "agent_message_chunk"`
- `session/new` response `sessionId` extraction handles valid and malformed input

### Acceptance tests (existing, unchanged)
`TaskPlanAcceptanceTests` uses `StubHarness` — no changes required. ACP adapter is not exercised
in automated acceptance tests (requires a live agent subprocess).

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| ACP agent subprocess hangs (no response) | Set a configurable timeout on the `Process`; kill on timeout and return `Error` |
| Agent writes non-ACP output to stdout (startup banners, etc.) | Validate each line is valid JSON-RPC before parsing; log unparseable lines to stderr in debug mode |
| Local `itr.json` accidentally committed | Document convention; add `itr.json` to `.gitignore` in the itr repo itself as a living example |
| `System.Text.Json` serialisation of F# lists needs care | Use `JsonSerializer` with explicit options; test the exact wire format before integrating |

---

## Open Questions

- ~~Should `args` in `AgentConfig` support shell-style quoting (e.g. `--flag "value with spaces"`), or is a string array sufficient?~~ **Resolved:** String array is sufficient; no shell-parsing dependency.
- ~~Should the local `itr.json` be documented as an opt-in file (user creates it manually) or should `itr product init` scaffold it?~~ **Resolved:** Out of scope. `itr.json` is created by `itr init`; manual creation is sufficient for this task.
