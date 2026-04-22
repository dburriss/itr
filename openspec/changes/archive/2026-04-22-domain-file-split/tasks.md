## 1. Create shared infrastructure files

- [x] 1.1 Create `src/domain/Validation.fs` with shared slug regex helper (`isValidSlug`) in namespace `Itr.Domain`
- [x] 1.2 Create `src/domain/Types.fs` with shared primitive types: `ProfileName`, `RepoPath`, `ProductRoot`, `GitIdentity`, `CoordinationMode`, `CoordinationRoot`, `RepoId`, plus infrastructure interfaces: `IoError`, `IFileSystem`, `IEnvironment`, `IYamlService`, `IGitService`, `IAgentHarness`

## 2. Create per-concept domain files

- [x] 2.1 Create `src/domain/Portfolio.fs` with `PortfolioError`, `Profile`, `Portfolio`, `ResolvedProduct`, `ProfileName` module, `Portfolio` module, and `IPortfolioConfig` interface
- [x] 2.2 Create `src/domain/Product.fs` with `ProductId`, `CoordinationConfig`, `RepoConfig`, `ProductDefinition`, `AgentConfig`, `ProductRef`, `ProductConfig`, `ProductId` module, `RepoId` module (if not already in Types.fs), and `IProductConfig` interface
- [x] 2.3 Create `src/domain/Task.fs` with `TaskId`, `TaskState`, `ItrTask`, `TaskError` DU (containing `TaskNotFound`, `InvalidTaskState`, `MissingPlanArtifact`, `TaskIdConflict`, `TaskIdOverrideRequiresSingleRepo`), `TaskId` module, `ItrTask` module, and `ITaskStore` interface using `TaskError`
- [x] 2.4 Create `src/domain/Backlog.fs` with `BacklogId`, `BacklogItemType`, `BacklogItem`, `BacklogItemStatus`, `BacklogItemSummary`, `BacklogItemDetail`, `BacklogView`, `BacklogSnapshot`, `BacklogError` (without task-specific cases), `BacklogItemStatus` module, `BacklogItemType` module, `BacklogId` module, `BacklogItem` module, `IBacklogStore`, and `IViewStore`

## 3. Update project file

- [x] 3.1 Update `src/domain/Itr.Domain.fsproj` compile order to: `Effect.fs`, `Types.fs`, `Validation.fs`, `Portfolio.fs`, `Product.fs`, `Task.fs`, `Backlog.fs`
- [x] 3.2 Remove `Domain.fs` and `Interfaces.fs` entries from `Itr.Domain.fsproj`

## 4. Delete old files

- [x] 4.1 Delete `src/domain/Domain.fs`
- [x] 4.2 Delete `src/domain/Interfaces.fs`

## 5. Update adapter and usecases

- [x] 5.1 Update `YamlAdapter.fs` task-related methods to return `TaskError` instead of `BacklogError` (map errors accordingly)
- [x] 5.2 Update feature usecases that currently return `BacklogError` for task failures to return `TaskError`

## 6. Build and test

- [x] 6.1 Run `dotnet build` and fix all compile errors
- [x] 6.2 Run `dotnet test` and verify all tests pass with no behaviour changes
