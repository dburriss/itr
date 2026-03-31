# Show agent activity when doing an ACP call

**Task ID:** acp-activity
**Backlog Item:** acp-activity
**Repo:** itr

## Description

When using an agent over ACP, `session/update` notifications are streamed back by the agent but
currently discarded. This task surfaces them to the user via flags, and also changes the default
streaming behaviour of `agent_message_chunk`.

The `plan-acp` item specified "agent message chunks are streamed to stdout as they arrive" as
an acceptance criterion. This task supersedes that: chunks are now only streamed when `--print`
or `--debug` is active. The default (no flags) is silent — the accumulated response is returned
and the caller writes it at the end.

Flag behaviour:

| Flags | `agent_message_chunk` to stdout | Activity updates |
|---|---|---|
| none | No — silent | No |
| `--print` | Yes — streamed | No |
| `--debug` | Yes — streamed | Yes, plain lines to stderr |
| `--verbose` | No | Yes, spinner on stderr |
| `--print --debug` | Yes — streamed | Yes, plain lines (`--debug` wins) |
| `--debug --verbose` | Yes — streamed | Yes, plain lines (`--debug` wins) |

`--print --verbose` is not a supported combination; `--verbose` without `--debug` never streams
chunks, so the Spectre.Console Status spinner runs for the full duration without conflict.

The `--print` and `--verbose` flags are new. `--debug` already exists on `TaskPlanArgs` and is
threaded through to `IAgentHarness.Prompt`. `OpenCodeHarnessAdapter` is left unchanged — it
will be removed once ACP has proven stable.

---

## Scope

### Included

- Add `--print` and `--verbose` flags to `TaskPlanArgs` in `src/cli/Program.fs`
- Update `IAgentHarness.Prompt` signature to accept `print: bool` and `verbose: bool`
  alongside the existing `debug: bool`
- Update all `IAgentHarness` implementations:
  - `AcpHarnessAdapter` in `src/adapters/AcpAdapter.fs` — main implementation
  - `OpenCodeHarnessAdapter` in `src/adapters/OpenCodeAdapter.fs` — accept params, ignore them
  - `StubHarness` in `tests/acceptance/TaskPlanAcceptanceTests.fs` — accept params, ignore them
  - Inline adapter wrapper in `src/cli/Program.fs` (~line 340)
- In `AcpHarnessAdapter`, gate `agent_message_chunk` stdout printing on `print || debug`
- In `AcpHarnessAdapter`, handle ACP `session/update` notification types:
  - `agent_thought_chunk` → `[thinking] ...` (verbose spinner label / debug line)
  - `tool_call` → `[tool] <title>` (verbose spinner label / debug line)
  - `tool_call_update` → `[tool] <title> → <status>` (verbose spinner label / debug line)
  - `usage_update` → accumulate; print at end (debug: always; verbose: final status label)
  - `agent_message_chunk` → stream to stdout only if `print || debug`; always accumulate
  - Other types → ignored
- Spectre.Console `AnsiConsole.Status` spinner for `--verbose` mode (stderr-targeted)
- New `AcpActivity` DU and `extractActivityUpdate` pure function in `AcpAdapter.fs`
- Unit tests for `extractActivityUpdate`

### Excluded

- No changes to `OpenCodeHarnessAdapter` behaviour (to be deleted when ACP is stable)
- No changes to domain usecases or backlog data structures
- No Spectre.Console changes for `--debug` mode (plain text to stderr is sufficient)
- No persistent logging of agent activity

---

## Steps

1. **Add `Print` and `Verbose` cases to `TaskPlanArgs` in `src/cli/Program.fs`**  
   Add alongside the existing `| Debug` case:
   ```fsharp
   | Print   // "stream agent response chunks to stdout as they arrive"
   | Verbose // "show agent activity on a status line during AI interaction"
   ```

2. **Update `IAgentHarness.Prompt` in `src/domain/Interfaces.fs`**  
   Change signature from:
   ```fsharp
   abstract Prompt: prompt: string -> debug: bool -> Result<string, string>
   ```
   To:
   ```fsharp
   abstract Prompt: prompt: string -> debug: bool -> print: bool -> verbose: bool -> Result<string, string>
   ```
   Update the doc comment.

