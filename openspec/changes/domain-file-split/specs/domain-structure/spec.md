## ADDED Requirements

### Requirement: Domain types are organised into per-concept files
The domain layer SHALL organise types and interfaces into separate files per concept: `Types.fs`, `Validation.fs`, `Portfolio.fs`, `Product.fs`, `Task.fs`, and `Backlog.fs`. The monolithic `Domain.fs` and `Interfaces.fs` SHALL NOT exist after the refactor.

#### Scenario: Build succeeds after split
- **WHEN** `Domain.fs` and `Interfaces.fs` are deleted and their content distributed across the per-concept files
- **THEN** `dotnet build` SHALL complete with no errors

#### Scenario: All tests pass after split
- **WHEN** the per-concept file split is complete
- **THEN** `dotnet test` SHALL pass with no behaviour changes

### Requirement: TaskError is a distinct type from BacklogError
The domain SHALL define `TaskError` in `Task.fs` as a separate discriminated union from `BacklogError`. `ITaskStore` methods SHALL return `Result<_, TaskError>` rather than `Result<_, BacklogError>`.

#### Scenario: ITaskStore uses TaskError
- **WHEN** a caller invokes `ITaskStore.ListTasks`, `ITaskStore.WriteTask`, `ITaskStore.ArchiveTask`, or `ITaskStore.ListAllTasks`
- **THEN** the return type SHALL be `Result<_, TaskError>`

#### Scenario: BacklogError does not contain task-specific cases
- **WHEN** `BacklogError` is defined in `Backlog.fs`
- **THEN** it SHALL NOT contain `TaskNotFound`, `InvalidTaskState`, `MissingPlanArtifact`, `TaskIdConflict`, or `TaskIdOverrideRequiresSingleRepo`

### Requirement: Interfaces are co-located with their domain concept
Each capability interface SHALL reside in the same file as its primary domain type rather than in a shared `Interfaces.fs`.

#### Scenario: IPortfolioConfig lives in Portfolio.fs
- **WHEN** the refactor is complete
- **THEN** `IPortfolioConfig` SHALL be defined in `Portfolio.fs`

#### Scenario: IProductConfig lives in Product.fs
- **WHEN** the refactor is complete
- **THEN** `IProductConfig` SHALL be defined in `Product.fs`

#### Scenario: ITaskStore lives in Task.fs
- **WHEN** the refactor is complete
- **THEN** `ITaskStore` SHALL be defined in `Task.fs`

#### Scenario: IBacklogStore and IViewStore live in Backlog.fs
- **WHEN** the refactor is complete
- **THEN** `IBacklogStore` and `IViewStore` SHALL be defined in `Backlog.fs`

#### Scenario: Infrastructure interfaces live in Types.fs
- **WHEN** the refactor is complete
- **THEN** `IFileSystem`, `IEnvironment`, `IYamlService`, `IGitService`, and `IAgentHarness` SHALL be defined in `Types.fs`
