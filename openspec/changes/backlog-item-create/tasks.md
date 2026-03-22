## 1. Domain

- [ ] 1.1 Extend `BacklogItem` record with `Type`, `Priority`, `Summary`, `AcceptanceCriteria`, `Dependencies`, `CreatedAt` fields
- [ ] 1.2 Rename `TakeError` → `BacklogError` in `Domain.fs`
- [ ] 1.3 Add `DuplicateBacklogId`, `InvalidItemType`, `MissingTitle` cases to `BacklogError`

## 2. Interfaces

- [ ] 2.1 Update `IBacklogStore` and `ITaskStore` signatures: `TakeError` → `BacklogError`
- [ ] 2.2 Add `WriteBacklogItem` and `BacklogItemExists` abstract members to `IBacklogStore`

## 3. Adapter

- [ ] 3.1 Extend `BacklogItemDto` with optional fields: `type`, `priority`, `summary`, `acceptance_criteria`, `dependencies`, `created_at`
- [ ] 3.2 Update `LoadBacklogItem` to map new DTO fields onto `BacklogItem` (with safe defaults for missing fields)
- [ ] 3.3 Implement `BacklogItemExists` on `BacklogStoreAdapter` (check file path via `fs.FileExists`)
- [ ] 3.4 Implement `WriteBacklogItem` on `BacklogStoreAdapter` (serialize `BacklogItem` → `BacklogItemDto` → YAML, write via `fs.WriteFile`)
- [ ] 3.5 Update all `TakeError` references to `BacklogError` in `YamlAdapter.fs`

## 4. Feature

- [ ] 4.1 Create `src/features/Backlog/BacklogUsecase.fs` with `CreateBacklogItemInput` type
- [ ] 4.2 Implement pure `createBacklogItem` function: validate id, type, repos, dependencies; return `Result<BacklogItem, BacklogError>`
- [ ] 4.3 Update `TaskUsecase.fs`: rename `TakeError` → `BacklogError`
- [ ] 4.4 Add `BacklogUsecase.fs` to the project `.fsproj` file

## 5. CLI

- [ ] 5.1 Add `AddArgs` DU to `Program.fs` with `Backlog_Id`, `Title`, `Repo`, `Item_Type`, `Summary`, `Priority`, `Depends_On`
- [ ] 5.2 Extend `BacklogArgs` with `Add of ParseResults<AddArgs>` case
- [ ] 5.3 Implement `handleBacklogAdd` handler: resolve coordRoot, check duplicate, call usecase, write item, print result
- [ ] 5.4 Add dispatch branch for `Add` in the backlog handler
- [ ] 5.5 Add `AppDeps` delegation for `WriteBacklogItem` and `BacklogItemExists`
- [ ] 5.6 Update all `TakeError` references to `BacklogError` in `Program.fs`

## 6. Tests

- [ ] 6.1 Create `tests/acceptance/BacklogAcceptanceTests.fs`: success, duplicate id, unknown repo, invalid type, single-repo default, multi-repo no repo
- [ ] 6.2 Add communication tests: duplicate id message, unknown repo message, type default behavior
- [ ] 6.3 Add `BacklogAcceptanceTests.fs` to the test project `.fsproj` file
- [ ] 6.4 Run `dotnet build` and fix any compilation errors
- [ ] 6.5 Run `dotnet test` and ensure all tests pass
