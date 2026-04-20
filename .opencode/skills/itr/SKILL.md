---
name: itr
description: Use when working with the itr CLI to manage backlogs and tasks — creating items, listing, inspecting, taking into tasks, planning, and approving.
license: MIT
metadata:
  author: itr
  version: "1.0"
---

Work with the `itr` CLI to manage backlog items and tasks.

**Running commands**

Use the `ITR_BIN` environment variable (set in `mise.toml`):

```bash
$ITR_BIN <command> [options]
```

Or via mise:

```bash
mise run itr -- <command> [options]
```

Global flags available on every command:

| Flag | Short | Description |
|---|---|---|
| `--profile` | `-p` | Select the active portfolio profile |
| `--output` | — | Output mode: `json` for machine-readable output |

Profile resolution order (first match wins): `--profile` flag → `ITR_PROFILE` env var → `defaultProfile` in `itr.json`.

---

## Setup

Only needed if `itr` has not been initialised. Run in order:

```bash
$ITR_BIN settings bootstrap          # Create ~/.config/itr/itr.json if missing (auto-done on first run)
$ITR_BIN profile add <name> [--set-default]
$ITR_BIN product init <path>
$ITR_BIN product register <path>
```

### `profile add`

```
$ITR_BIN profile add <name> [--git-name <name>] [--git-email <email>] [--set-default]
```

| Argument | Required | Description |
|---|---|---|
| `<name>` | Yes | Profile name slug |
| `--git-name <name>` | No | Git user name |
| `--git-email <email>` | No | Git user email (requires `--git-name`) |
| `--set-default` | No | Set as default profile in `itr.json` |

### `product init`

```
$ITR_BIN product init <path> [options]
```

| Argument | Required | Description |
|---|---|---|
| `<path>` | Yes | Target directory (must already exist) |
| `--id <id>` | No | Product ID slug (prompted if omitted) |
| `--repo-id <id>` | No | Repo ID slug (defaults to product ID) |
| `--coord-path <path>` | No | Coordination directory relative to product root (default: `.itr`) |
| `--coord-mode <mode>` | No | `primary-repo` \| `standalone` (default: `primary-repo`) |
| `--register-profile <profile>` | No | Register product into this profile |
| `--no-register` | No | Skip registration entirely |

### `product register`

```
$ITR_BIN product register <path>
```

Reads product ID from `product.yaml` at `<path>` and registers it in the active profile.

---

## Backlog management

### `backlog add` — create a backlog item

```
$ITR_BIN backlog add <backlog-id> --title <title> [options]
```

| Argument | Required | Description |
|---|---|---|
| `<backlog-id>` | Yes | Slug ID for the new item |
| `--title <title>` | Yes | Short title |
| `--repo <repo-id>` | No | Repo to assign to. Required if product has multiple repos |
| `--item-type <type>` | No | `feature` \| `bug` \| `chore` \| `spike` (default: `feature`) |
| `--summary <text>` | No | Longer description |
| `--priority <label>` | No | e.g. `high`, `medium`, `low` |
| `--depends-on <id>` | No | Dependency on another backlog item ID. Repeatable |

Writes to `<coord-root>/BACKLOG/<backlog-id>/item.yaml`. Fails if ID already exists.

### `backlog take` — create tasks from a backlog item

```
$ITR_BIN backlog take <backlog-id> [--task-id <id>]
```

| Argument | Required | Description |
|---|---|---|
| `<backlog-id>` | Yes | Backlog item to take |
| `--task-id <id>` | No | Override auto-generated task ID (single-repo items only) |

Task ID auto-derivation:
- Single-repo, first take → ID equals `<backlog-id>`
- Single-repo re-take, or multi-repo → `<repo-id>-<backlog-id>`
- Collision → appends `-2`, `-3`, etc.

Creates `<coord-root>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml` per repo.

### `backlog list` — list backlog items

```
$ITR_BIN backlog list [options]
```

| Option | Description |
|---|---|
| `--view <view>` | Filter by view ID |
| `--status <status>` | Filter: `created` \| `planning` \| `planned` \| `approved` \| `in-progress` \| `completed` \| `archived` |
| `--type <type>` | Filter: `feature` \| `bug` \| `chore` \| `spike` |
| `--exclude <status>` | Exclude by status. Repeatable |
| `--order-by <field>` | `created` \| `priority` \| `type` |
| `--output <format>` | `table` (default) \| `json` \| `text` |

### `backlog info` — inspect a backlog item

```
$ITR_BIN backlog info <backlog-id> [--output <format>]
```

---

## Task management

### Task lifecycle

Backlog item state is **calculated** from its tasks (no stored state field):

| Backlog state | Condition |
|---|---|
| `unstarted` | No tasks exist |
| `in-progress` | Tasks exist and at least one is not archived |
| `done` | All tasks are archived |

Task state is stored in `task.yaml`:

| State | Transition command | Guard |
|---|---|---|
| `planning` | _(created by `backlog take`)_ | — |
| `planned` | `task plan` | — |
| `approved` | `task approve` | Plan artifact must exist |

### `task list` — list tasks

```
$ITR_BIN task list [options]
```

| Option | Description |
|---|---|
| `--backlog-id <id>`, `--backlog <id>` | Filter by backlog item |
| `--repo-id <id>`, `--repo <id>` | Filter by repo |
| `--state <state>` | Filter: `planning` \| `planned` \| `approved` \| `in_progress` \| `implemented` \| `validated` \| `archived` |
| `--exclude <state>` | Exclude by state. Repeatable |
| `--order-by <field>` | `created` \| `state` |
| `--output, -o <format>` | `table` (default) \| `json` \| `text` |

### `task info` — inspect a task

```
$ITR_BIN task info <task-id> [--output <format>]
```

### `task plan` — generate a plan

```
$ITR_BIN task plan <task-id> [--ai] [--debug]
```

| Argument | Required | Description |
|---|---|---|
| `<task-id>` | Yes | Task to plan |
| `--ai` | No | Use OpenCode AI to generate plan content |
| `--debug` | No | Print raw HTTP responses to stderr |

Re-runnable — creates or updates the plan artifact without changing state beyond `planned`.

### `task approve` — approve a plan

```
$ITR_BIN task approve <task-id>
```

Transitions task from `planned` → `approved`. Requires a plan artifact to exist.

---

## Views

### `view list` — list backlog views

```
$ITR_BIN view list [--product <id>] [--output <format>]
```

| Option | Description |
|---|---|
| `--product <id>` | Product ID (defaults to product resolved from working directory) |
| `--output, -o <format>` | `table` (default) \| `json` \| `text` |

---

## Guardrails

- All slug IDs must match `[a-z0-9][a-z0-9-]*`
- `--task-id` on `backlog take` is only valid for single-repo items
- `--git-email` requires `--git-name` to also be set
- `task approve` requires a plan artifact — run `task plan` first
- Don't invent task state transitions; only use what's documented above
- Use `$ITR_BIN` (or `mise run itr --`) for all commands, not a bare `itr` binary
