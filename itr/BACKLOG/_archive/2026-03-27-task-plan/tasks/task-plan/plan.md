# Plan: Generate a Plan for a Task

**Status:** Draft  
**Task:** `task-plan`  
**Backlog Item:** `task-plan`  
**Repo:** `itr`

---

## Description

Add a `task plan <task-id>` CLI command that generates a `plan.md` artifact inside a task's directory and transitions the task state from `planning` → `planned`. Without `--ai`, a stub plan is written from a file-backed template populated with backlog item metadata. With `--ai`, the command connects to a locally running OpenCode server (`http://127.0.0.1:4096`), creates a named session, sends the `itr-plan` prompt, and uses the response as plan content. Re-running overwrites the plan and prints a notice if the task is already `planned`. A `--debug` flag prints raw HTTP responses to stderr.

The OpenCode connection is hidden behind an `IAgentHarness` capability interface, making it straightforward to swap in other harnesses via a different adapter in future commands.

---

## Scope

### New / modified files

| File | Change |
|---|---|
| `src/cli/Program.fs` | Add `TaskPlanArgs` DU; extend `TaskArgs` with `Plan` case; add `handleTaskPlan` handler; wire into `dispatch`; register `IAgentHarness` in `AppDeps` |
| `src/features/Task/TaskUsecase.fs` | Add `planTask` pure function: validates state, returns `(updatedTask, wasAlreadyPlanned)` |
| `src/domain/Domain.fs` | Add `InvalidTaskState of taskId: TaskId * current: TaskState` to `BacklogError` |
| `src/domain/Interfaces.fs` | Add `IAgentHarness` capability interface |
| `src/adapters/OpenCodeAdapter.fs` | `IAgentHarness` implementation over the OpenCode HTTP server API |
| `src/adapters/Itr.Adapters.fsproj` | Add `OpenCodeAdapter.fs` (no new NuGet packages — `System.Net.Http` is built into .NET 10) |
| `src/cli/assets/plan-template.md` | Stub plan template with Fue `{{{triple-brace}}}` placeholders |
| `src/cli/assets/plan-prompt.md` | AI planning prompt (verbatim copy of `.opencode/command/itr-plan.md` with `$ARGUMENTS` → task id at runtime) |
| `src/cli/Itr.Cli.fsproj` | Declare both assets as `<Content CopyToOutputDirectory="PreserveNewest" />`; add `Fue` NuGet reference |
| `tests/acceptance/TaskPlanAcceptanceTests.fs` | New acceptance test file |
| `tests/acceptance/Itr.Tests.Acceptance.fsproj` | Register new test file |

---

## Steps

### 1. Domain error

Add to `BacklogError` in `Domain.fs`:
```fsharp
| InvalidTaskState of taskId: TaskId * current: TaskState
```
Update the exhaustive `formatBacklogError` match in `Program.fs`.

### 2. `planTask` usecase

Add to `TaskUsecase.fs`:
```fsharp
/// Returns (updatedTask, wasAlreadyPlanned).
/// Allowed from: Planning, Planned (re-plan).
/// Error for states beyond Planned (Approved, InProgress, ...).
let planTask (task: ItrTask) : Result<ItrTask * bool, BacklogError>
```

### 3. `IAgentHarness` interface

Add to `Interfaces.fs`:
```fsharp
/// Capability interface for AI agent harness integration.
/// Abstracts over OpenCode, other harnesses, or future bidirectional protocols.
type IAgentHarness =
    /// Send a prompt and return the assistant text response.
    abstract Prompt: prompt: string -> debug: bool -> Result<string, string>
```

The adapter holds all harness-specific config (URL, auth). The interface stays generic. Future bidirectional use (e.g. `SendResponse`) can be added as additional members without breaking existing callers.

### 4. `OpenCodeAdapter`

New file `src/adapters/OpenCodeAdapter.fs` implementing `IAgentHarness`:

- Base URL hardcoded to `http://127.0.0.1:4096` for MVP.
- Flow:
  1. `GET /global/health` — if unreachable, return `Error "OpenCode server not reachable at http://127.0.0.1:4096. Start it with: opencode serve"`.
  2. `POST /session` with `{ "title": "[itr] planning | <task-id>" }` — session is left alive after use; `itr` does not own server or session lifetime.
  3. `POST /session/:id/message` with prompt as a text part.
  4. Extract and concatenate text parts from the response; return as `Ok content`.
- `debug = true`: write raw JSON response bodies to stderr at each HTTP step before parsing.
- Uses `System.Net.Http.HttpClient` (built into .NET 10, no new package needed).
- Session naming convention for future commands: `[itr] <verb> | <task-id>` (e.g. `[itr] start | task-plan`).

### 5. File assets

Add `Fue` NuGet package to `Itr.Cli.fsproj`. Use `fromText` (not `fromTextSafe`) to avoid HTML-encoding markdown content.

**`src/cli/assets/plan-template.md`** — Fue template; placeholders use `{{{triple-brace}}}` syntax:

```markdown
# Plan: {{{title}}}

**Status:** Draft
**Task:** {{{taskId}}}
**Backlog Item:** {{{backlogId}}}
**Repo:** {{{repo}}}

---

## Description

{{{summary}}}

---

## Scope

<!-- TODO: define scope -->

## Steps

<!-- TODO: define steps -->

## Dependencies

{{{dependencies}}}

## Acceptance Criteria

{{{acceptanceCriteria}}}

## Impact

<!-- TODO: describe impact on existing code -->

## Risks

<!-- TODO: identify risks -->

## Open Questions

<!-- TODO: list open questions -->
```

**`src/cli/assets/plan-prompt.md`** — verbatim copy of `.opencode/command/itr-plan.md` front-matter stripped; `$ARGUMENTS` replaced by the actual task id at call time via Fue before sending to OpenCode. Uses a `{{{taskId}}}` placeholder.

