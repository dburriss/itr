# Plan: Create Backlog Item Command

**Status:** Draft  
**Task:** `backlog-item-create`  
**Backlog item:** `backlog-item-create`  
**Repo:** `itr`

---

## Description

Add a CLI command `itr backlog add <id>` that creates a new backlog item YAML file at `<coordRoot>/BACKLOG/<id>/item.yaml`. The command validates the id, title, repos, and type before writing the file. Duplicate ids and invalid repo references are rejected.

---

## Decisions

| # | Decision |
|---|---|
| 1 | Rename `TakeError` → `BacklogError`; update all use sites. Add `DuplicateBacklogId`, `InvalidItemType`, `MissingTitle` cases. |
| 2 | `--title` is `Mandatory` in Argu — fail at parse time. |
| 3 | `--repo` defaults to the sole repo when `productConfig.Repos` has exactly one entry; error if omitted with multiple repos. |

---

## Scope

### 1. Domain (`src/domain/Domain.fs`)

- Extend `BacklogItem` with the full set of fields required by `item.yaml`:
  - `Type: string` (validated against `feature | bug | chore | spike`; default `feature`)
  - `Priority: string option`
  - `Summary: string option`
  - `AcceptanceCriteria: string list`
  - `Dependencies: BacklogId list`
  - `CreatedAt: DateOnly`
- Rename `TakeError` → `BacklogError` and add create-specific cases:
  - `DuplicateBacklogId of BacklogId`
  - `InvalidItemType of string`
  - `MissingTitle`
- Update all existing use sites of `TakeError` to `BacklogError`.

### 2. Interfaces (`src/domain/Interfaces.fs`)

- Add `WriteBacklogItem` and `BacklogItemExists` to `IBacklogStore`:
  ```fsharp
  abstract WriteBacklogItem: coordRoot: string -> item: BacklogItem -> Result<unit, BacklogError>
  abstract BacklogItemExists: coordRoot: string -> backlogId: BacklogId -> bool
  ```
- Update existing `IBacklogStore` and `ITaskStore` signatures from `TakeError` → `BacklogError`.

### 3. Adapter (`src/adapters/YamlAdapter.fs`)

- Extend `BacklogItemDto` to include all `item.yaml` fields:
  `type`, `priority`, `summary`, `acceptance_criteria`, `dependencies`, `created_at`.
- Implement `WriteBacklogItem` on `BacklogStoreAdapter`:
  - Path: `<coordRoot>/BACKLOG/<id>/item.yaml`
  - Serialize `BacklogItem` → `BacklogItemDto` → YAML string
  - Write via `fs.WriteFile` (parent dir is auto-created by `FileSystemAdapter`)
- Implement `BacklogItemExists`:
  - Check `fs.FileExists` on the expected item path.
- Update all `TakeError` references to `BacklogError`.

### 4. Feature (`src/features/`)

- Create `src/features/Backlog/BacklogUsecase.fs` (new vertical slice).
- Add `createBacklogItem` — pure, no I/O:
  ```fsharp
  type CreateBacklogItemInput =
      { Id: string
        Title: string
        Repos: string list        // empty = default to sole repo (resolved by caller)
        Type: string option       // defaults to "feature"
        Summary: string option
        Priority: string option
        Dependencies: string list }

  let createBacklogItem
      (productConfig: ProductConfig)
      (input: CreateBacklogItemInput)
      (today: DateOnly)
      : Result<BacklogItem, BacklogError>
  ```
  Steps:
  - Validate `Id` slug via `BacklogId.tryCreate`.
  - Validate `Type` against `["feature"; "bug"; "chore"; "spike"]`; default `"feature"`.
  - Resolve repos: if `input.Repos` is empty and product has exactly one repo, use it; else validate each against `productConfig.Repos`.
  - Validate each dependency id slug.
  - Return `Ok(BacklogItem)` or `Error`.

### 5. CLI (`src/cli/Program.fs`)

- Add Argu args:
  ```fsharp
  type AddArgs =
      | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
      | [<Mandatory>] Title of string
      | Repo of string        // repeatable; optional when product has one repo
      | Item_Type of string
      | Summary of string
      | Priority of string
      | Depends_On of string  // repeatable
  ```
- Extend `BacklogArgs` with `Add of ParseResults<AddArgs>`.
- Add `handleBacklogAdd` handler:
  1. Resolve portfolio → profile → product → `coordRoot` (same pattern as `handleBacklogTake`).
  2. Load `ProductConfig`.
  3. Check duplicate: `backlogStore.BacklogItemExists coordRoot backlogId`.
  4. Call `BacklogUsecase.createBacklogItem productConfig input today`.
  5. On success: `backlogStore.WriteBacklogItem coordRoot item`.
  6. Print confirmation or JSON result.
- Add `AppDeps` delegation for the two new interface members.

---

## Dependencies / Prerequisites

- `product-init` must be complete (product.yaml readable) — satisfied per backlog dependency.
- `IBacklogStore.LoadBacklogItem` already exists; this task adds write capability alongside it.

---

## Impact on Existing Code

| File | Change |
|---|---|
| `Domain.fs` | Extend `BacklogItem`; rename `TakeError` → `BacklogError`; add 3 new cases |
| `Interfaces.fs` | Rename error type references; add 2 members to `IBacklogStore` |
| `YamlAdapter.fs` | Extend `BacklogItemDto`; rename error references; implement new members |
| `TaskUsecase.fs` | Rename `TakeError` → `BacklogError` |
| `Program.fs` | Rename error references; new args, dispatch branch, handler |

The `BacklogItem` record expansion requires updating the field mapping inside `LoadBacklogItem` — all new fields should be optional in the DTO to preserve backward compatibility with existing YAML files that lack them.

---

## Acceptance Criteria

- `itr backlog add <id> --title "..." --repo <repo-id>` creates `<coordRoot>/BACKLOG/<id>/item.yaml`.
- `item.yaml` contains `id`, `title`, `repos`, `type`, `created_at` at minimum.
- Running the command again with the same id returns a `DuplicateBacklogId` error.
- Specifying a `--repo` not in `product.yaml` returns a `RepoNotInProduct` error.
- Omitting `--repo` on a single-repo product succeeds using the sole repo.
- Omitting `--repo` on a multi-repo product returns an error.
- `--type` defaults to `feature`; values outside the allowed set are rejected with `InvalidItemType`.
- `created_at` is set to today's date automatically.
- JSON output mode (`--output json`) emits a structured result.

---

## Testing Strategy

### Acceptance tests (`tests/acceptance/BacklogAcceptanceTests.fs`)

Follow the `TaskAcceptanceTests.fs` pattern — real adapters, temp dir fixture, no mocks:

- Success: file written, YAML content correct.
- Duplicate id: second call returns `DuplicateBacklogId` error.
- Unknown repo: returns `RepoNotInProduct` error.
- Invalid type: returns `InvalidItemType` error.
- Single-repo default: omitting `--repo` succeeds when product has one repo.
- Multi-repo no repo: omitting `--repo` fails when product has multiple repos.

### Communication tests (`tests/communication/`)

- "Cannot create a backlog item with an id that already exists."
- "Cannot reference a repo not declared in product.yaml."
- "Type defaults to 'feature' when not specified."

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| `BacklogItem` expansion breaks existing YAML round-trip | Make all new DTO fields nullable/optional; existing files without them still parse cleanly |
| `TakeError` rename touches many files | Compile after rename before adding new code; let the compiler surface all use sites |
| `created_at` format inconsistency | Use `DateOnly.ToString("yyyy-MM-dd")` — consistent with `ItrTaskDto.CreatedAt` |
