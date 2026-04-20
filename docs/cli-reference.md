# CLI Reference

## Synopsis

```
itr [--profile|-p <profile>] [--output <format>] <command> [<subcommand>] [options]
```

## Global Options

| Flag | Short | Type | Description |
|---|---|---|---|
| `--profile` | `-p` | `string` | Select the active portfolio profile. Overrides `ITR_PROFILE` env var and `defaultProfile` in `itr.json` |
| `--output` | — | `string` | Set output mode. Accepted value: `json` for machine-readable output |

**Profile resolution precedence:** `--profile` flag > `ITR_PROFILE` environment variable > `defaultProfile` in `itr.json`

---

## Commands

### `backlog`

Manage backlog items.

---

#### `backlog add`

Create a new backlog item.

```
itr backlog add <backlog-id> --title <title> [options]
```

| Argument | Required | Description |
|---|---|---|
| `<backlog-id>` | Yes | Slug ID for the new item |
| `--title <title>` | Yes | Short title for the backlog item |
| `--repo <repo-id>` | No | Repo ID to assign item to. Required if the product has more than one repo |
| `--item-type <type>` | No | Item type: `feature` \| `bug` \| `chore` \| `spike` (default: `feature`) |
| `--summary <text>` | No | Longer description of the item |
| `--priority <label>` | No | Priority label, e.g. `high`, `medium`, `low` |
| `--depends-on <id>` | No | Backlog item ID this item depends on. Repeatable |

Writes the item to `<coord-root>/BACKLOG/<backlog-id>/item.yaml`. Fails if the ID already exists.

---

#### `backlog take`

Take a backlog item and create task files for it.

```
itr backlog take <backlog-id> [--task-id <id>]
```

| Argument | Required | Description |
|---|---|---|
| `<backlog-id>` | Yes | Slug ID of the backlog item to take |
| `--task-id <id>` | No | Override the auto-generated task ID. Only valid for single-repo backlog items |

Creates a `task.yaml` file per repo assigned to the backlog item under `<coord-root>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`.

Task ID auto-derivation:
- Single-repo, first take → ID equals `<backlog-id>`
- Single-repo re-take, or multi-repo → `<repo-id>-<backlog-id>`
- Collision → appends `-2`, `-3`, etc.

---

#### `backlog list`

List backlog items.

```
itr backlog list [options]
```

| Option | Description |
|---|---|
| `--view <view>` | Filter by view ID |
| `--status <status>` | Filter by status: `created` \| `planning` \| `planned` \| `approved` \| `in-progress` \| `completed` \| `archived` |
| `--type <type>` | Filter by item type: `feature` \| `bug` \| `chore` \| `spike` |
| `--exclude <status>` | Exclude items with this status. Repeatable |
| `--order-by <field>` | Override sort order: `created` \| `priority` \| `type` |
| `--output <format>` | Output mode: `table` (default) \| `json` \| `text` |

---

#### `backlog info`

Show detailed information about a backlog item.

```
itr backlog info <backlog-id> [--output <format>]
```

| Argument | Required | Description |
|---|---|---|
| `<backlog-id>` | Yes | Backlog item ID to inspect |
| `--output <format>` | No | Output mode: `table` (default) \| `json` \| `text` |

---

### `task`

Manage tasks.

---

#### `task list`

List all tasks across a product.

```
itr task list [options]
```

| Option | Description |
|---|---|
| `--backlog-id <id>`, `--backlog <id>` | Filter by backlog item ID |
| `--repo-id <id>`, `--repo <id>` | Filter by repo ID |
| `--state <state>` | Filter by task state: `planning` \| `planned` \| `approved` \| `in_progress` \| `implemented` \| `validated` \| `archived` |
| `--exclude <state>` | Exclude tasks with this state. Repeatable |
| `--order-by <field>` | Sort order: `created` \| `state` |
| `--output, -o <format>` | Output mode: `table` (default) \| `json` \| `text` |

---

#### `task info`

Show detailed information about a task.

```
itr task info <task-id> [--output <format>]
```

| Argument | Required | Description |
|---|---|---|
| `<task-id>` | Yes | Task ID to inspect |
| `--output, -o <format>` | No | Output mode: `table` (default) \| `json` \| `text` |

---

#### `task plan`

Generate a plan for a task.

```
itr task plan <task-id> [--ai] [--debug]
```

| Argument | Required | Description |
|---|---|---|
| `<task-id>` | Yes | Task ID to plan |
| `--ai` | No | Use OpenCode AI to generate plan content |
| `--debug` | No | Print raw HTTP responses to stderr during AI interaction |

