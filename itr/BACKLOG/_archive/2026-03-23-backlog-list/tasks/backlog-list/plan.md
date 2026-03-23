# Plan: backlog-list

**Status:** Draft

---

## Description

Add a `backlog list` CLI command that lists all backlog items for the active product's coordination root, with optional filtering by view, type, and status. Status is computed from the item's position in the filesystem and its associated task states. Output supports both human-readable (table) and structured (JSON) modes.

---

## Scope

### 1. Domain: fix `TaskState` and add `Approved`

`TaskState` is currently missing `Planned` and `Approved`. The adapter's `mapTaskState` silently maps unknown strings (including `"planned"`) to `Planning`, which means existing `task.yaml` files with `state: planned` are misread. Fix this as part of this task.

Update `TaskState` in `src/domain/Domain.fs`:

```fsharp
type TaskState =
    | Planning      // task created, no plan
    | Planned       // plan written, not yet approved
    | Approved      // plan approved, ready to start
    | InProgress
    | Implemented
    | Validated
```

Update `mapTaskState` and `taskStateToString` in `src/adapters/YamlAdapter.fs`:

```
"planning"    → Planning
"planned"     → Planned
"approved"    → Approved
"in_progress" → InProgress
"implemented" → Implemented
"validated"   → Validated
```

> This is a **bug fix**: `state: planned` in existing task files was silently falling through to `Planning`.

### 2. Domain: `BacklogItemStatus` and computed status logic

Add to `src/domain/Domain.fs`:

```fsharp
type BacklogItemStatus =
    | Created       // item exists, no tasks
    | Planning      // has a task in Planning state
    | Planned       // all tasks Planned (plan written, not approved)
    | Approved      // all tasks Approved (plan approved, ready to start)
    | InProgress    // at least one task InProgress
    | Completed     // all tasks Implemented or Validated
    | Archived      // item is under BACKLOG/_archive/
```

Add a pure function:

```fsharp
module BacklogItemStatus =
    let compute (tasks: ItrTask list) (isArchived: bool) : BacklogItemStatus
```

Rules (evaluated in order):
1. `Archived` — `isArchived = true`.
2. `Completed` — tasks non-empty and all are `Implemented` or `Validated`.
3. `InProgress` — any task is `InProgress`.
4. `Approved` — tasks non-empty and all are `Approved` (or further).
5. `Planned` — tasks non-empty and all are `Planned` (or further, but not yet Approved).
6. `Planning` — tasks exist but none have advanced past `Planning`.
7. `Created` — no tasks.

> Plan detection is now fully driven by `task.State` — no filesystem `plan.md` check required.

### 3. Domain: `BacklogSnapshot` aggregate read model

Add to `src/domain/Domain.fs`:

```fsharp
type BacklogItemSummary =
    { Item: BacklogItem
      Tasks: ItrTask list
      Status: BacklogItemStatus
      ViewId: string option }

type BacklogSnapshot =
    { Items: BacklogItemSummary list }
```

This is loaded once per command invocation and provides a flexible base for current and future commands (`backlog list`, `backlog next`, etc.).

### 4. Interface: `IBacklogStore.ListBacklogItems`

Add to `IBacklogStore` in `src/domain/Interfaces.fs`:

```fsharp
abstract ListBacklogItems: coordRoot: string -> Result<BacklogItem list, BacklogError>
```

Implement in `BacklogStoreAdapter` (`src/adapters/YamlAdapter.fs`):
- Enumerate `<coordRoot>/BACKLOG/` directories; skip entries whose name starts with `_`.
- Load `item.yaml` from each using existing parse logic.
- Return all items; surface first error if any parse fails.

### 5. Interface: `IViewStore` (read-only)

Add to `src/domain/Interfaces.fs`:

```fsharp
type BacklogView = { Id: string; Description: string option; Items: string list }

type IViewStore =
    abstract ListViews: coordRoot: string -> Result<BacklogView list, BacklogError>
```

Implement `ViewStoreAdapter` in `src/adapters/YamlAdapter.fs`:
- Add `BacklogViewDto` CLIMutable DTO (`id`, `description`, `items: string array`).
- Enumerate `<coordRoot>/BACKLOG/_views/*.yaml` and deserialise each.
- Return empty list if `_views/` does not exist.
- **Multi-view detection**: if the same item id appears in more than one view, emit a warning to stderr. First-match wins for `ViewId` assignment.

### 6. Usecase: `BacklogUsecase.loadSnapshot`

New function in `src/features/Backlog/BacklogUsecase.fs`:

```fsharp
let loadSnapshot (coordRoot: string)
    : EffectResult<#IBacklogStore * #ITaskStore * #IViewStore, BacklogSnapshot, BacklogError>
```

Steps:
1. Load all items via `IBacklogStore.ListBacklogItems`.
2. Load all views via `IViewStore.ListViews`.
3. Build `itemId → viewId` map (first-match); emit warning for duplicates.
4. For each item:
   a. Scan task directories via `ITaskStore.ListTasks`.
   b. Compute `BacklogItemStatus.compute tasks isArchived=false`.
   c. Assemble `BacklogItemSummary`.
5. Return `BacklogSnapshot` with items sorted by `CreatedAt` ascending.

### 7. Usecase: `BacklogUsecase.listBacklogItems`