3. **Add `AcpActivity` DU and `extractActivityUpdate` to `AcpAdapter.fs`**  
   ```fsharp
   type AcpActivity =
       | Thinking of text: string
       | ToolCall of title: string
       | ToolCallUpdate of title: string * status: string
       | UsageUpdate of used: int * size: int * costAmount: decimal * costCurrency: string
       | Other  // unknown/unhandled sessionUpdate type
   ```
   Pure function `extractActivityUpdate: json: string -> AcpActivity option` — returns `None`
   for non-`session/update` messages and `Some Other` for unrecognised update types.

4. **Update `AcpHarnessAdapter.Prompt` in `AcpAdapter.fs`**  
   - Accept `print: bool` and `verbose: bool` parameters.
   - Gate `printf "%s" text` (chunk streaming) on `print || debug`.
   - In the read loop call `extractActivityUpdate` on each line:
     - `debug && not verbose`: print `[thinking] ...`, `[tool] ...` etc. as plain stderr lines;
       accumulate `UsageUpdate`; print usage at end.
     - `verbose && not debug`: create a stderr-targeted `AnsiConsole` and run
       `AnsiConsole.Status().Start(...)` wrapping the entire read loop; call `ctx.Status(label)`
       on each `Thinking` / `ToolCall` / `ToolCallUpdate`; after the loop print usage summary
       to stderr. Guard with TTY check; fall back to plain stderr lines if not a TTY.
     - `debug && verbose`: `--debug` wins — plain per-line stderr output, no spinner.
     - Neither: silent (no activity output).

5. **Update `OpenCodeHarnessAdapter.Prompt` in `OpenCodeAdapter.fs`**  
   Add `print: bool` and `verbose: bool` parameters; ignore both.

6. **Update call sites in `src/cli/Program.fs`**  
   - Inline adapter wrapper (~line 340): thread `print` and `verbose` through.
   - `handleTaskPlan`: extract `print` and `verbose` from `planArgs`; pass to
     `harness.Prompt renderedPrompt debug print verbose`.

7. **Update `StubHarness` in `tests/acceptance/TaskPlanAcceptanceTests.fs`**  
   Add `print: bool` and `verbose: bool` parameters; ignore them.

8. **Add unit tests in `AcpAdapterTests.fs`**  
   - `extractActivityUpdate` returns `Some (Thinking "...")` for `agent_thought_chunk` message
   - `extractActivityUpdate` returns `Some (ToolCall "title")` for `tool_call` message
   - `extractActivityUpdate` returns `Some (ToolCallUpdate ("title", "completed"))` for
     `tool_call_update` message
   - `extractActivityUpdate` returns `Some (UsageUpdate ...)` for `usage_update` message
   - `extractActivityUpdate` returns `None` for a non-`session/update` message
   - `extractActivityUpdate` returns `Some Other` for an unknown `sessionUpdate` type

9. **Run `dotnet build` and `dotnet test`**

---

## Dependencies

- backlog-add-ai
- plan-acp (AcpAdapter must be in place — done)

---

## Acceptance Criteria

- Default (no flags): `agent_message_chunk` is **not** streamed to stdout; only the final
  accumulated response is returned (supersedes plan-acp criterion "chunks streamed as they arrive")
- `--print`: chunks are streamed to stdout as they arrive; no activity output
- `--debug`: chunks are streamed to stdout; activity updates (`agent_thought_chunk`, `tool_call`,
  `tool_call_update`) printed as plain lines to stderr; `usage_update` printed at end
- `--verbose`: no chunk streaming; agent activity shown on a single updating Spectre.Console
  status line on stderr; usage summary shown at end
- `--debug --verbose`: `--debug` wins (plain lines, no spinner)
- Build and all existing tests pass

---

## Impact

