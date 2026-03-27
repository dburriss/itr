## Why

Tasks created via `backlog take` land in `planning` state with no structured artifact to capture the implementation plan. Without a `task plan` command, there is no consistent way to generate or persist a plan document, and no state transition to signal a task is ready to be approved.

## What Changes

- New `itr task plan <task-id>` CLI command that generates a `plan.md` in the task directory
- Task state transitions from `planning` → `planned` on success; re-running is allowed from `planned`
- Stub plan generated from a file-backed template populated with backlog item metadata
- `--ai` flag connects to a locally running OpenCode server to generate plan content via prompt
- `--debug` flag prints raw HTTP responses to stderr during OpenCode interaction
- `IAgentHarness` capability interface added to `Interfaces.fs` to abstract the AI harness
- `OpenCodeAdapter` implements `IAgentHarness` over the OpenCode HTTP server API
- New `InvalidTaskState` domain error for attempting to plan a task in an invalid state

## Capabilities

### New Capabilities

- `task-plan`: CLI command to generate a plan artifact for a task and transition it from `planning` to `planned`

### Modified Capabilities

- `task-info`: Extend output to show `plan exists: yes/no` after planning

## Impact

- `BacklogError` DU gains `InvalidTaskState` case — exhaustive match in `Program.fs` must be updated
- `TaskArgs` DU gains `Plan` case — `dispatch` must handle it
- `AppDeps` gains an `IAgentHarness` field (additive, no breaking change)
- New NuGet dependency: `Fue` (template rendering) in `Itr.Cli` only
- New file assets: `plan-template.md` and `plan-prompt.md` copied to CLI output directory
