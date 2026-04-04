

# Adds the absolute path to backlog item.yaml in the backlog info and list commands

**Task ID:** backlog-item-path
**Backlog Item:** backlog-item-path
**Repo:** itr

## Description

Adds the absolute path to `item.yaml` as a column in `backlog list --output text` and as a field in `backlog info` output formats. Also centralises all path construction into qualified modules in `Domain.fs` and replaces existing inline `Path.Combine` call sites in `YamlAdapter.fs` and `Program.fs`. All four `IBacklogStore` read methods return the resolved path alongside the item for a consistent interface — active items construct the path via `BacklogItem.itemFile` inside the adapter, archived items surface the path already resolved during the directory scan.

## Scope

### Included
- Add `BacklogItem` module to `src/domain/Domain.fs` with `itemFile` and `itemDir` functions
- Add `ItrTask` module to `src/domain/Domain.fs` with `taskFile`, `planFile`, and `taskDir` functions
- Change all four read methods on `IBacklogStore` in `src/domain/Interfaces.fs` to return the resolved path alongside the item
- Update `src/adapters/YamlAdapter.fs`: replace inline `Path.Combine` call sites with modules; return path from all four read methods
- Update all call sites of `LoadBacklogItem` and `ListBacklogItems` outside the adapter that don't need the path to ignore it
- Update `AppDeps` pass-through delegators in `src/cli/Program.fs` to match the new return types
- Replace inline `Path.Combine` call sites in `src/cli/Program.fs` (`handleTaskInfo`, `handleTaskPlan`, `handleTaskApprove`) with `ItrTask.planFile`
- Add `Path: string` field to `BacklogItemSummary` and `BacklogItemDetail` in `src/domain/Domain.fs`
- Update `loadSnapshot` and `getBacklogItemDetail` in `src/features/Backlog/BacklogUsecase.fs` to populate `Path` by destructuring `(item, path)` from all store calls
- Update `backlog list` output (text, JSON, table formats) in `src/cli/Program.fs`
- Update `backlog info` output (text, JSON, table formats) in `src/cli/Program.fs`

### Excluded
- Any changes to task list/info commands (separate backlog item)
- Changes to file storage format (path is computed, not stored in YAML)

## Steps

1. **Add path construction modules to `Domain.fs`**
   - Add `BacklogItem` module immediately after the `BacklogItem` type; `itemDir` first so `itemFile` can reuse it:
     ```fsharp
     [<RequireQualifiedAccess>]
     module BacklogItem =
         let itemDir (coordRoot: string) (id: BacklogId) =
             System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value id)
         let itemFile (coordRoot: string) (id: BacklogId) =
             System.IO.Path.Combine(itemDir coordRoot id, "item.yaml")
     ```
   - Add `ItrTask` module immediately after the `ItrTask` type; `taskDir` first, reusing `BacklogItem.itemDir`, so `taskFile` and `planFile` can reuse it:
     ```fsharp
     [<RequireQualifiedAccess>]
     module ItrTask =
         let taskDir (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) =
             System.IO.Path.Combine(BacklogItem.itemDir coordRoot backlogId, "tasks", TaskId.value taskId)
         let taskFile (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) =
             System.IO.Path.Combine(taskDir coordRoot backlogId taskId, "task.yaml")
         let planFile (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) =
             System.IO.Path.Combine(taskDir coordRoot backlogId taskId, "plan.md")
     ```

2. **Update `IBacklogStore` interface in `Interfaces.fs`**
   - `LoadBacklogItem`: `Result<BacklogItem, BacklogError>` → `Result<BacklogItem * string, BacklogError>`
   - `LoadArchivedBacklogItem`: `Result<BacklogItem option, BacklogError>` → `Result<(BacklogItem * string) option, BacklogError>`
   - `ListBacklogItems`: `Result<BacklogItem list, BacklogError>` → `Result<(BacklogItem * string) list, BacklogError>`
   - `ListArchivedBacklogItems`: `Result<BacklogItem list, BacklogError>` → `Result<(BacklogItem * string) list, BacklogError>`

3. **Update `YamlAdapter.fs`**
   - Replace inline `Path.Combine` call sites with path construction modules:
     - `LoadBacklogItem`: use `BacklogItem.itemFile coordRoot backlogId`; return `Ok(item, path)`
     - `BacklogItemExists`: `BacklogItem.itemFile coordRoot backlogId`
     - `WriteBacklogItem`: `BacklogItem.itemFile coordRoot backlogId`
     - `ArchiveBacklogItem` source dir: `BacklogItem.itemDir coordRoot backlogId`
     - `ListTasks` task.yaml path: `ItrTask.taskFile coordRoot backlogId taskId`
     - `WriteTask`: `ItrTask.taskFile coordRoot backlogId taskId`
     - `ArchiveTask` dir: `ItrTask.taskDir coordRoot backlogId taskId`
   - `ListBacklogItems`: internal self-call to `LoadBacklogItem` now returns `(item, path)` — propagate both up; return `(BacklogItem * string) list`
   - `LoadArchivedBacklogItem`: return `Ok(Some(item, path))` — `path` is already a local `let` in the scan loop
   - `ListArchivedBacklogItems`: return `Ok(Some(item, path))` per entry — `path` is already a local `let` in the map

