# Split Domain.fs and Interfaces.fs into per-concept files

**Task ID:** domain-file-split
**Backlog Item:** domain-file-split
**Repo:** itr

## Description

Domain.fs is a 343-line flat file and Interfaces.fs defines capability interfaces for all domain concepts in one place. Split both into per-concept files (Types, Validation, Portfolio, Product, Task, Backlog) aligned with the four categories in docs/terminology.md, co-locating each interface with its domain file. No logic changes — pure structural reorganisation that improves navigability, maintainability, and creates natural homes for state machine, validation, and capability boundary definitions.

## Scope

**Included:**
- Split Domain.fs into per-concept files: Types.fs, Product.fs, Task.fs, Backlog.fs
- Split Interfaces.fs by co-locating interfaces with their domain concept (IFileSystem/IEnvironment/IYamlService/IGitService/IAgentHarness/IoError in Types.fs; IPortfolioConfig in Portfolio.fs; IProductConfig in Product.fs; IBacklogStore/IViewStore in Backlog.fs; ITaskStore in Task.fs)
- Create Validation.fs as a generic validation utility module (shared helpers only); domain-specific validation modules live in their respective domain files
- Extract TaskError from BacklogError per acceptance criteria
- Update compile order in Itr.Domain.fsproj
- Delete Domain.fs and Interfaces.fs after split

**Excluded:**
- No logic changes to usecases or adapters
- No changes to test code beyond updating module namespaces if needed
- No changes to existing error handling patterns beyond extracting TaskError

## Steps

1. Create Validation.fs as a generic validation utility module containing only shared helpers; domain-specific validation stays in each domain file
2. Create Product.fs with ProductDefinition, ProductRef, RepoConfig, CoordRoot, AgentConfig, CoordinationConfig, ProductId, ProductRef, RepoId modules from Domain.fs; include ProductId/RepoId validation modules; add IProductConfig interface
3. Create Task.fs with TaskId, TaskState, ItrTask types; include TaskId validation module; define TaskError with TaskNotFound, InvalidTaskState, MissingPlanArtifact, TaskIdConflict, TaskIdOverrideRequiresSingleRepo (extracted from BacklogError); add ITaskStore interface using TaskError
4. Create Backlog.fs with BacklogId, BacklogItemType, BacklogItem, BacklogItemStatus, BacklogItemSummary, BacklogItemDetail, BacklogView, BacklogSnapshot, BacklogError types; include BacklogId/BacklogItemType/BacklogItem validation modules; add IBacklogStore, IViewStore interfaces
5. Update Itr.Domain.fsproj compile order: Effect.fs, Types.fs, Validation.fs, Portfolio.fs, Product.fs, Task.fs, Backlog.fs
6. Build and fix any compile errors
7. Run all tests to verify no behavior changes

## Dependencies

- none

## Acceptance Criteria

- Domain.fs is deleted and its content distributed across Effect.fs, Types.fs, Validation.fs, Portfolio.fs, Product.fs, Task.fs, and Backlog.fs
- Domain-specific validation modules live in their respective domain files (e.g. ProductId validation in Product.fs, BacklogId validation in Backlog.fs); Validation.fs contains only shared generic helpers
- Interfaces.fs is deleted and its interfaces co-located with their domain files: IFileSystem/IEnvironment/IYamlService/IGitService/IAgentHarness/IoError in Types.fs; IPortfolioConfig/IProductConfig in Portfolio.fs; ITaskStore in Task.fs; IBacklogStore/IViewStore in Backlog.fs
- Compile order in Itr.Domain.fsproj reflects the new file dependency graph with no Domain.fs or Interfaces.fs entries
- TaskError lives in Task.fs (separate from BacklogError which stays in Backlog.fs)
- ITaskStore methods use TaskError instead of BacklogError
- Feature usecases that return BacklogError for task-related failures are updated to TaskError
- All existing tests pass with no behaviour changes

## Impact

**Files changed:**
- src/domain/Domain.fs → deleted, content split into Types.fs, Product.fs, Task.fs, Backlog.fs
- src/domain/Interfaces.fs → deleted, interfaces co-located with domain files
- src/domain/Itr.Domain.fsproj → updated compile order

- **New files created:**
- src/domain/Validation.fs (generic shared helpers only)
- src/domain/Product.fs
- src/domain/Task.fs
- src/domain/Backlog.fs

**Interfaces affected:**
- ITaskStore changes return type from BacklogError to TaskError
- All implementations (YamlAdapter) need updates to handle TaskError instead of BacklogError for task operations

**Test impact:**
- Tests may need updated module/namespace references if they explicitly open Itr.Domain

## Risks

- **Risk:** ITaskStore interface changes require updates in YamlAdapter.fs implementation
  - **Mitigation:** Update YamlAdapter to use TaskError for task operations, keep BacklogError for backlog operations

- **Risk:** Circular dependencies if file order is wrong
  - **Mitigation:** Verify compile order matches dependency graph (Effect → Types → Validation → Portfolio/Product/Task/Backlog)

- **Risk:** Tests depend on module paths that change
  - **Mitigation:** Run tests after split, fix any namespace/reference issues

## Decisions

- Validation modules live in their respective domain files (ProductId in Product.fs, BacklogId in Backlog.fs, etc.), using shared helpers from Validation.fs if needed. Validation.fs is a generic utility only.
- TaskError lives in Task.fs, separate from BacklogError which stays in Backlog.fs.