### Requirement: Create a backlog item from the CLI
The system SHALL create a new backlog item YAML file at `<coordRoot>/BACKLOG/<id>/item.yaml` when the user runs `itr backlog add <id> --title <title>`.

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

#### Scenario: Type defaults to feature when omitted
- **WHEN** `--type` is not provided
- **THEN** the created `item.yaml` contains `type: feature`

#### Scenario: Single-repo product auto-resolves repo
- **WHEN** the product has exactly one repo and `--repo` is omitted
- **THEN** the item is created using that sole repo

#### Scenario: Multi-repo product requires explicit repo
- **WHEN** the product has more than one repo and `--repo` is omitted
- **THEN** the command fails with a clear error indicating that `--repo` is required

#### Scenario: created_at is set to today
- **WHEN** a backlog item is successfully created
- **THEN** `item.yaml` contains `created_at` set to today's date in `yyyy-MM-dd` format

#### Scenario: JSON output returns structured result
- **WHEN** the command completes successfully with `--output json`
- **THEN** stdout contains a JSON object with `ok: true` and the created item id
