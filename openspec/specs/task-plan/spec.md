## Requirements

### Requirement: Generate a plan document for a task
The system SHALL provide a `task plan <task-id>` subcommand that generates a `plan.md` file in the task directory (`<coordRoot>/BACKLOG/<backlogId>/tasks/<taskId>/plan.md`) and transitions the task state from `planning` to `planned`.

#### Scenario: Plan created for a task in planning state
- **WHEN** `itr task plan <task-id>` is run for a task with state `planning`
- **THEN** `plan.md` is written to the task directory and the task state is updated to `planned`

#### Scenario: Plan output path is reported
- **WHEN** `itr task plan <task-id>` completes successfully
- **THEN** stdout includes `Plan written: <path>` showing the full path to `plan.md`

### Requirement: Stub plan from template
The system SHALL render the plan document from a file-backed template (`plan-template.md`) populated with metadata from the source backlog item: title, task id, backlog item id, repo, summary, dependencies (as bullet list or "none"), and acceptance criteria (as bullet list).

#### Scenario: Plan contains metadata from backlog item
- **WHEN** `itr task plan <task-id>` is run without `--ai`
- **THEN** the generated `plan.md` contains the task id, backlog item id, repo, and acceptance criteria from the source item

#### Scenario: Plan contains all required sections
- **WHEN** `itr task plan <task-id>` is run without `--ai`
- **THEN** the generated `plan.md` contains sections: Description, Scope, Steps, Dependencies, Acceptance Criteria, Impact, Risks, Open Questions

### Requirement: Re-planning an already-planned task
The system SHALL allow re-running `task plan` when the task is already in `planned` state, overwriting the existing `plan.md` and printing a re-plan notice.

#### Scenario: Re-plan overwrites existing plan
- **WHEN** `itr task plan <task-id>` is run on a task already in `planned` state
- **THEN** `plan.md` is overwritten, a notice `Re-planning task <id> (was already planned).` is printed, and the state remains `planned`

### Requirement: Planning blocked for tasks beyond planned state
The system SHALL reject `task plan` for tasks in states beyond `planned` (e.g. `approved`, `in_progress`, `implemented`, `validated`, `archived`), returning an error and writing no files.

#### Scenario: Error for task in approved state
- **WHEN** `itr task plan <task-id>` is run for a task with state `approved`
- **THEN** the command exits with a non-zero code, an error is printed indicating an invalid state transition for the task, and no files are written

#### Scenario: Error for task not found
- **WHEN** `itr task plan unknown-id` is run and no such task exists
- **THEN** the command exits with a non-zero code and outputs an appropriate error message

### Requirement: AI-generated plan via configured agent harness
The system SHALL support a `--ai` flag that selects an agent harness based on the merged agent config (global profile overridden by local `itr.json`). When `protocol` is `"acp"`, the system SHALL launch the configured `command` as a subprocess and communicate via ACP (JSON-RPC 2.0 over stdin/stdout). When `protocol` is `"opencode-http"` or absent, the system SHALL connect to a locally running OpenCode server at `http://127.0.0.1:4096` as before. The harness sends the planning prompt and uses the response as the plan content.

#### Scenario: AI plan generated with opencode-http protocol (default)
- **WHEN** `itr task plan <task-id> --ai` is run with no agent config (or `protocol: "opencode-http"`)
- **AND** the OpenCode server is reachable at `http://127.0.0.1:4096`
- **THEN** a session is created via HTTP, the planning prompt is sent, and the response is written as `plan.md`

#### Scenario: Error when OpenCode server is unreachable (opencode-http)
- **WHEN** `itr task plan <task-id> --ai` is run with `protocol: "opencode-http"`
- **AND** no OpenCode server is running at `http://127.0.0.1:4096`
- **THEN** the command exits with a non-zero code, outputs an error message suggesting `opencode serve`, and no files are written

#### Scenario: AI plan generated with ACP protocol
- **WHEN** `itr task plan <task-id> --ai` is run with `protocol: "acp"` and a valid `command`
- **AND** the agent subprocess starts and responds to ACP messages
- **THEN** the agent is launched as a subprocess, the planning prompt is sent via ACP, streamed chunks are printed to stdout, and the accumulated response is written as `plan.md`

#### Scenario: ACP agent subprocess fails to start
- **WHEN** `itr task plan <task-id> --ai` is run with `protocol: "acp"` and the command cannot be found
- **THEN** the command exits with a non-zero code, an error message is printed, and no files are written

#### Scenario: ACP agent message chunks streamed to stdout
- **WHEN** `itr task plan <task-id> --ai` is run with `protocol: "acp"`
- **AND** the agent sends `session/update` messages with `agent_message_chunk` content
- **THEN** each chunk's text is printed to stdout as it arrives

#### Scenario: AI harness error prevents file write
- **WHEN** `itr task plan <task-id> --ai` is run and the harness returns an error
- **THEN** no `plan.md` is written and no state transition occurs

### Requirement: Debug mode for ACP interaction
The system SHALL support a `--debug` flag that prints raw ACP JSON-RPC messages to stderr at each step of the ACP interaction when `protocol: "acp"` is in use. Non-JSON stdout lines from the agent subprocess SHALL be logged to stderr in debug mode and skipped.

#### Scenario: Debug output shown on stderr for ACP
- **WHEN** `itr task plan <task-id> --ai --debug` is run with `protocol: "acp"`
- **THEN** raw ACP JSON-RPC messages exchanged with the subprocess are printed to stderr