4. **Update `AppDeps` pass-through in `Program.fs`**
   - Update all four delegating `IBacklogStore` members to forward the new return types unchanged

5. **Update call sites that don't need the path**
   - `Program.fs:738` (task update flow): `let (backlogItem, _) = ...` to ignore path
   - `Program.fs:1416` (backlog info / task list handler): `let (backlogItem, _) = ...` to ignore path
   - `InteractivePrompts.fs:165` (dependency multi-select): `|> Result.map (List.map fst)` to discard paths
   - `tests/acceptance/TaskAcceptanceTests.fs:94`: `let (backlogItem, _) = ...` to ignore path

6. **Replace `Program.fs` inline path sites**
   - `handleTaskInfo` plan path: `ItrTask.planFile coordRoot backlogId taskId`
   - `handleTaskPlan` plan write path: `ItrTask.planFile coordRoot backlogId taskId`
   - `handleTaskApprove` plan exists check: `ItrTask.planFile coordRoot backlogId taskId`
   - `handleBacklogAdd` display path (lines 1545, 1588): `BacklogItem.itemFile coordRoot backlogId`
   - `handleBacklogTake` display path (line 1439): `ItrTask.taskFile coordRoot backlogId taskId`

7. **Add `Path` field to domain types**
   - Add `Path: string` field to `BacklogItemSummary` in `src/domain/Domain.fs`
   - Add `Path: string` field to `BacklogItemDetail` in `src/domain/Domain.fs`

8. **Update `loadSnapshot` usecase**
   - Active items: destructure `(item, path)` from `ListBacklogItems` result; use `path` directly
   - Archived items: destructure `(item, path)` from `ListArchivedBacklogItems` result; use `path` directly

9. **Update `getBacklogItemDetail` usecase**
   - Active item: destructure `(item, path)` from `LoadBacklogItem` result; use `path` directly
   - Archived item: destructure `(item, path)` from `LoadArchivedBacklogItem` result; use `path` directly

10. **Update `backlog list` CLI handler**
    - Text output: append `path` as 9th tab-separated column (column index 8), preserving existing column order:
      `id\ttype\tpriority\tstatus\tview\ttaskCount\tcreatedAt\ttitle\tpath`
    - JSON output: add `"path"` as last field
    - Table output: add `Path` as last column

11. **Update `backlog info` CLI handler**
    - Text output: add `path` as a key-value line
    - JSON output: add `"path"` field
    - Table output: add `Path` row

12. **Build and test**
    - Run `dotnet build` to verify compilation
    - Run `dotnet test` to verify no regressions

## Dependencies

- None (path construction modules are added in the same task)

## Acceptance Criteria

- `BacklogItem.itemFile`/`itemDir` and `ItrTask.taskFile`/`planFile`/`taskDir` modules exist in `Domain.fs`
- All existing inline `Path.Combine` call sites in `YamlAdapter.fs` and `Program.fs` are replaced
- All four `IBacklogStore` read methods return the resolved path alongside the item
- Active item paths are constructed via `BacklogItem.itemFile` inside the adapter
- Archived item paths resolve to the actual `_archive/{date}-{id}/item.yaml` location
- `backlog list --output text` emits exactly 9 tab-separated columns with `path` last
- `backlog list --output json` includes `path` field
- `backlog list --output table` includes `Path` column
- `backlog info` includes path in all three output formats
- Paths are absolute

## Impact

- **Files changed:**
  - `src/domain/Domain.fs` — add `BacklogItem` and `ItrTask` modules; add `Path` field to `BacklogItemSummary` and `BacklogItemDetail`
  - `src/domain/Interfaces.fs` — update all four read method return types on `IBacklogStore`
  - `src/adapters/YamlAdapter.fs` — replace inline path construction (7 call sites); return path from all four read methods
  - `src/features/Backlog/BacklogUsecase.fs` — destructure `(item, path)` in `loadSnapshot` and `getBacklogItemDetail`
  - `src/cli/Program.fs` — update 4 `AppDeps` delegators; update 2 call sites to ignore path; replace 5 inline path sites; include path in all output formats for `backlog list` and `backlog info`
  - `src/cli/InteractivePrompts.fs` — discard path from `ListBacklogItems` result
  - `tests/acceptance/TaskAcceptanceTests.fs` — update `LoadBacklogItem` call to ignore path
- **Interfaces affected:** `IBacklogStore` — all four read method return types change
- **Data migrations:** None (path is computed, not stored)

## Risks

- **Low risk:** Path construction is pure string manipulation with no I/O — no behaviour change, only consolidation
- **Medium risk:** `IBacklogStore` interface change touches 7 files; several call sites ignore the path entirely
- **Mitigation:** Build step will catch all missed call sites at compile time. No test fakes exist — only the real adapter implements the interface, so no fakes need updating.