| File | Change |
|---|---|
| `src/domain/Interfaces.fs` | `IAgentHarness.Prompt` gains `print: bool` and `verbose: bool` |
| `src/adapters/AcpAdapter.fs` | New `AcpActivity` DU, `extractActivityUpdate`; chunk streaming gated on `print \|\| debug`; verbose/debug display logic |
| `src/adapters/OpenCodeAdapter.fs` | `Prompt` accepts `print` and `verbose`, ignores them |
| `src/cli/Program.fs` | `Print` and `Verbose` cases in `TaskPlanArgs`; updated call sites |
| `tests/acceptance/TaskPlanAcceptanceTests.fs` | `StubHarness.Prompt` accepts and ignores new params |
| `tests/communication/AcpAdapterTests.fs` | New tests for `extractActivityUpdate` |

---

## Spectre.Console Status API notes

From https://spectreconsole.net/console/live/status:

- `AnsiConsole.Status().Start("message...", ctx => { ... })` — synchronous; callback receives a
  `StatusContext` to update the label via `ctx.Status("new text")` and optionally change the
  spinner via `ctx.Spinner(Spinner.Known.Dots)`.
- `AnsiConsole.Status().StartAsync("message...", async ctx => { ... })` — async variant.
- **Not thread safe** — cannot be used alongside other interactive components.
- Markup is supported in status strings: `ctx.Status("[blue]Connecting...[/]")`.
- The `AnsiConsole` for Status must target stderr: create via
  `AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) })`.

Since `--verbose` never streams chunks to stdout, the Status spinner runs uninterrupted for the
full duration of the ACP exchange — no conflict with stdout writes.

---

## Risks

| Risk | Mitigation |
|---|---|
| Spinner output garbled in non-TTY environments | Guard with TTY check; fall back to plain stderr lines |
| `tool_call` / `tool_call_update` message shape varies across ACP agents | Parse defensively; fall back to `Other` on missing fields |
| ACP spec adds new `sessionUpdate` types | `Other` case ensures unknown types are silently ignored |

---

## Open Questions

- Should `agent_thought_chunk` text be shown in full as the spinner label, or truncated/summarised
  as `[thinking...]`? Lean toward `[thinking...]` — thinking text can be very long for reasoning
  models.


**Task ID:** acp-activity
**Backlog Item:** acp-activity
**Repo:** itr

## Description

When using an agent over ACP, `session/update` notifications are streamed back by the agent but
currently discarded. This task surfaces them to the user via two existing flags:

- `--verbose`: shows agent activity on a single updating status line using Spectre.Console
  `Status` display. Usage summary is shown at the end.
- `--debug`: prints each raw message type as it arrives (plain text to stderr). Usage summary
  is printed at the end.
- Both flags together: `--debug` wins (raw message-per-line output).
- Neither flag: only `agent_message_chunk` text is printed to stdout (existing behaviour,
  unchanged).

The `--verbose` flag is new. The `--debug` flag already exists on `TaskPlanArgs` and is already
threaded through to `IAgentHarness.Prompt`.

---

## Scope

### Included

- Add `--verbose` flag to `TaskPlanArgs` in `src/cli/Program.fs`
- Update `IAgentHarness.Prompt` signature to accept a `verbose: bool` parameter alongside the
  existing `debug: bool`
- Update all implementations of `IAgentHarness` accordingly:
  - `AcpHarnessAdapter` in `src/adapters/AcpAdapter.fs` — main implementation
  - `OpenCodeHarnessAdapter` in `src/adapters/OpenCodeAdapter.fs` — accept param, ignore it
  - `StubHarness` in `tests/acceptance/TaskPlanAcceptanceTests.fs` — accept param, ignore it
  - Inline adapter wrapper in `src/cli/Program.fs` (~line 340)
- In `AcpHarnessAdapter`, handle ACP `session/update` notification types:
  - `agent_thought_chunk`: print `[thinking] <text>` (verbose status / debug line)
  - `tool_call`: print `[tool] <title>` (verbose status / debug line)
  - `tool_call_update`: print `[tool] <title> → <status>` (verbose status / debug line)
  - `usage_update`: accumulate; print at end of run (debug: always; verbose: update status line)
  - `agent_message_chunk`: unchanged — always printed to stdout
  - Other types: ignored
