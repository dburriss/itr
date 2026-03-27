## 1. Domain Layer

- [ ] 1.1 Add `InvalidTaskState of taskId: TaskId * current: TaskState` case to `BacklogError` in `src/domain/Domain.fs`
- [ ] 1.2 Update exhaustive `formatBacklogError` match in `src/cli/Program.fs` to handle `InvalidTaskState`

## 2. Use-Case Logic

- [ ] 2.1 Add `planTask` function to `src/features/Task/TaskUsecase.fs` returning `Result<ItrTask * bool, BacklogError>` — allows Planning and Planned states, errors on states beyond Planned

## 3. Interfaces and Adapters

- [ ] 3.1 Add `IAgentHarness` interface to `src/domain/Interfaces.fs` with `Prompt: prompt: string -> debug: bool -> Result<string, string>`
- [ ] 3.2 Create `src/adapters/OpenCodeAdapter.fs` implementing `IAgentHarness` with health check, session creation, and message POST against `http://127.0.0.1:4096`
- [ ] 3.3 Register `OpenCodeAdapter.fs` in `src/adapters/Itr.Adapters.fsproj`

## 4. File Assets

- [ ] 4.1 Create `src/cli/assets/plan-template.md` with Fue `{{{triple-brace}}}` placeholders for title, taskId, backlogId, repo, summary, dependencies, acceptanceCriteria
- [ ] 4.2 Create `src/cli/assets/plan-prompt.md` as the AI planning prompt with `{{{taskId}}}` placeholder
- [ ] 4.3 Add `Fue` NuGet package reference to `src/cli/Itr.Cli.fsproj`
- [ ] 4.4 Declare both asset files as `<Content CopyToOutputDirectory="PreserveNewest" />` in `src/cli/Itr.Cli.fsproj`

## 5. CLI Handler

- [ ] 5.1 Add `TaskPlanArgs` DU with `Task_Id`, `Ai`, and `Debug` cases to `src/cli/Program.fs`
- [ ] 5.2 Add `Plan of ParseResults<TaskPlanArgs>` case to `TaskArgs` DU
- [ ] 5.3 Implement `handleTaskPlan` handler: load task and backlog item, call `planTask`, render template via Fue, optionally call harness, write plan and updated task
- [ ] 5.4 Wire `handleTaskPlan` into `dispatch` for `TaskArgs.Plan`
- [ ] 5.5 Instantiate `OpenCodeHarnessAdapter` in `AppDeps` and wire as `IAgentHarness`

## 6. Acceptance Tests

- [ ] 6.1 Create `tests/acceptance/TaskPlanAcceptanceTests.fs` with tests: happy path (stub plan), re-plan, task not found, invalid state, AI happy path (stub harness), AI harness error
- [ ] 6.2 Register `TaskPlanAcceptanceTests.fs` in `tests/acceptance/Itr.Tests.Acceptance.fsproj`

## 7. Verification

- [ ] 7.1 Run `dotnet build` — no errors or warnings
- [ ] 7.2 Run `dotnet test` — all tests pass including new acceptance tests
