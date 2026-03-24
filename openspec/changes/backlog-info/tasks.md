## 1. Domain

- [ ] 1.1 Add `BacklogItemDetail` record to `src/domain/Domain.fs` with fields: `Item`, `Status`, `ViewId`, `Tasks`

## 2. Feature Layer

- [ ] 2.1 Add `getBacklogItemDetail` function to `src/features/Backlog/BacklogUsecase.fs` that loads item, tasks, views, computes status, and returns `BacklogItemDetail`

## 3. CLI

- [ ] 3.1 Add `InfoArgs` DU to `src/cli/Program.fs` with `Backlog_Id` main command and `Output` option
- [ ] 3.2 Add `Info` case to `BacklogArgs` DU
- [ ] 3.3 Implement `handleBacklogInfo` handler with human table and JSON output modes
- [ ] 3.4 Wire `Info` into the `dispatch` function

## 4. Tests

- [ ] 4.1 Add unit tests for `getBacklogItemDetail` (valid item, not found, no tasks, with tasks)
- [ ] 4.2 Add unit tests for computed status in detail view (Created when no tasks, Approved when all tasks approved)
- [ ] 4.3 Verify build passes: `dotnet build`
- [ ] 4.4 Verify all tests pass: `dotnet test`
