## Context

Tasks are created by `backlog take` and land in `planning` state. Until now, there was no command to generate a structured plan document or advance the task to `planned` state. The plan document captures the implementation intent and is a prerequisite for task approval.

The codebase follows a layered architecture: domain types/errors in `Domain.fs`, use-case logic in feature-specific files (e.g. `TaskUsecase.fs`), port interfaces in `Interfaces.fs`, adapters in `src/adapters/`, and CLI wiring in `Program.fs`. Tests use real filesystems via acceptance tests with stub adapters.

## Goals / Non-Goals

**Goals:**
- Introduce `itr task plan <task-id>` CLI command
- Generate a `plan.md` in the task directory from a file-backed template
- Transition task state `planning` → `planned`; allow re-planning from `planned`
- Support `--ai` flag to generate plan content via a locally running OpenCode server
- Support `--debug` flag for raw HTTP tracing during OpenCode interaction
- Abstract the AI harness behind an `IAgentHarness` interface for future extensibility

**Non-Goals:**
- Automatic plan approval (separate command/flow)
- Support for OpenCode servers at non-default addresses (MVP hardcodes `http://127.0.0.1:4096`)
- Bidirectional streaming with OpenCode (single prompt/response for MVP)
- Persistent session reuse across command invocations

## Decisions

### Template rendering: Fue library
Use `Fue` for `{{{triple-brace}}}` template substitution over raw string replacement or Scriban. Fue is lightweight and its triple-brace syntax avoids HTML-encoding issues with markdown content. `fromText` (not `fromTextSafe`) must be used to preserve raw strings without HTML escaping.

**Alternatives considered:** Scriban (heavier, more complex API), plain `String.Replace` (fragile, no structured data model).

### `IAgentHarness` capability interface
A single-method interface (`Prompt: string -> bool -> Result<string, string>`) hides all harness-specific wiring. The adapter holds the URL and HTTP client; the interface stays generic. This makes the handler testable via a stub injected in acceptance tests without touching the real OpenCode adapter.

**Alternatives considered:** Passing a plain function — viable but less discoverable. Inline HTTP in the handler — untestable and not reusable.

### OpenCode HTTP flow
1. `GET /global/health` — fail fast if server is unreachable
2. `POST /session` — create a named session (`[itr] planning | <task-id>`)
3. `POST /session/:id/message` — send the planning prompt as a text part
4. Concatenate text parts from the response as plan content

Sessions are left alive; `itr` does not own the server or session lifetime. `System.Net.Http.HttpClient` (built into .NET 10) is used — no new NuGet packages.

### Error-first: no partial writes
If the harness returns an error (unreachable server, bad response), no files are written and no state transition occurs. This prevents partially committed state.

### Re-planning allowed from `planned`; blocked beyond
`planTask` permits `Planning` and `Planned` states; returns `wasAlreadyPlanned = true` for the latter so the handler can print a notice. States beyond `Planned` (e.g. `Approved`, `InProgress`) return `InvalidTaskState` error — plan content is authoritative once approved and must not be silently overwritten.

## Risks / Trade-offs

- **OpenCode API drift** → The adapter targets a minimal subset of the API (health, session, message). If the API changes, only the adapter needs updating.
- **Fue HTML-encoding** → Mitigated by using `fromText`; triple-brace syntax passes raw strings.
- **Asset files absent from publish** → `CopyToOutputDirectory=PreserveNewest` ensures presence; missing files surface as runtime errors detectable in manual testing.
- **Concurrent plan runs** → Both writes produce equivalent `planned` state (last-writer-wins, idempotent outcome).
- **`IAgentHarness.Prompt` too narrow for future use** → Interface can gain additional members without breaking existing callers.
