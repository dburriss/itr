## 1. Domain Types

- [ ] 1.1 Add `BacklogId` slug-validated string wrapper to `Domain.fs` (same regex as `ProductId`)
- [ ] 1.2 Add `TaskId` slug-validated string wrapper to `Domain.fs`
- [ ] 1.3 Add `RepoId` type alias to `Domain.fs`
- [ ] 1.4 Add `TaskState` DU (`Planning | InProgress | Implemented | Validated`) to `Domain.fs`
- [ ] 1.5 Add `BacklogItem` record (`Id`, `Title`, `Repos`) to `Domain.fs`
- [ ] 1.6 Add `ItrTask` record (`Id`, `SourceBacklog`, `Repo`, `State`, `CreatedAt`) to `Domain.fs`
- [ ] 1.7 Add `ProductConfig` and `RepoConfig` records to `Domain.fs`
- [ ] 1.8 Add `TakeError` DU (`ProductConfigNotFound`, `ProductConfigParseError`, `BacklogItemNotFound`, `RepoNotInProduct`, `TaskIdConflict`, `TaskIdOverrideRequiresSingleRepo`) to `Domain.fs`

## 2. Interfaces

- [ ] 2.1 Update `IYamlService` in `Interfaces.fs` to use typed `Result<'a, string>` signature for `Parse` (already done per current code — verify and fill in `ParseError` type if needed)
- [ ] 2.2 Add `IProductConfig` interface to `Interfaces.fs` (`LoadProductConfig : string -> Result<ProductConfig, TakeError>`)
- [ ] 2.3 Add `IBacklogStore` interface to `Interfaces.fs` (`LoadBacklogItem : string -> BacklogId -> Result<BacklogItem, TakeError>`)
- [ ] 2.4 Add `ITaskStore` interface to `Interfaces.fs` (`ListTasks : string -> BacklogId -> Result<ItrTask list, TakeError>` and `WriteTask : string -> ItrTask -> Result<unit, TakeError>`)

## 3. YAML Adapter

- [ ] 3.1 Add `YamlDotNet` package reference to `Itr.Adapters.fsproj`
- [ ] 3.2 Create `src/adapters/YamlAdapter.fs` with CLIMutable DTOs for `BacklogItem`, `ProductConfig`, and `ItrTask` using `[<YamlMember(Alias="...")>]` for snake_case field mapping
- [ ] 3.3 Implement `IYamlService` in `YamlAdapter.fs` using `YamlDotNet`
- [ ] 3.4 Implement `IProductConfig` in `YamlAdapter.fs` (reads `<coordRoot>/product.yaml`)
- [ ] 3.5 Implement `IBacklogStore` in `YamlAdapter.fs` (reads `<coordRoot>/BACKLOG/items/<backlog-id>.yaml`)
- [ ] 3.6 Implement `ITaskStore` in `YamlAdapter.fs` (reads/writes `<coordRoot>/TASKS/<backlog-id>/<task-id>-task.yaml`)
- [ ] 3.7 Add `YamlAdapter.fs` to `Itr.Adapters.fsproj` compile order

## 4. Use Case

- [ ] 4.1 Create `src/features/Task/TaskUsecase.fs`
- [ ] 4.2 Implement `takeBacklogItem` pure function that takes adapters + inputs and returns `Result<ItrTask list, TakeError>` (no I/O — returns tasks for caller to write)
- [ ] 4.3 Implement `TaskId` derivation logic: single-repo/no-existing → backlog id; single-repo/re-take or multi-repo → `<repo-id>-<backlog-id>`; numeric suffix on further collision
- [ ] 4.4 Implement `--task-id` override path with `TaskIdConflict` and `TaskIdOverrideRequiresSingleRepo` validation
- [ ] 4.5 Add `TaskUsecase.fs` to `Itr.Features.fsproj` compile order

## 5. CLI Command

- [ ] 5.1 Add `TakeArgs` Argu DU with `BacklogId` positional and `TaskId` optional flag to `Program.fs`
- [ ] 5.2 Add `BacklogArgs` Argu DU with `Take` subcommand to `Program.fs`
- [ ] 5.3 Add `Backlog` case to the top-level `CliArgs` DU in `Program.fs`
- [ ] 5.4 Wire `AppDeps` with `IProductConfig`, `IBacklogStore`, and `ITaskStore` implementations from `YamlAdapter`
- [ ] 5.5 Implement dispatch: `loadPortfolio → resolveActiveProfile → resolveProduct → takeBacklogItem → write tasks`
- [ ] 5.6 Implement human output: print each created task id and file path
- [ ] 5.7 Implement JSON output for `--output json`: `{ "ok": true, "tasks": [ { "id": "...", "path": "..." } ] }`

## 6. Tests

- [ ] 6.1 Add communication tests for `TaskId` derivation: single-repo/no-existing, single-repo/re-take, multi-repo
- [ ] 6.2 Add communication tests for `--task-id` override: happy path and `TaskIdConflict`
- [ ] 6.3 Add communication test for `TaskIdOverrideRequiresSingleRepo`
- [ ] 6.4 Add communication test for `RepoNotInProduct` validation
- [ ] 6.5 Add acceptance test with temp dir fixture: take a single-repo item, assert task file written with correct YAML
- [ ] 6.6 Add acceptance test: re-take produces additional task file

## 7. Build Verification

- [ ] 7.1 Run `dotnet build` and fix any errors
- [ ] 7.2 Run `dotnet test` and fix any failures