```fsharp
type BacklogListFilter =
    { ViewId: string option
      Status: BacklogItemStatus option   // None → exclude Archived
      ItemType: BacklogItemType option }

let listBacklogItems (filter: BacklogListFilter) (snapshot: BacklogSnapshot) : BacklogItemSummary list
```

Pure function over a loaded snapshot:
1. Exclude `Archived` unless `Status = Some Archived` is explicitly requested.
2. Apply `ViewId` filter if provided.
3. Apply `Status` filter if provided.
4. Apply `ItemType` filter if provided.
5. Return filtered list (order preserved from snapshot — sorted by `CreatedAt`).

### 8. CLI: `backlog list` subcommand

Add to `src/cli/Program.fs`:

**Argu DU** (`ListArgs`):
- `--view <view-id>` — filter by view id
- `--status <status>` — filter by computed status (`created|planning|planned|approved|in-progress|completed|archived`)
- `--type <type>` — filter by item type (`feature|bug|chore|spike`)

**`BacklogArgs`** union: add `| [<CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>`

**Handler** (`handleBacklogList`):
- Resolve product and coordination root.
- Call `loadSnapshot` then `listBacklogItems` with parsed filter.
- Human output: Spectre.Console table — columns: `ID | Type | Priority | Status | View | Tasks | Created`.
- JSON output: array of objects with the same fields.
- Multi-view warnings emitted to stderr before table output.

### 9. Composition root (`AppDeps`)

Wire `IViewStore` into `AppDeps`. `IBacklogStore` gains a new method; `TaskState` gains new cases — both require corresponding updates in `AppDeps` and the adapter.

---

## Dependencies / Prerequisites

- `backlog-item-create` task complete (it is — state: `planned`).
- `ITaskStore` already implemented and wired; reuse as-is.
- `backlog-view-create` and `backlog-view-membership` are **not** prerequisites — views are read from existing YAML files.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/domain/Domain.fs` | Add `Planned`, `Approved` to `TaskState`; add `BacklogItemStatus`, `BacklogItemSummary`, `BacklogSnapshot` |
| `src/domain/Interfaces.fs` | Add `ListBacklogItems` to `IBacklogStore`; add `BacklogView`, `IViewStore` |
| `src/adapters/YamlAdapter.fs` | Fix `mapTaskState`/`taskStateToString` for `Planned`/`Approved`; implement `ListBacklogItems`; add `ViewStoreAdapter` |
| `src/features/Backlog/BacklogUsecase.fs` | Add `loadSnapshot`, `BacklogListFilter`, `listBacklogItems` |
| `src/cli/Program.fs` | Add `ListArgs`, extend `BacklogArgs`, add `handleBacklogList`, wire dispatch |

The `TaskState` change is a **breaking fix**: any exhaustive match on `TaskState` elsewhere must be updated. Check `Program.fs` and any communication/acceptance tests.

---

## Acceptance Criteria

- [ ] `itr backlog list` shows all active (non-archived) items sorted by creation date.
- [ ] `itr backlog list --view <id>` shows only items in that view.
- [ ] `itr backlog list --status approved` shows only items with computed status `approved`.
- [ ] `itr backlog list --type bug` shows only bug items.
- [ ] `itr backlog list --output json` emits a JSON array with all fields.
- [ ] Each row/object includes: id, type, priority, status, view, task count, creation date.
- [ ] Status computation:
  - No tasks → `created`
  - Tasks all in `Planning` → `planning`
  - Tasks all `Planned` or above but not `Approved` → `planned`
  - Tasks all `Approved` or above but not `InProgress` → `approved`
  - Any task `InProgress` → `in-progress`
  - All tasks `Implemented`/`Validated` → `completed`
- [ ] `state: planned` in existing task files is no longer misread as `Planning`.
- [ ] Multi-view membership prints a warning to stderr; first-match view is used.
- [ ] Existing `backlog add` and `backlog take` commands are unaffected.

---

## Testing Strategy

### Acceptance tests (`tests/acceptance/BacklogAcceptanceTests.fs`)

1. `list returns all active items sorted by creation date` — 3 items with known dates, verify order.
2. `list filtered by view returns only matching items` — write `_views/test-view.yaml`, verify filter.
3. `list filtered by type returns only matching items` — mix of feature/bug, filter by bug.
4. `list with no items returns empty`.
5. `task count is correct` — item with 2 task directories, verify count = 2.
6. `multi-view membership warns and first-match wins`.

### Communication tests (`tests/communication/BacklogDomainTests.fs`)

`BacklogItemStatus.compute` — one test per status variant (pure function, no IO):
- No tasks → `Created`
- Tasks in `Planning` → `Planning`
- Tasks in `Planned` → `Planned`
- Tasks in `Approved` → `Approved`
- Any task `InProgress` → `InProgress`
- All tasks `Implemented`/`Validated` → `Completed`

`mapTaskState` round-trip — verify `planned` and `approved` serialise and deserialise correctly (adapter-level test or building test).

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| `TaskState` exhaustive matches break at compile time | Compiler will flag all incomplete matches; fix is mechanical |
| `_views/` doesn't exist | `ViewStoreAdapter` guards with `Directory.Exists`; returns `Ok []` |
| Malformed `item.yaml` in BACKLOG dir | Surface first parse error; do not silently skip |
| `loadSnapshot` slow for large backlogs | Acceptable for MVP; snapshot design allows future caching |
| Multi-view membership confusion | Warn on stderr, first-match wins; write-time enforcement in future view commands |
