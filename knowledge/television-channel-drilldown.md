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
     ↓ (sets default profile, scopes everything below)
```

Each level launches the next via `mode = "execute"` + `tv <channel> --input '...'`.

Products are always shown scoped to the active/default profile. `itr-profiles` is
the mechanism for switching that default — selecting a profile sets it as
`defaultProfile` in `itr.json` (persistent). All downstream channels then resolve
against it implicitly via the existing profile resolution logic.

## `itr-profiles` channel design

**Purpose:** browse profiles and switch the default.

**Source:** `itr profiles list --output text`

**Text output columns (4):**
```
name\tisDefault\tproductCount\tgitName
```

- `isDefault`: `yes` / `no`
- `productCount`: number of registered `ProductRef` entries in the profile
- `gitName`: from `GitIdentity.Name`

**Preview:** list the products for the hovered profile. Since `product list` requires
loading `product.yaml` for each product, the preview command will be:
```
itr product list --profile '{split:\t:0}' --output text
```

**Actions:**

| Key | Action | Command |
|---|---|---|
| `enter` | Set as default and drill into products | `itr profiles set-default '{split:\t:0}' && tv itr-products` |
| `ctrl-d` | Drill into products without changing default | `tv itr-products` |

Note: `enter` uses `mode = "execute"` (replaces tv with products channel after setting default).
`set-default` write is done via a new CLI command (see below).

## `itr-products` channel design

**Purpose:** browse products for the active profile, drill into backlog.

**Source:** `itr product list --output text`
(resolves against active/default profile — no `--profile` flag needed at this level)

**Text output columns:**
```
id\trepoCount\tcoordRoot
```

**Preview:** summary info from `product.yaml` (title, repos, coord mode).

**Actions:**

| Key | Action | Command |
|---|---|---|
| `enter` | Drill into backlog for this product | `tv itr-backlog` (backlog already scoped to active profile/product) |

Note: since backlog is scoped to a single active product via `coordRoot`, the
backlog channel doesn't need a filter passed — it just runs against the resolved
product. If multi-product navigation is needed, `--product <id>` can be added later.

## `itr-backlog` channel (existing, extended)

Add drill-down to tasks and editor open actions once path column is available.

**Additional actions:**

| Key | Action |
|---|---|
| `ctrl-d` | Drill into `itr-tasks` for selected backlog item |
| `ctrl-e` | Open `item.yaml` in `$EDITOR` |

## `itr-tasks` channel (new)

**Source:** `itr task list --backlog-id '{input}' --output text`

**Actions:**

| Key | Action |
|---|---|
| `ctrl-e` | Open `task.yaml` in `$EDITOR` |
| `ctrl-p` | Open `plan.md` in `$EDITOR` (if exists) |

## Keybinding conflict

In `config.toml`, `ctrl-t` is globally bound to `toggle_remote_control`. In
`itr-backlog.toml`, `ctrl-t` is bound to the channel-local `actions:take`. The
channel-local binding wins inside the channel, making `toggle_remote_control`
unreachable from `itr-backlog`.

**Fix:** rebind `toggle_remote_control` in `config.toml` to a different key (e.g. `ctrl-b`).

## Missing CLI commands needed

| Command | Purpose | Notes |
|---|---|---|
| `itr profiles list --output text` | Source for `itr-profiles` channel | New subcommand under `profiles` |
| `itr profiles set-default <name>` | Persist default profile switch | Writes `defaultProfile` in `itr.json` |
| `itr product list --output text` | Source for `itr-products` channel | Resolves against active profile; new subcommand under `product` |

`profiles list` and `profiles set-default` are both new subcommands.
`product list` is a new subcommand — `product` currently only has `init` and `register`.

All commands follow the same tab-delimited `--output text` format as `backlog list`
and `task list`.

## Text output column formats

**`profiles list --output text` (4 columns):**
```
name\tisDefault\tproductCount\tgitName
```

**`product list --output text` (3 columns):**
```
id\trepoCount\tcoordRoot
```

**`backlog list --output text` (9 columns, path appended — see path-construction.md):**
```
id\ttype\tpriority\tstatus\tview\ttaskCount\tcreatedAt\ttitle\tpath
```

**`task list --output text` (6 columns, path appended — see path-construction.md):**
```
taskId\tbacklogId\trepoId\tstate\thasAiPlan\tpath
```

## Channel file locations

All custom channel definitions live in `.tv/cable/` relative to the repo root.

## Backlog items

Dependencies: 4 → 5, 6 | 1+2 → 7 | 3 → 8 | 5+6 → 9+10

| # | Item | Type | Depends on |
|---|---|---|---|
| 1 | Add `itr profiles list` command | feature | — |
| 2 | Add `itr profiles set-default` command | feature | — |
| 3 | Add `itr product list` command | feature | — |
| 4 | Add path construction modules to domain + replace call sites | chore | — |
| 5 | Add item.yaml path column to `backlog list --output text` | feature | 4 |
| 6 | Add task.yaml and plan.md path columns to `task list --output text` | feature | 4 |
| 7 | tv channel: `itr-profiles` | feature | 1, 2 |
| 8 | tv channel: `itr-products` | feature | 3 |
| 9 | tv channel: `itr-tasks` | feature | 5, 6 |
| 10 | Extend `itr-backlog` channel + fix `ctrl-t` conflict | feature | 5 |
