### Requirement: Create a backlog item from the CLI
The system SHALL create a new backlog item YAML file at `<coordRoot>/BACKLOG/<id>/item.yaml` when the user runs `itr backlog add <id> --title <title>` OR when `itr backlog add --interactive` completes successfully with all required fields provided via prompts.

`Backlog_Id` and `Title` are no longer `Mandatory` at Argu parse time; the `handleBacklogAdd` handler SHALL validate their presence and produce a clear error when `--interactive` is not set and either field is missing.

#### Scenario: Minimal valid invocation writes item.yaml
- **WHEN** `itr backlog add my-feature --title "My Feature" --repo my-repo` is run with a valid id, title, and repo present in `product.yaml`
- **THEN** `<coordRoot>/BACKLOG/my-feature/item.yaml` is created containing at least `id`, `title`, `repos`, `type`, and `created_at`

#### Scenario: Duplicate id is rejected
- **WHEN** `itr backlog add my-feature --title "..."` is run and `<coordRoot>/BACKLOG/my-feature/item.yaml` already exists
- **THEN** the command fails with `DuplicateBacklogId my-feature` and no file is overwritten

#### Scenario: Unknown repo is rejected
- **WHEN** `--repo unknown-repo` is specified and `unknown-repo` is not present in `product.yaml`
- **THEN** the command fails with `RepoNotInProduct unknown-repo`

#### Scenario: Invalid type is rejected
- **WHEN** `--type invalid-type` is specified
- **THEN** the command fails with `InvalidItemType invalid-type`

#### Scenario: Refactor type is accepted
- **WHEN** `--type refactor` is specified
- **THEN** the item is created with `type: refactor` in `item.yaml`

#### Scenario: Type defaults to feature when omitted
- **WHEN** `--type` is not provided
- **THEN** the created `item.yaml` contains `type: feature`

#### Scenario: Single-repo product auto-resolves repo
- **WHEN** the product has exactly one repo and `--repo` is omitted
- **THEN** the item is created using that sole repo

#### Scenario: Multi-repo product requires explicit repo
- **WHEN** the product has more than one repo and `--repo` is omitted and `--interactive` is not set
- **THEN** the command fails with a clear error indicating that `--repo` is required

#### Scenario: created_at is set to today
- **WHEN** a backlog item is successfully created
- **THEN** `item.yaml` contains `created_at` set to today's date in `yyyy-MM-dd` format

#### Scenario: JSON output returns structured result
- **WHEN** the command completes successfully with `--output json`
- **THEN** stdout contains a JSON object with `ok: true` and the created item id

#### Scenario: Missing id without interactive flag produces error
- **WHEN** `itr backlog add --title "My Feature"` is run without `--interactive` and without a backlog id
- **THEN** the command fails with a clear error indicating that `--backlog-id` is required

#### Scenario: Missing title without interactive flag produces error
- **WHEN** `itr backlog add my-feature` is run without `--interactive` and without `--title`
- **THEN** the command fails with a clear error indicating that `--title` is required
