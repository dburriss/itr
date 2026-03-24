# Plan: backlog-info

**Status:** Draft

---

## Description

Add a `backlog info <id>` CLI subcommand that displays the full detail of a single backlog item: all fields from `item.yaml`, computed status, view membership, task breakdown, forward dependencies, and reverse dependencies (items that depend on this one). Output supports both a rich human-readable card and structured JSON.

This task also completes `BacklogSnapshot` as the single, full read model by embedding tasks directly into `BacklogItemSummary` (removing the lossy `TaskCount: int`), and extends archived item loading to include their tasks. Markdown files (e.g. `plan.md`) are **not** part of the snapshot; they are loaded lazily on explicit demand only.

---

## Scope

### 1. Interface: `ITaskStore.ListArchivedTasks`

Archived items live at `BACKLOG/_archive/<date>-<id>/tasks/`; the existing `ListTasks` constructs the active path and cannot reach them. Add a new method to `ITaskStore` in `src/domain/Interfaces.fs`:

```fsharp
abstract ListArchivedTasks: coordRoot: string -> archiveDirName: string -> Result<ItrTask list, BacklogError>
```

`archiveDirName` is the full prefixed folder name (e.g. `2026-03-23-backlog-list`). The adapter constructs:

```
<coordRoot>/BACKLOG/_archive/<archiveDirName>/tasks/
```

Reuse the same subdirectory-scan and `mapTaskDto` logic from `ListTasks`. Return `Ok []` if the tasks directory does not exist.

Wire into `AppDeps` in `src/cli/Program.fs`.

### 2. Interface: update `IBacklogStore.ListArchivedBacklogItems`

`loadSnapshot` needs the archive folder name (e.g. `2026-03-23-backlog-list`) alongside each item in order to call `ListArchivedTasks`. Update the signature in `src/domain/Interfaces.fs`:

```fsharp
// before
abstract ListArchivedBacklogItems: coordRoot: string -> Result<BacklogItem list, BacklogError>

// after
abstract ListArchivedBacklogItems: coordRoot: string -> Result<(string * BacklogItem) list, BacklogError>
```

The string is the archive directory name. Update the adapter and all call sites (only `loadSnapshot` and tests).

### 3. Domain: replace `TaskCount` with `Tasks` in `BacklogItemSummary`

Remove `TaskCount: int` from `BacklogItemSummary` in `src/domain/Domain.fs`. Replace with the full task list:

```fsharp
type BacklogItemSummary =
    { Item: BacklogItem
      Status: BacklogItemStatus
      ViewId: string option
      Tasks: ItrTask list }
```

All consumers derive count as `summary.Tasks.Length`. The compiler will flag every construction site.

### 4. Usecase: populate `Tasks` in `loadSnapshot`

Update `loadSnapshot` in `src/features/Backlog/BacklogUsecase.fs`:

- **Active items**: call `taskStore.ListTasks coordRoot item.Id`; store result in `Tasks`.
- **Archived items**: use the archive dir name from the updated `ListArchivedBacklogItems` to call `taskStore.ListArchivedTasks coordRoot archiveDirName`; store result in `Tasks`.

`BacklogItemStatus.compute` for archived items still passes `isArchived = true` (status is always `Archived` regardless of task states), but tasks are now populated.

### 5. Domain: `BacklogItemInfo` and `getBacklogItemInfo`

Add to `src/features/Backlog/BacklogUsecase.fs`:

```fsharp
type BacklogItemInfo =
    { Summary: BacklogItemSummary
      ReverseDeps: BacklogId list }
```

```fsharp
let getBacklogItemInfo
    (backlogId: BacklogId)
    (snapshot: BacklogSnapshot)
    : Result<BacklogItemInfo, BacklogError>
```

Steps:
1. Find the item in `snapshot.Items` by id; return `BacklogItemNotFound` if absent.
2. Compute `ReverseDeps`: scan all `snapshot.Items` where `item.Item.Dependencies` contains `backlogId`.
3. Return `BacklogItemInfo` — tasks are already in `Summary.Tasks`.

Pure function; no IO.

### 6. CLI: `backlog info` subcommand

Add to `src/cli/Program.fs`:

**Argu DU** (`InfoArgs`):
- `[<MainCommand; Mandatory>] Backlog_Id of backlog_id: string`

**`BacklogArgs`** union: add `| [<CliPrefix(CliPrefix.None)>] Info of ParseResults<InfoArgs>`

**Handler** (`handleBacklogInfo`):
1. Parse and validate `backlogId`.
2. Load snapshot via `loadSnapshot`.
3. Call `getBacklogItemInfo backlogId snapshot`.
4. Emit output.

**Human output** (Spectre.Console `Panel` + `Grid`; hide sections with no content):

