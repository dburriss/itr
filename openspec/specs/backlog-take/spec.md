### Requirement: Take a backlog item to create task files
When the user runs `itr backlog take <backlog-id>`, the system SHALL read the named backlog item from the coordination root and create one task YAML file per repo listed on the item. Each task SHALL start in the `planning` state.

#### Scenario: Single-repo item with no existing tasks uses backlog id as task id
- **WHEN** `itr backlog take feature-x` is run and the backlog item has one repo and no tasks exist yet
- **THEN** one task file is written at `<coordRoot>/BACKLOG/feature-x/tasks/feature-x/task.yaml` with `id: feature-x`

#### Scenario: Single-repo item re-taken uses repo-prefixed id
- **WHEN** `itr backlog take feature-x` is run and a task for that backlog id already exists
- **THEN** the new task file is written with id `<repo-id>-feature-x` (with numeric suffix if that also collides)

#### Scenario: Multi-repo item uses repo-prefixed ids
- **WHEN** `itr backlog take feature-x` is run and the backlog item lists two repos
- **THEN** two task files are written, each with id `<repo-id>-feature-x`, under `<coordRoot>/BACKLOG/feature-x/tasks/<repo-id>-feature-x/task.yaml`

### Requirement: Task state includes Planned and Approved
The `TaskState` type SHALL include `Planned` (plan written, not yet approved) and `Approved` (plan approved, ready to start) in addition to the existing states. The `planning` state in task YAML SHALL map to `Planning`; `planned` SHALL map to `Planned`; `approved` SHALL map to `Approved`.

#### Scenario: task.yaml with state planned deserialises to Planned
- **WHEN** a `task.yaml` file contains `state: planned`
- **THEN** the task's `State` field is `Planned`, not `Planning`

#### Scenario: task.yaml with state approved deserialises to Approved
- **WHEN** a `task.yaml` file contains `state: approved`
- **THEN** the task's `State` field is `Approved`

#### Scenario: Planned task serialises back to planned
- **WHEN** a task with `State = Planned` is serialised
- **THEN** the YAML contains `state: planned`

### Requirement: Task id override for single-repo items
The system SHALL accept an optional `--task-id <id>` flag when taking a single-repo backlog item to override the derived task id.

#### Scenario: Custom task id is used when provided
- **WHEN** `itr backlog take feature-x --task-id my-task` is run on a single-repo item
- **THEN** the task file is written with `id: my-task` at `<coordRoot>/BACKLOG/feature-x/tasks/my-task/task.yaml`

#### Scenario: Custom task id that already exists is rejected
- **WHEN** `--task-id` is provided but a task file with that id already exists
- **THEN** the command fails with a `TaskIdConflict` error and no files are written

#### Scenario: Custom task id on multi-repo item is rejected
- **WHEN** `--task-id` is provided but the backlog item lists more than one repo
- **THEN** the command fails with `TaskIdOverrideRequiresSingleRepo` and no files are written

### Requirement: Repo validation against product config
The system SHALL validate that every repo listed on the backlog item exists as a key in `product.yaml` before writing any task files.

#### Scenario: All repos present in product config succeeds
- **WHEN** all repos on the backlog item are present in `product.yaml`
- **THEN** task files are written successfully

#### Scenario: Unknown repo produces a clear error
- **WHEN** a repo id on the backlog item is not present in `product.yaml`
- **THEN** the command fails with `RepoNotInProduct <repo-id>` and no files are written

### Requirement: Human and JSON output modes
The system SHALL support both human-readable and JSON output for the `backlog take` command.

#### Scenario: Default human output lists created task ids and paths
- **WHEN** the command completes successfully without `--output json`
- **THEN** the console shows each created task id and its file path

#### Scenario: JSON output returns structured result
- **WHEN** the command completes successfully with `--output json`
- **THEN** stdout contains `{ "ok": true, "tasks": [ { "id": "...", "path": "..." } ] }`

### Requirement: Missing or invalid inputs produce clear errors
The system SHALL surface configuration and input errors with specific, actionable messages. Errors are represented by the `BacklogError` type (formerly `TakeError`).

#### Scenario: Missing product.yaml produces an error
- **WHEN** no `product.yaml` exists at the coordination root
- **THEN** the command fails with `ProductConfigNotFound`

#### Scenario: Missing backlog item produces an error
- **WHEN** no backlog item file exists for the given `<backlog-id>`
- **THEN** the command fails with `BacklogItemNotFound <backlog-id>`
