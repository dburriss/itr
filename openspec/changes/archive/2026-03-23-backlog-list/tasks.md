## 1. Fix TaskState (bug fix, enables all downstream work)

- [x] 1.1 Add `Planned` and `Approved` cases to `TaskState` DU in `src/domain/Domain.fs`
- [x] 1.2 Update `mapTaskState` in `src/adapters/YamlAdapter.fs` to map `"planned"` → `Planned` and `"approved"` → `Approved`
- [x] 1.3 Update `taskStateToString` in `src/adapters/YamlAdapter.fs` to serialise `Planned` → `"planned"` and `Approved` → `"approved"`
- [x] 1.4 Fix all exhaustive `TaskState` match expressions that fail to compile (check `Program.fs`, `BacklogUsecase.fs`, test files)
- [x] 1.5 Add communication tests: `mapTaskState` round-trip for `planned` and `approved` in `tests/communication/BacklogDomainTests.fs`

## 2. Domain Types

- [x] 2.1 Add `BacklogItemStatus` DU (`Created | Planning | Planned | Approved | InProgress | Completed | Archived`) to `src/domain/Domain.fs`
- [x] 2.2 Add `BacklogItemStatus.compute (tasks: ItrTask list) (isArchived: bool) : BacklogItemStatus` pure function in `src/domain/Domain.fs`
- [x] 2.3 Add `BacklogItemSummary` and `BacklogSnapshot` record types to `src/domain/Domain.fs`
- [x] 2.4 Add communication tests for `BacklogItemStatus.compute` (one per variant) in `tests/communication/BacklogDomainTests.fs`

## 3. Interfaces

- [x] 3.1 Add `ListBacklogItems: coordRoot: string -> Result<BacklogItem list, BacklogError>` to `IBacklogStore` in `src/domain/Interfaces.fs`
- [x] 3.2 Add `BacklogView` record type to `src/domain/Interfaces.fs`
- [x] 3.3 Add `IViewStore` interface with `ListViews: coordRoot: string -> Result<BacklogView list, BacklogError>` to `src/domain/Interfaces.fs`

## 4. Adapter Implementations

- [x] 4.1 Implement `ListBacklogItems` in `BacklogStoreAdapter` (`src/adapters/YamlAdapter.fs`): enumerate `<coordRoot>/BACKLOG/` dirs, skip names starting with `_`, load `item.yaml` from each, surface first error
- [x] 4.2 Add `BacklogViewDto` CLIMutable DTO (`id`, `description`, `items: string array`) in `src/adapters/YamlAdapter.fs`
- [x] 4.3 Implement `ViewStoreAdapter` in `src/adapters/YamlAdapter.fs`: enumerate `<coordRoot>/BACKLOG/_views/*.yaml`, deserialise each, return `Ok []` if directory absent

## 5. Use Cases

- [x] 5.1 Add `BacklogListFilter` record type (`ViewId`, `Status`, `ItemType` all optional) in `src/features/Backlog/BacklogUsecase.fs`
- [x] 5.2 Implement `loadSnapshot (coordRoot: string)` in `src/features/Backlog/BacklogUsecase.fs`: load items, load views, build item→viewId map (first-match, warn on duplicate), load tasks per item, compute status, assemble `BacklogSnapshot` sorted by `CreatedAt`
- [x] 5.3 Implement `listBacklogItems (filter: BacklogListFilter) (snapshot: BacklogSnapshot)` pure function in `src/features/Backlog/BacklogUsecase.fs`

## 6. CLI Wiring

- [x] 6.1 Add `ListArgs` Argu DU (`--view`, `--status`, `--type` flags) in `src/cli/Program.fs`
- [x] 6.2 Add `| List of ParseResults<ListArgs>` case to `BacklogArgs` in `src/cli/Program.fs`
- [x] 6.3 Implement `handleBacklogList` handler: resolve coord root, call `loadSnapshot` then `listBacklogItems`, render Spectre.Console table (human) or JSON array; emit multi-view warnings to stderr first
- [x] 6.4 Wire `List` dispatch in the `BacklogArgs` match in `src/cli/Program.fs`

## 7. Composition Root

- [x] 7.1 Wire `ViewStoreAdapter` as `IViewStore` into `AppDeps` in the composition root

## 8. Acceptance Tests

- [x] 8.1 `list returns all active items sorted by creation date` — 3 items with known dates, verify order
- [x] 8.2 `list filtered by view returns only matching items` — write `_views/test-view.yaml`, verify filter
- [x] 8.3 `list filtered by type returns only matching items` — mix of feature/bug, filter by bug
- [x] 8.4 `list with no items returns empty`
- [x] 8.5 `task count is correct` — item with 2 task directories, verify count = 2
- [x] 8.6 `multi-view membership warns and first-match wins`