```
╔══════════════════════════════════════════════════╗
║  <id>                              [<type>]      ║
╠══════════════════════════════════════════════════╣
║  Title       <title>                             ║
║  Status      <status>                            ║
║  Priority    <priority or ->                     ║
║  View        <view or ->                         ║
║  Repos       <repo1>, <repo2>                    ║
║  Created     <yyyy-MM-dd>                        ║
╠══════════════════════════════════════════════════╣  ← only if summary non-empty
║  Summary                                         ║
║  <summary text>                                  ║
╠══════════════════════════════════════════════════╣  ← only if AC non-empty
║  Acceptance Criteria                             ║
║  • <criterion>                                   ║
╠══════════════════════════════════════════════════╣  ← only if deps non-empty
║  Dependencies                                    ║
║  • <dep-id>                                      ║
╠══════════════════════════════════════════════════╣  ← only if reverse deps non-empty
║  Depended on by                                  ║
║  • <rev-dep-id>                                  ║
╠══════════════════════════════════════════════════╣  ← only if tasks non-empty
║  Tasks                                           ║
║  <task-id>   <state>   <repo>                    ║
╚══════════════════════════════════════════════════╝
```

**JSON output**:

```json
{
  "id": "...",
  "type": "...",
  "priority": "...",
  "status": "...",
  "view": "...",
  "taskCount": 2,
  "createdAt": "...",
  "title": "...",
  "repos": ["..."],
  "summary": "...",
  "acceptanceCriteria": ["..."],
  "dependencies": ["..."],
  "dependedOnBy": ["..."],
  "tasks": [
    { "id": "...", "state": "...", "repo": "..." }
  ]
}
```

`taskCount` is `Tasks.Length` computed at serialisation. Empty arrays emit as `[]`; absent strings as `""`.

---

## Dependencies / Prerequisites

- `backlog-item-create` — complete.
- `backlog-list` — complete (`loadSnapshot`, `IViewStore`, `BacklogItemSummary` all in place). This task extends those foundations.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/domain/Domain.fs` | Remove `TaskCount`; add `Tasks: ItrTask list` to `BacklogItemSummary` |
| `src/domain/Interfaces.fs` | Add `ListArchivedTasks` to `ITaskStore`; update `ListArchivedBacklogItems` return type |
| `src/adapters/YamlAdapter.fs` | Implement `ListArchivedTasks`; update `ListArchivedBacklogItems` to return `(string * BacklogItem) list` |
| `src/features/Backlog/BacklogUsecase.fs` | Populate `Tasks` in `loadSnapshot` (active + archived); add `BacklogItemInfo`, `getBacklogItemInfo` |
| `src/cli/Program.fs` | Replace `s.TaskCount` with `s.Tasks.Length`; add `InfoArgs`, `handleBacklogInfo`, wire dispatch, wire `ListArchivedTasks` in `AppDeps` |
| `tests/acceptance/BacklogAcceptanceTests.fs` | Update `BacklogItemSummary` constructions; replace `TaskCount` with `Tasks.Length` |
| Any communication tests using `BacklogItemSummary` | Same — compiler will flag all sites |

---

## Acceptance Criteria

- [ ] `itr backlog info <id>` shows title, status, type, priority, view, repos, creation date.
- [ ] Summary section shown only when `summary` is non-empty.
- [ ] Acceptance criteria section shown only when list is non-empty.
- [ ] Dependencies section shown only when list is non-empty.
- [ ] "Depended on by" section shown only when reverse deps exist.
- [ ] Tasks section shown only when tasks exist; each row shows id, state, repo.
- [ ] `itr backlog info <id> --output json` emits valid JSON with all fields including `tasks` and `dependedOnBy`.
- [ ] `itr backlog info unknown-id` exits with `BacklogItemNotFound unknown-id`.
- [ ] `backlog list` still shows correct task count (derived from `Tasks.Length`).
- [ ] Existing `backlog list`, `backlog add`, and `backlog take` commands are unaffected.

---

## Testing Strategy

### Acceptance tests

1. `info shows full detail for item with tasks and deps` — write item with summary, AC, deps, one task; verify all sections present.
2. `info hides empty sections` — write minimal item (no summary, no AC, no deps, no tasks); verify only metadata section shown.
3. `info shows reverse deps` — write two items where B depends on A; verify `info A` shows B in "Depended on by".
4. `info json output is valid and complete` — parse JSON, assert all fields present including `tasks` array.
5. `info returns error for unknown id` — verify exit code and error message.
6. `backlog list task count unchanged` — verify task count column still correct after `TaskCount` removal.

### Communication tests

`getBacklogItemInfo`:
- Item found → correct tasks and reverse deps returned.
- Item not found → `BacklogItemNotFound`.
- Multiple reverse deps → all returned.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| `BacklogItemSummary` field removal breaks all construction sites | Compiler flags every site; mechanical fix |
| `IBacklogStore.ListArchivedBacklogItems` signature change | Only called in `loadSnapshot` and tests; straightforward update |
| Archived tasks dir may not exist | `ListArchivedTasks` returns `Ok []` when dir absent |
| Snapshot loads all task YAMLs eagerly | Acceptable for MVP; design allows lazy loading later |
| `plan.md` or other markdown files inadvertently loaded | Not part of snapshot — only `task.yaml` files are read |
