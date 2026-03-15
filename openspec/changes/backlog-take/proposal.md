## Why

The `itr` tool needs a way to translate a product-level backlog item into per-repo task files, enabling developers to track and manage work at the repository level. Without this command, backlog items remain abstract planning artifacts that aren't connected to actual development workflows.

## What Changes

- Add `itr backlog take <backlog-id>` CLI command
- Introduce `BacklogId`, `TaskId`, `RepoId`, `TaskState`, `BacklogItem`, `ItrTask`, `ProductConfig`, and `RepoConfig` domain types
- Fill in `IYamlService` interface placeholder with concrete `Parse`/`Serialize` signatures
- Add `IProductConfig`, `IBacklogStore`, and `ITaskStore` interfaces
- Implement `YamlAdapter` using `YamlDotNet` for all YAML-backed interfaces
- Add `takeBacklogItem` use-case that derives task IDs and returns `ItrTask` values
- Write task YAML files at `<coordRoot>/TASKS/<backlog-id>/<task-id>-task.yaml`

## Capabilities

### New Capabilities

- `backlog-take`: Command that reads a backlog item and creates one planning task file per repo listed on the item

### Modified Capabilities

<!-- No existing spec-level requirements are changing -->

## Impact

- `Itr.Domain/Domain.fs` — additive new types
- `Itr.Domain/Interfaces.fs` — additive new interfaces; `IYamlService` placeholder filled in
- `Itr.Features/Task/TaskUsecase.fs` — new file with `takeBacklogItem` pure pipeline
- `Itr.Adapters/YamlAdapter.fs` — new file implementing YAML-backed stores; adds `YamlDotNet` package reference
- `Itr.Cli/Program.fs` — adds `Backlog`/`Take` subcommand to Argu DU and dispatch
- `AppDeps` composition root — wires `YamlAdapter` for new interfaces
