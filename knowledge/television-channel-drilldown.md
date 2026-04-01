# Television Channel Drill-Down (itr workflow)

## Overview

`television` (`tv`) supports channel chaining via `mode = "execute"` — an action replaces the current `tv` process with a new one, enabling a drill-down UX. The selected entry from the current channel can be passed to the next channel via `--input '{}'`.

Pattern:
```toml
[actions.drill]
description = "Drill into related channel"
command = "tv <other-channel> --input '{selection}'"
mode = "execute"
```

## Planned hierarchy

```
itr-profiles  →  itr-products  →  itr-backlog  →  itr-tasks
```

Each level launches the next via `mode = "execute"` + `tv <channel> --input '...'`.

## Keybinding conflict

In `config.toml`, `ctrl-t` is globally bound to `toggle_remote_control`. In `itr-backlog.toml`, `ctrl-t` is bound to the channel-local `actions:take`. The channel-local binding wins inside the channel, making `toggle_remote_control` unreachable from `itr-backlog`.

**Fix:** rebind `toggle_remote_control` in `config.toml` to a different key (e.g. `ctrl-b`).

## Missing CLI commands needed

The drill-down hierarchy requires CLI commands that do not yet exist:

| Command | Purpose |
|---|---|
| `itr profiles list --output text` | List profile names for the `itr-profiles` channel source |
| `itr product list --profile <name> --output text` | List products for a profile for the `itr-products` channel source |

These should follow the same tab-delimited text output format as `itr backlog list --output text` and `itr task list --output text`.

## Text output column formats

**`backlog list --output text`:**
```
id\ttype\tpriority\tstatus\tview\ttaskCount\tcreatedAt\ttitle
```

**`task list --output text`:**
```
taskId\tbacklogId\trepoId\tstate\thasAiPlan
```

## Channel file locations

All custom channel definitions live in `.tv/cable/` relative to the repo root. Television picks these up automatically when `TV_CABLE_DIR` or the local config path points here.

## Backlog → Tasks drill-down (near-term, no new CLI commands needed)

`task list --backlog-id` already exists, so the `itr-backlog` → `itr-tasks` leg can be implemented immediately:

```toml
# in itr-backlog.toml
ctrl-d = "actions:tasks"

[actions.tasks]
description = "Browse tasks for this backlog item"
command = "tv itr-tasks --input '{split:\t:0}'"
mode = "execute"
```

```toml
# itr-tasks.toml source
command = "mise run task-list --backlog-id {input} --output text"
```

## Open questions

1. Should `itr-products` be scoped to a profile, or is one product per profile sufficient for the initial implementation?
2. Preferred keybind for `toggle_remote_control` after freeing `ctrl-t`?