- Spectre.Console `Status` spinner for `--verbose` mode (single updating line on stderr)
- Unit tests for new `extractActivityUpdate` pure function in `AcpAdapter.fs`

### Excluded

- No changes to the OpenCode HTTP adapter behaviour
- No changes to domain usecases or backlog data structures
- No Spectre.Console changes for `--debug` mode (plain text output is sufficient)
- No persistent logging of agent activity

---

## Steps

1. **Add `Verbose` case to `TaskPlanArgs` in `src/cli/Program.fs`**  
   Add `| Verbose` alongside the existing `| Debug` case. Usage string:
   `"show agent activity on a status line during AI interaction"`.

2. **Update `IAgentHarness.Prompt` in `src/domain/Interfaces.fs`**  
   Change signature from:
   ```fsharp
   abstract Prompt: prompt: string -> debug: bool -> Result<string, string>
   ```
   To:
   ```fsharp
   abstract Prompt: prompt: string -> debug: bool -> verbose: bool -> Result<string, string>
   ```
   Update the doc comment to describe `verbose`.

3. **Add `extractActivityUpdate` pure function to `AcpAdapter.fs`**  
   Returns a discriminated union describing what to display for a given `session/update` line:
   ```fsharp
   type AcpActivity =
       | Thinking of text: string
       | ToolCall of title: string
       | ToolCallUpdate of title: string * status: string
       | UsageUpdate of used: int * size: int * costAmount: decimal * costCurrency: string
       | Other  // unknown/unhandled sessionUpdate type
   ```
   Pure function `extractActivityUpdate: json: string -> AcpActivity option` — returns `None`
   for non-`session/update` messages and `Some Other` for unrecognised update types.

4. **Update `AcpHarnessAdapter.Prompt` in `AcpAdapter.fs`**  
   - Accept new `verbose: bool` parameter.
   - In the read loop, call `extractActivityUpdate` on each line.
   - If `debug && not verbose`: print `[<sessionUpdate>] <summary>` to stderr for
     `Thinking`, `ToolCall`, `ToolCallUpdate`. Print `UsageUpdate` to stderr at end of run.
   - If `verbose && not debug`: create a stderr-targeted `AnsiConsole` and start an
     `AnsiConsole.Status` spinner with `ctx.Status(...)` updated on each `Thinking` /
     `ToolCall` / `ToolCallUpdate` notification. Stop the spinner (exit the Status block)
     on the first `agent_message_chunk` — after which chunks flow to stdout normally and any
     further activity messages fall back to plain stderr lines. Print usage summary to stderr
     after the loop. Guard with a TTY check; fall back to plain stderr lines if not a TTY.
   - If `debug && verbose`: treat as `debug` only (plain per-line output, no spinner).
   - If neither: existing behaviour (only `agent_message_chunk` to stdout).

5. **Update `OpenCodeHarnessAdapter.Prompt` in `OpenCodeAdapter.fs`**  
   Add `verbose: bool` parameter; ignore it (HTTP adapter has no streaming notifications).

6. **Update call sites**  
   - Inline adapter wrapper in `src/cli/Program.fs` (~line 340): pass `verbose` through.
   - `handleTaskPlan` in `src/cli/Program.fs`: extract `verbose` from `planArgs`; pass to
     `harness.Prompt renderedPrompt debug verbose`.

7. **Update `StubHarness` in `tests/acceptance/TaskPlanAcceptanceTests.fs`**  
   Add `verbose: bool` parameter; ignore it.

8. **Add unit tests in `AcpAdapterTests.fs`**  
   - `extractActivityUpdate` returns `Some (Thinking "...")` for `agent_thought_chunk` message
   - `extractActivityUpdate` returns `Some (ToolCall "title")` for `tool_call` message
   - `extractActivityUpdate` returns `Some (ToolCallUpdate ("id", "completed"))` for
     `tool_call_update` message
   - `extractActivityUpdate` returns `Some (UsageUpdate ...)` for `usage_update` message
   - `extractActivityUpdate` returns `None` for a non-`session/update` message
   - `extractActivityUpdate` returns `Some Other` for an unknown `sessionUpdate` type

