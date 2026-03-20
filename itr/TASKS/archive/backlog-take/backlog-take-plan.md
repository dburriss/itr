# Plan: Take a Backlog Item — `backlog-take`

**Status: Draft**

---

## Description

Implement the `itr backlog take <backlog-id>` command. When invoked, the command reads the named backlog item from the coordination root and creates one task file per repo listed on the item. Each task starts in the `planning` state.

This is the first command that reads product-level YAML (`product.yaml`, backlog items) and writes task files. It also establishes the YAML adapter, the `ProductConfig` domain model, and the `ItrTask` domain model.

Focus is single-repo items. Multi-repo coordination is deferred.

---

## Scope

### New domain types (`Itr.Domain` — `Domain.fs`)

- `BacklogId` — slug-validated string wrapper (same slug rules as `ProductId`)
- `TaskId` — slug-validated string wrapper
- `RepoId` — string (matches repo keys in `product.yaml`)
- `TaskState` — DU: `Planning | InProgress | Implemented | Validated`
- `BacklogItem` — `{ Id: BacklogId; Title: string; Repos: RepoId list }`
- `ItrTask` — `{ Id: TaskId; SourceBacklog: BacklogId; Repo: RepoId; State: TaskState; CreatedAt: DateOnly }`
- `ProductConfig` — `{ Id: ProductId; Repos: Map<RepoId, RepoConfig> }` (read from `product.yaml`)
- `RepoConfig` — `{ Path: string; Url: string option }`
- `TakeError` — error DU (see Validation section)

### New interfaces (`Itr.Domain` — `Interfaces.fs`)

- `IYamlService` — fill in placeholder: `Parse<'a> : string -> Result<'a, ParseError>` / `Serialize<'a> : 'a -> string`
- `IProductConfig` — `LoadProductConfig : coordRoot: string -> Result<ProductConfig, TakeError>`
- `IBacklogStore` — `LoadBacklogItem : coordRoot: string -> BacklogId -> Result<BacklogItem, TakeError>`
- `ITaskStore` — `ListTasks : coordRoot: string -> BacklogId -> Result<ItrTask list, TakeError>` and `WriteTask : coordRoot: string -> ItrTask -> Result<unit, TakeError>`

### New usecase (`Itr.Features` — `Task/TaskUsecase.fs`)

`takeBacklogItem` pure pipeline:

1. Load `product.yaml` from the coordination root.
2. Load backlog item from `<coordRoot>/BACKLOG/items/<backlog-id>.yaml`.
3. Validate all repos on the item exist in `product.yaml`.
4. Load existing tasks for this backlog id from `<coordRoot>/TASKS/<backlog-id>/`.
5. Generate a `TaskId` for each repo on the item:
   - Single repo, no existing tasks → use backlog id directly.
   - Single repo, existing tasks already present → use `<repo-id>-<backlog-id>` (with numeric suffix if still collides).
   - Multiple repos → use `<repo-id>-<backlog-id>` for each.
6. Return the list of `ItrTask` values (does not write — entry point writes).

Optional override: if `--task-id <id>` is supplied (single-repo only), use it instead of the derived id. Validate no collision with existing task ids.

### New adapter (`Itr.Adapters` — `YamlAdapter.fs`)

- Implement `IYamlService` using `YamlDotNet` (add package reference to `Itr.Adapters`).
- CLIMutable DTOs for `BacklogItem`, `ProductConfig`, `ItrTask` (snake_case YAML keys).
- Concrete implementations of `IProductConfig`, `IBacklogStore`, `ITaskStore`.

### CLI command (`Itr.Cli` — `Program.fs`)

Add `backlog take <backlog-id>` subcommand to Argu:

```
itr [--profile <p>] backlog take <backlog-id> [--task-id <id>]
```

Pipeline:

```
loadPortfolio → resolveActiveProfile → resolveProduct → takeBacklogItem → write tasks
```

Output (human): list of created task ids and file paths.  
Output (`--output json`): `{ "ok": true, "tasks": [ { "id": "...", "path": "..." } ] }`.

---

## File Layout

```
<coordRoot>/
  BACKLOG/
    items/
      <backlog-id>.yaml        ← read
  TASKS/
    <backlog-id>/
      <task-id>-task.yaml      ← written (one per repo)
```

Task YAML schema:

```yaml
id: <task-id>
source:
  backlog: <backlog-id>
repo: <repo-id>
state: planning
created_at: <ISO date>
```

---

## Dependencies / Prerequisites

- `portfolio-layer` (archived) — portfolio resolution pipeline already implemented.
- `product-init` and `backlog-item-create` backlog items are **not yet implemented** as commands, but real fixture files already exist in the itr repo (`product.yaml`, backlog item YAMLs) and are sufficient for testing.

---

## Validation Rules

| Check | Error |
|---|---|
| `product.yaml` missing or unparseable | `ProductConfigNotFound` / `ProductConfigParseError` |
| Backlog item file does not exist | `BacklogItemNotFound of BacklogId` |
| Repo on item not present in `product.yaml` | `RepoNotInProduct of RepoId` |
| `--task-id` supplied but id already exists | `TaskIdConflict of TaskId` |
| `--task-id` supplied on multi-repo item | `TaskIdOverrideRequiresSingleRepo` |

Existing task id collisions from auto-generation are resolved by incrementing a numeric suffix — not an error.

---

## Impact on Existing Code

- `Domain.fs` — additive only (new types appended).
- `Interfaces.fs` — additive only; `IYamlService` placeholder filled in.
- `Program.fs` (CLI) — add `Backlog` subcommand group and `Take` case to Argu DU; update dispatch.
- `AppDeps` composition root — wire in `YamlAdapter` for new interfaces.
- No existing files renamed.

---

## Acceptance Criteria

- `itr backlog take <backlog-id>` creates one `*-task.yaml` per repo listed on the item.
- Single-repo item with no existing tasks uses the backlog id as the task id.
- Single-repo item re-taken, or multi-repo item, uses `<repo-id>-<backlog-id>` as the id.
- `--task-id <id>` overrides the generated id for single-repo items.
- All repos on the item are validated against `product.yaml`; invalid repos produce a clear error.
- Created tasks have `state: planning` and a populated `created_at`.
- Re-running creates additional tasks rather than failing.

---

## Testing Strategy

**Communication tests** (`tests/communication/`):

- `TaskId` generation: single-repo/no-existing, single-repo/re-take, multi-repo.
- `--task-id` override: happy path and conflict.
- Repo validation: item repo not in product.
- All `TakeError` cases documented as readable specifications.

**Acceptance tests** (`tests/acceptance/`):

- Temp dir fixture with a `product.yaml` and a backlog item YAML.
- Run `takeBacklogItem` end-to-end with real filesystem.
- Assert task files written at correct paths with correct YAML content.
- Assert re-take produces additional tasks.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| `YamlDotNet` snake_case deserialization quirks | Use `[<YamlMember(Alias="...")>]` attributes on DTOs; add building tests while shaping |
| Task id collision from external file edits | Re-read existing ids at write time in the entry point, not just at planning time |
| `product.yaml` schema evolves | Keep DTOs minimal; unknown fields ignored via `YamlDotNet` config |
