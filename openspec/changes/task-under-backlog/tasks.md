## 1. Migrate Existing Files on Disk

- [x] 1.1 Create `itr/BACKLOG/archive/2026-03-13-portfolio-layer/tasks/2026-03-13-portfolio-layer/` and move `BACKLOG/items/portfolio-layer.yaml` → `item.yaml`; move task and plan files from `TASKS/archive/portfolio-layer/` → renamed to `task.yaml` / `plan.md`
- [x] 1.2 Create `itr/BACKLOG/archive/2026-03-20-backlog-take/tasks/2026-03-20-backlog-take/` and move `BACKLOG/items/backlog-take.yaml` → `item.yaml`; move task and plan files from `TASKS/archive/backlog-take/` → renamed to `task.yaml` / `plan.md`
- [x] 1.3 Create `itr/BACKLOG/archive/2026-03-20-settings-bootstrap/tasks/2026-03-20-settings-bootstrap/` and move `BACKLOG/items/settings-bootstrap.yaml` → `item.yaml`; move task and plan files from `TASKS/archive/settings-bootstrap/` → renamed to `task.yaml` / `plan.md`
- [x] 1.4 Move all 19 remaining `itr/BACKLOG/items/<id>.yaml` files to `itr/BACKLOG/<id>/item.yaml`
- [x] 1.5 Move any active tasks under `itr/TASKS/<id>/` to `itr/BACKLOG/<id>/tasks/<task-id>/task.yaml` and `plan.md`
- [x] 1.6 Verify all new paths exist, then delete `itr/BACKLOG/items/` and `itr/TASKS/`

## 2. Update Domain Interfaces

- [x] 2.1 Update `IBacklogStore` doc comment in `src/domain/Interfaces.fs` to reflect new path `BACKLOG/<id>/item.yaml`
- [x] 2.2 Update `ITaskStore` doc comments in `src/domain/Interfaces.fs` to reflect new paths
- [x] 2.3 Add `ArchiveBacklogItem: coordRoot: string -> backlogId: BacklogId -> date: string -> Result<unit, TakeError>` to `IBacklogStore` in `src/domain/Interfaces.fs`

## 3. Update YamlAdapter

- [x] 3.1 Update `BacklogStoreAdapter.LoadBacklogItem` path: `BACKLOG/items/<id>.yaml` → `BACKLOG/<id>/item.yaml`
- [x] 3.2 Update `TaskStoreAdapter.ListTasks` to enumerate subdirectories of `BACKLOG/<backlog-id>/tasks/` and read `task.yaml` from each (both dated and undated folders)
- [x] 3.3 Update `TaskStoreAdapter.WriteTask` path: `TASKS/<id>/<task-id>-task.yaml` → `BACKLOG/<id>/tasks/<task-id>/task.yaml`; use `Directory.CreateDirectory` on full task subfolder path before writing
- [x] 3.4 Update archive task path in `YamlAdapter.fs`: rename `tasks/<task-id>/` → `tasks/<date>-<task-id>/` in place (was `TASKS/archive/<backlog-id>/<task-id>-*`)
- [x] 3.5 Implement `BacklogStoreAdapter.ArchiveBacklogItem`: move `BACKLOG/<backlog-id>/` → `BACKLOG/archive/<date>-<backlog-id>/`
- [x] 3.6 Wire `ArchiveBacklogItem` in `src/cli/Program.fs` composite adapter

## 4. Update Tests

- [x] 4.1 Update acceptance test fixture in `tests/acceptance/TaskAcceptanceTests.fs`: change `BACKLOG/items` setup to `BACKLOG/<id>/item.yaml` layout
- [x] 4.2 Update expected task paths in acceptance tests from `TASKS/<id>/<task-id>-task.yaml` to `BACKLOG/<id>/tasks/<task-id>/task.yaml`
- [x] 4.3 Add unit tests for `ListTasks` with both active (`<task-id>/`) and completed (`<date>-<task-id>/`) subdirectory formats
- [x] 4.4 Add unit tests for `WriteTask` creating intermediate `tasks/<task-id>/` directory
- [x] 4.5 Add unit test for `ArchiveBacklogItem`: verifies folder moves to `archive/<date>-<id>/`
- [x] 4.6 Add unit test for `ArchiveBacklogItem` blocked when active (undated) task folder exists
- [x] 4.7 Run `dotnet test` and fix any failures

## 5. Update Slash Command and Docs

- [x] 5.1 Update `.opencode/command/plan.md`: task file path reference `TASKS/<backlog-id>/<task-id>-task.yaml` → `BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`; backlog item path `BACKLOG/items/<id>.yaml` → `BACKLOG/<id>/item.yaml`; plan output path `TASKS/<backlog-id>/` → `BACKLOG/<backlog-id>/tasks/<task-id>/`
- [x] 5.2 Update `docs/config-files.md`: directory layout diagram and path examples for new structure
- [x] 5.3 Update `docs/lifecycles.md`: replace `TASKS/archive/` references with `BACKLOG/<id>/tasks/<date>-<task-id>/` convention

## 6. Build and Verify

- [x] 6.1 Run `dotnet build` and fix any compilation errors
- [x] 6.2 Run `dotnet test` and confirm all tests pass
