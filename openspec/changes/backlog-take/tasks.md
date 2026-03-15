## 1. Domain Types

- [x] 1.1 Add `BacklogId` slug-validated string wrapper to `Domain.fs` (same regex as `ProductId`)
- [x] 1.2 Add `TaskId` slug-validated string wrapper to `Domain.fs`
- [x] 1.3 Add `RepoId` type alias to `Domain.fs`
- [x] 1.4 Add `TaskState` DU (`Planning | InProgress | Implemented | Validated`) to `Domain.fs`
- [x] 1.5 Add `BacklogItem` record (`Id`, `Title`, `Repos`) to `Domain.fs`
- [x] 1.6 Add `ItrTask` record (`Id`, `SourceBacklog`, `Repo`, `State`, `CreatedAt`) to `Domain.fs`
- [x] 1.7 Add `ProductConfig` and `RepoConfig` records to `Domain.fs`
- [x] 1.8 Add `TakeError` DU (`ProductConfigNotFound`, `ProductConfigParseError`, `BacklogItemNotFound`, `RepoNotInProduct`, `TaskIdConflict`, `TaskIdOverrideRequiresSingleRepo`) to `Domain.fs`

## 2. Interfaces

- [x] 2.1 Update `IYamlService` in `Interfaces.fs` to use typed `Result<'a, string>` signature for `Parse` (already done per current code — verify and fill in `ParseError` type if needed)
- [x] 2.2 Add `IProductConfig` interface to `Interfaces.fs` (`LoadProductConfig : string -> Result<ProductConfig, TakeError>`)
- [x] 2.3 Add `IBacklogStore` interface to `Interfaces.fs` (`LoadBacklogItem : string -> BacklogId -> Result<BacklogItem, TakeError>`)
- [x] 2.4 Add `ITaskStore` interface to `Interfaces.fs` (`ListTasks : string -> BacklogId -> Result<ItrTask list, TakeError>` and `WriteTask : string -> ItrTask -> Result<unit, TakeError>`)

## 3. YAML Adapter

- [x] 3.1 Add `YamlDotNet` package reference to `Itr.Adapters.fsproj`
- [x] 3.2 Create `src/adapters/YamlAdapter.fs` with CLIMutable DTOs for `BacklogItem`, `ProductConfig`, and `ItrTask` using `[<YamlMember(Alias="...")>]` for snake_case field mapping
- [x] 3.3 Implement `IYamlService` in `YamlAdapter.fs` using `YamlDotNet`
- [x] 3.4 Implement `IProductConfig` in `YamlAdapter.fs` (reads `<coordRoot>/product.yaml`)
- [x] 3.5 Implement `IBacklogStore` in `YamlAdapter.fs` (reads `<coordRoot>/BACKLOG/items/<backlog-id>.yaml`)
- [x] 3.6 Implement `ITaskStore` in `YamlAdapter.fs` (reads/writes `<coordRoot>/TASKS/<backlog-id>/<task-id>-task.yaml`)
- [x] 3.7 Add `YamlAdapter.fs` to `Itr.Adapters.fsproj` compile order

## 4. Use Case

- [x] 4.1 Create `src/features/Task/TaskUsecase.fs`
- [x] 4.2 Implement `takeBacklogItem` pure function that takes adapters + inputs and returns `Result<ItrTask list, TakeError>` (no I/O — returns tasks for caller to write)
- [x] 4.3 Implement `TaskId` derivation logic: single-repo/no-existing → backlog id; single-repo/re-take or multi-repo → `<repo-id>-<backlog-id>`; numeric suffix on further collision
- [x] 4.4 Implement `--task-id` override path with `TaskIdConflict` and `TaskIdOverrideRequiresSingleRepo` validation
- [x] 4.5 Add `TaskUsecase.fs` to `Itr.Features.fsproj` compile order

## 5. CLI Command

- [x] 5.1 Add `TakeArgs` Argu DU with `BacklogId` positional and `TaskId` optional flag to `Program.fs`
- [x] 5.2 Add `BacklogArgs` Argu DU with `Take` subcommand to `Program.fs`
- [x] 5.3 Add `Backlog` case to the top-level `CliArgs` DU in `Program.fs`
- [x] 5.4 Wire `AppDeps` with `IProductConfig`, `IBacklogStore`, and `ITaskStore` implementations from `YamlAdapter`
- [x] 5.5 Implement dispatch: `loadPortfolio → resolveActiveProfile → resolveProduct → takeBacklogItem → write tasks`
- [x] 5.6 Implement human output: print each created task id and file path
- [x] 5.7 Implement JSON output for `--output json`: `{ "ok": true, "tasks": [ { "id": "...", "path": "..." } ] }`

## 6. Tests

- [x] 6.1 Add communication tests for `TaskId` derivation: single-repo/no-existing, single-repo/re-take, multi-repo
- [x] 6.2 Add communication tests for `--task-id` override: happy path and `TaskIdConflict`
- [x] 6.3 Add communication test for `TaskIdOverrideRequiresSingleRepo`
- [x] 6.4 Add communication test for `RepoNotInProduct` validation
- [x] 6.5 Add acceptance test with temp dir fixture: take a single-repo item, assert task file written with correct YAML
- [x] 6.6 Add acceptance test: re-take produces additional task file

## 7. Build Verification

- [x] 7.1 Run `dotnet build` and fix any errors
- [x] 7.2 Run `dotnet test` and fix any failures
