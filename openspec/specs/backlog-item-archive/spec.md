### Requirement: Archive a completed backlog item atomically
When all tasks under a backlog item are completed (i.e. all task folders under `BACKLOG/<backlog-id>/tasks/` carry a date prefix), the system SHALL move the entire `BACKLOG/<backlog-id>/` directory to `BACKLOG/archive/<date>-<backlog-id>/` where `<date>` is today's date in `YYYY-MM-DD` format.

#### Scenario: All tasks completed triggers successful archive
- **WHEN** `itr task archive <backlog-id>` is run and every folder under `BACKLOG/<backlog-id>/tasks/` has a date prefix
- **THEN** the entire `BACKLOG/<backlog-id>/` folder is moved to `BACKLOG/archive/<date>-<backlog-id>/` and no longer exists at the original path

#### Scenario: Active task prevents archive
- **WHEN** `itr task archive <backlog-id>` is run and at least one folder under `BACKLOG/<backlog-id>/tasks/` has no date prefix
- **THEN** the command fails with `ActiveTasksRemaining <backlog-id>` and no files are moved

#### Scenario: Missing backlog item produces an error
- **WHEN** `itr task archive <backlog-id>` is run and `BACKLOG/<backlog-id>/item.yaml` does not exist
- **THEN** the command fails with `BacklogItemNotFound <backlog-id>`

#### Scenario: Archive is idempotent when already archived
- **WHEN** `itr task archive <backlog-id>` is run and the item is already under `BACKLOG/archive/`
- **THEN** the command fails with `BacklogItemNotFound <backlog-id>` (original path is gone)