Template rendering in the handler:
```fsharp
open Fue.Data
open Fue.Compiler

let rendered =
    init
    |> add "title"            item.Title
    |> add "taskId"           rawTaskId
    |> add "backlogId"        (BacklogId.value task.SourceBacklog)
    |> add "repo"             (RepoId.value task.Repo)
    |> add "summary"          (item.Summary |> Option.defaultValue "")
    |> add "dependencies"     depsText        // pre-formatted bullet list or "none"
    |> add "acceptanceCriteria" criteriaText  // pre-formatted bullet list
    |> fromText templateContent
```

Both assets loaded at runtime:
```fsharp
Path.Combine(AppContext.BaseDirectory, "assets", "plan-template.md")
```

### 6. `handleTaskPlan` handler

```
1. Parse taskId, --ai flag, --debug flag.
2. taskStore.ListAllTasks coordRoot → find task → Error if missing.
3. backlogStore.LoadBacklogItem coordRoot backlogId → load item.
4. Task.planTask task → (updatedTask, wasAlreadyPlanned).
   - If wasAlreadyPlanned: print "Re-planning task <id> (was already planned)."
5. Load plan-template.md; render via Fue (fromText):
   - acceptanceCriteria: each criterion as a "- " bullet joined by newlines.
   - dependencies: each dep as a "- " bullet, or "none" if empty.
6. If --ai:
   a. Load plan-prompt.md; render via Fue substituting taskId.
   b. harness.Prompt promptText debug
      → Ok content: use as planContent (replaces stub).
      → Error msg: return Error — do not write any files.
7. fileSystem.WriteFile planPath planContent.
8. taskStore.WriteTask coordRoot updatedTask.
9. Print "Plan written: <path>".
```

### 7. `AppDeps` wiring

Instantiate `OpenCodeHarnessAdapter()` in `AppDeps`; implement `IAgentHarness` by delegating to it. Handler receives `deps :> IAgentHarness`.

### 8. Argu wiring

```fsharp
type TaskPlanArgs =
    | [<MainCommand; Mandatory>] Task_Id of string
    | Ai
    | Debug
    interface IArgParserTemplate with ...

type TaskArgs =
    | List of ParseResults<TaskListArgs>
    | Info of ParseResults<TaskInfoArgs>
    | Plan of ParseResults<TaskPlanArgs>
```

---

## Dependencies

- `task-promotion` (archived) — satisfied; `backlog take` creates tasks with `State = Planning`.

---

## Impact on Existing Code

- `BacklogError` gains one case — `formatBacklogError` exhaustive match must be updated.
- `TaskArgs` gains `Plan` case — `dispatch` must handle it.
- `IAgentHarness` and `OpenCodeAdapter` are additive; no existing handlers are touched.
- `AppDeps` grows one adapter field — no breaking change.
- `Fue` is a new NuGet dependency in `Itr.Cli` only.

---

## Acceptance Criteria

- `itr task plan <task-id>` creates `plan.md` in `<coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/`.
- Task state transitions `planning` → `planned`; persisted in `task.yaml`.
- Re-running overwrites `plan.md` and prints a re-plan notice; state stays `planned`.
- Attempting to plan a task in `approved` or beyond returns an error; no files are written.
- The plan contains: Description, Scope, Steps, Dependencies, Acceptance Criteria, Impact sections.
- Acceptance criteria from `item.yaml` are rendered as bullets under Acceptance Criteria.
- The plan references the source backlog item id and the task's repo.
- `itr task info <task-id>` shows `plan exists: yes` after planning.
- `--ai` connects to `http://127.0.0.1:4096`, creates a session titled `[itr] planning | <task-id>`, sends the planning prompt, and uses the response as plan content.
- `--debug` prints raw HTTP JSON responses to stderr during the OpenCode interaction.
- If OpenCode server is unreachable and `--ai` is passed, a clear error is returned and no files are written.
- Template and prompt files are present in the CLI output directory and loaded at runtime.

---

## Testing Strategy

- **Acceptance tests** (`tests/acceptance/TaskPlanAcceptanceTests.fs`): real filesystem, `IAgentHarness` stub injected in place of `OpenCodeAdapter`. Covers:
  - Happy path (stub plan): file written, state = `planned`, all required sections present.
  - Re-plan: file overwritten, re-plan notice printed, state stays `planned`.
  - Error: task not found.
  - Error: task in non-plannable state (e.g. `approved`).
  - Happy path (AI plan): stub harness returns fixed content; file written with that content.
  - AI harness error: stub harness returns `Error`; no files written, error returned.
- **`OpenCodeAdapter`** is not exercised in acceptance tests (requires live server). Manual verification against a running `opencode serve` instance.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| OpenCode server not running when `--ai` used | Health check first; clear error with `opencode serve` hint; no files written |
| Fue HTML-encoding corrupting markdown | Use `fromText` not `fromTextSafe`; Fue processes plain strings without HTML encoding |
| `{{{triple-brace}}}` clashing with existing markdown content | Unlikely in prose; Fue leaves undefined variables as-is (renders `{{{name}}}` literally if key not added), so misses are visible |
| `IAgentHarness.Prompt` too narrow for future bidirectional use | Interface can gain additional members without breaking existing callers; OpenCode session stays alive between calls if needed |
| Asset files absent from publish output | `CopyToOutputDirectory=PreserveNewest` ensures presence; missing files will surface as runtime errors caught in manual testing |
| Two `--ai` plan runs against same task simultaneously | Both create independent sessions and write to the same path — last-writer-wins, but both writes produce equivalent `planned` state; idempotent in outcome |

---

## Open Questions

*(none remaining)*
