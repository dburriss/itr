# task-start + OpenSpec Design

## Summary

`task-spec` is eliminated. All spec control lives on `task-start` via flags. OpenSpec is an optional implementation detail, togglable via config and/or flag, enabling speed/token efficiency and A/B testing of OpenSpec effectiveness.

## Config

`itr.json` (global user-level or repo-level; repo overrides global):

```json
{
  "spec": {
    "openspec": true
  }
}
```

The `spec` map is designed for extensibility — other spec backends could be added alongside `openspec`.

**Default:** OpenSpec on.

## CLI Flags

| Flag | Behavior |
|------|----------|
| *(none)* | spec mode per resolved config |
| `--with-spec` | force spec on, override config |
| `--no-spec` | skip spec, override config |
| `--spec-only` | generate spec + create worktree, stop before agent invocation |

Flags override everything (global config and repo config).

`--spec-only` with `--no-spec` is an error (nonsensical combination).

## task-start Flow

```
1. Validate task is in Approved state (idempotent if already InProgress)
2. Transition task → InProgress (write task.yaml)
3. Create worktree for task branch
4. Resolve spec mode: global itr.json → repo itr.json → flag override

If spec enabled:
  a. If artifacts absent → auto-generate via openspec ff-change (abort on failure)
  b. Enforce isComplete: true via openspec CLI (always, even if artifacts pre-existing)
  c. If --spec-only → stop here (no agent invocation)
  d. Invoke agent in worktree CWD with prompt: /opsx-apply <task-id>

If spec disabled:
  a. If --spec-only → error
  b. Invoke agent in worktree CWD with plan.md contents as prompt
```

## Key Decisions

- **Worktree created regardless of `--spec-only`** — starting spec generation is considered starting work; `InProgress` + worktree apply.
- **`isComplete` always enforced when spec is enabled** — whether artifacts were just generated or pre-existing.
- **Re-generation handled by rollback** — if user is unhappy with generated artifacts, they roll back via git (auto-commit feature, future work). No re-generation flag.
- **Agent CWD = worktree path** — agent is scoped to the task branch.
- **Prompt fallback (no spec)** — plan.md contents embedded directly in the agent prompt.
- **Auto-generate on absent artifacts** — `task-start` generates spec itself if OpenSpec is enabled and no artifacts exist; it does not require the user to have run a separate command first.

## Interfaces Affected

- `IOpenSpecCli`: `IsChangeReady`, `GenerateArtifacts` (new)
- `IAgentHarness.Prompt`: gains `workdir` parameter for worktree CWD
- `IWorktreeManager` (new): `CreateWorktree: TaskId -> WorktreePath`
- `IConfigStore` (new or extended): reads `itr.json` at global + repo level, merges with repo taking precedence
- CLI: `task start [--no-spec | --with-spec | --spec-only]`
- `task spec` command: **eliminated**