9. **Run `dotnet build` and `dotnet test`**

---

## Dependencies

- backlog-add-ai
- plan-acp (AcpAdapter must be in place — done)

---

## Acceptance Criteria

- If `--verbose` is passed, agent activity (thinking, tool calls) appears on a single updating
  status line using Spectre.Console; usage summary is shown at the end
- If `--debug` is passed, each activity message is printed as a plain line to stderr as it
  arrives; usage summary is printed at the end
- If both `--verbose` and `--debug` are passed, behaviour is identical to `--debug` only
- The following update types produce output: `agent_thought_chunk`, `tool_call`,
  `tool_call_update`
- `usage_update` is printed at the end with `--debug`; shown as final status line with
  `--verbose`
- With neither flag (default / `--print`), only `agent_message_chunk` text is printed to
  stdout — no other output from the ACP adapter

---

## Impact

| File | Change |
|---|---|
| `src/domain/Interfaces.fs` | `IAgentHarness.Prompt` gains `verbose: bool` parameter |
| `src/adapters/AcpAdapter.fs` | New `AcpActivity` DU, `extractActivityUpdate` pure function; `Prompt` handles verbose/debug display |
| `src/adapters/OpenCodeAdapter.fs` | `Prompt` accepts `verbose: bool`, ignores it |
| `src/cli/Program.fs` | `Verbose` case in `TaskPlanArgs`; inline adapter updated; `handleTaskPlan` extracts and passes `verbose` |
| `tests/acceptance/TaskPlanAcceptanceTests.fs` | `StubHarness.Prompt` accepts `verbose: bool`, ignores it |
| `tests/communication/AcpAdapterTests.fs` | New tests for `extractActivityUpdate` |

---

## Spectre.Console Status API notes

From https://spectreconsole.net/console/live/status:

- `AnsiConsole.Status().Start("message...", ctx => { ... })` — synchronous; callback receives a
  `StatusContext` to update the label via `ctx.Status("new text")` and optionally change the
  spinner via `ctx.Spinner(Spinner.Known.Dots)`.
- `AnsiConsole.Status().StartAsync("message...", async ctx => { ... })` — async variant.
- **Not thread safe** — cannot be used alongside other interactive components (prompts, progress
  displays, other status displays).
- `AutoRefresh(false)` + `ctx.Refresh()` gives manual control over when the display updates.
- Markup is supported in status strings: `ctx.Status("[blue]Connecting...[/]")`.

**Key constraint for this task:** `AnsiConsole.Status` is not thread safe and cannot run
concurrently with other console writes. Because `agent_message_chunk` text is written to stdout
during the same read loop that drives the spinner, the spinner must be stopped (Status block
exited) before any `agent_message_chunk` chunks are printed. The design therefore runs the
Status spinner only during the pre-response phases (thinking, tool calls) and stops it as soon
as the first `agent_message_chunk` arrives, after which chunks flow to stdout normally. The
Spectre.Console `AnsiConsole` used for the Status display must target stderr (via
`AnsiConsole.Create(new AnsiConsoleSettings { Out = AnsiConsoleOutput(Console.Error) })`) to
keep stdout clean.

---

## Risks

| Risk | Mitigation |
|---|---|
| `Status` stops before first chunk but thinking/tools still in flight | Stop spinner on first `agent_message_chunk`; any subsequent tool calls after that are printed as plain debug lines to stderr |
| Spinner output garbled in non-TTY environments | Guard `AnsiConsole.Status` behind a TTY check (`AnsiConsole.Profile.Capabilities.Ansi`); fall back to plain stderr lines |
| `tool_call` message shape varies across agents | Parse defensively; fall back to `Other` on missing fields |
| ACP spec adds new `sessionUpdate` types | `Other` case ensures unknown types are silently ignored |

---

## Open Questions

- Should `agent_thought_chunk` be shown with `--verbose`? It can be very verbose for models
  with extended thinking. For now: yes, update the spinner label but don't accumulate/print
  thinking text — only show `[thinking...]` as the status.
