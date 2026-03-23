## MODIFIED Requirements

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
