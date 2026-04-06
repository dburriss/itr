## 1. Update BacklogListFilter type

- [x] 1.1 Add `ExcludeStatuses: BacklogItemStatus list` field to `BacklogListFilter` in `BacklogUsecase.fs`
- [x] 1.2 Add `OrderBy: string option` field to `BacklogListFilter` in `BacklogUsecase.fs`
- [x] 1.3 Update all call sites that construct `BacklogListFilter` to include the new fields (default `ExcludeStatuses = []`, `OrderBy = None`)

## 2. Remove sort from loadSnapshot

- [x] 2.1 Remove `List.sortBy (fun s -> s.Item.CreatedAt)` from `loadSnapshot` in `BacklogUsecase.fs`

## 3. Implement ordering and exclusion in listBacklogItems

- [x] 3.1 Add exclusion filter: remove items whose `Status` is in `filter.ExcludeStatuses`
- [x] 3.2 Add default archived exclusion: when `filter.Status` is not `Some Archived`, add `Archived` to effective exclude list (unless already handled by `ExcludeStatuses`)
- [x] 3.3 Define priority order mapping (`"high"` → 0, `"medium"` → 1, `"low"` → 2, other/None → 3) with case-insensitive comparison
- [x] 3.4 Define type order mapping (`Bug` → 0, `Feature` → 1, `Chore` → 2, `Spike` → 3)
- [x] 3.5 Implement default multi-key sort: type → priority → `CreatedAt` ascending
- [x] 3.6 Implement view-based ordering: when `filter.ViewId` is `Some`, look up the view and sort by item index in `view.Items`; items not in view list append at end in default sort order
- [x] 3.7 Implement `--order-by` override: when `filter.OrderBy = Some "created"`, sort by `CreatedAt` only; `"priority"` → priority only; `"type"` → type only

## 4. Add CLI arguments

- [x] 4.1 Add `--exclude` argument to `ListArgs` in `Program.fs` (Argu, multiple allowed, type `string`)
- [x] 4.2 Add `--order-by` argument to `ListArgs` in `Program.fs` (Argu, optional, type `string`)
- [x] 4.3 Update `handleBacklogList` to parse `--exclude` values and convert to `BacklogItemStatus list`
- [x] 4.4 Update `handleBacklogList` to parse `--order-by` value and pass as `OrderBy` to filter
- [x] 4.5 Ensure default `BacklogListFilter` constructed in `handleBacklogList` omits archived by default (set `ExcludeStatuses = [Archived]` unless `--status archived` was passed)

## 5. Update acceptance tests

- [x] 5.1 Update existing tests in `BacklogAcceptanceTests.fs` that expected archived items in default `backlog list` output — they should now be excluded
- [x] 5.2 Update or add test for default multi-key sort order (type → priority → created)
- [x] 5.3 Add test for `--exclude` single status
- [x] 5.4 Add test for `--exclude` multiple statuses
- [x] 5.5 Add test for `--view` ordering respecting view item sequence
- [x] 5.6 Add test for `--order-by created`
- [x] 5.7 Add test for `--order-by priority`
- [x] 5.8 Add test for `--order-by type`

## 6. Verify

- [x] 6.1 Run `dotnet build` and fix any compilation errors
- [x] 6.2 Run `dotnet test` and fix any test failures