---

#### `task approve`

Approve a task plan.

```
itr task approve <task-id>
```

| Argument | Required | Description |
|---|---|---|
| `<task-id>` | Yes | Task ID to approve |

---

### `view`

Manage backlog views.

---

#### `view list`

List all named backlog views for a product.

```
itr view list [--product <product>] [--output <format>]
```

| Option | Description |
|---|---|
| `--product <id>` | Product ID (defaults to product resolved from working directory) |
| `--output, -o <format>` | Output mode: `table` (default) \| `json` \| `text` |

---

### `profile`

Manage portfolio profiles in `itr.json`.

---

#### `profile add`

Add a new profile.

```
itr profile add <name> [--git-name <name>] [--git-email <email>] [--set-default]
```

| Argument | Required | Description |
|---|---|---|
| `<name>` | Yes | Profile name slug |
| `--git-name <name>` | No | Git user name to associate with this profile |
| `--git-email <email>` | No | Git user email. Requires `--git-name` to also be set |
| `--set-default` | No | Set this profile as the default in `itr.json` |

Adds a named profile entry to `itr.json`. Fails on duplicate names (case-insensitive).

---

### `product`

Scaffold and register products.

---

#### `product init`

Scaffold a new product in a directory.

```
itr product init <path> [options]
```

| Argument | Required | Description |
|---|---|---|
| `<path>` | Yes | Target directory for the new product (must already exist) |
| `--id <id>` | No | Product ID slug. Interactive prompt if omitted |
| `--repo-id <id>` | No | Repo ID slug. Interactive prompt if omitted. Defaults to product ID if blank |
| `--coord-path <path>` | No | Coordination directory path relative to product root (default: `.itr`) |
| `--coord-mode <mode>` | No | Coordination mode: `primary-repo` \| `standalone` (default: `primary-repo`) |
| `--register-profile <profile>` | No | Register the new product in this named portfolio profile. Interactive prompt if omitted and `--no-register` not given |
| `--no-register` | No | Skip registration in `itr.json` entirely |

Creates the following files in `<path>`:
- `product.yaml` — product configuration
- `PRODUCT.md` — product description template
- `ARCHITECTURE.md` — architecture description template
- `<coord-path>/.gitkeep` — coordination directory

---

#### `product register`

Register an existing product in the active profile.

```
itr product register <path>
```

| Argument | Required | Description |
|---|---|---|
| `<path>` | Yes | Path to an existing product root directory (must contain `product.yaml`) |

Reads the product ID from `product.yaml`, resolves the active profile, and appends a product reference to it in `itr.json`.

---

## Command Tree

```
itr [--profile|-p <profile>] [--output <format>]
├── backlog
│   ├── add  <backlog-id> --title <title>
│   │                     [--repo <repo-id>]
│   │                     [--item-type feature|bug|chore|spike]
│   │                     [--summary <text>]
│   │                     [--priority <label>]
│   │                     [--depends-on <id>]  (repeatable)
│   ├── take <backlog-id> [--task-id <id>]
│   ├── list [--view <view>]
│   │        [--status created|planning|planned|approved|in-progress|completed|archived]
│   │        [--type feature|bug|chore|spike]
│   │        [--exclude <status>]  (repeatable)
│   │        [--order-by created|priority|type]
│   │        [--output table|json|text]
│   └── info <backlog-id> [--output table|json|text]
├── task
│   ├── list    [--backlog-id <id>] [--repo-id <id>]
│   │           [--state planning|planned|approved|in_progress|implemented|validated|archived]
│   │           [--exclude <state>]  (repeatable)
│   │           [--order-by created|state]
│   │           [--output table|json|text]
│   ├── info    <task-id> [--output table|json|text]
│   ├── plan    <task-id> [--ai] [--debug]
│   └── approve <task-id>
├── view
│   └── list [--product <id>] [--output table|json|text]
├── profile
│   └── add <name> [--git-name <name>]
│                  [--git-email <email>]
│                  [--set-default]
└── product
    ├── init <path> [--id <id>]
    │               [--repo-id <id>]
    │               [--coord-path <path>]
    │               [--coord-mode primary-repo|standalone]
    │               [--register-profile <profile>]
    │               [--no-register]
    └── register <path>
```

---

## Notes

- All slug IDs must match the pattern `[a-z0-9][a-z0-9-]*`
- `--git-email` requires `--git-name` to be set alongside it
- `--task-id` on `backlog take` is only valid for single-repo backlog items
- If no `itr.json` exists, it is auto-bootstrapped on the first run
