## ADDED Requirements

### Requirement: User can list backlog views for a product
The system SHALL provide a `view list` CLI command that reads all view YAML files from the product's `BACKLOG/_views/` directory and outputs the ID, description, total item count, and archived item count for each view.

#### Scenario: List views in table format (default)
- **WHEN** the user runs `itr view list` inside a product directory with views defined
- **THEN** the system SHALL display a table with columns Id, Description, Items, Archived for each view

#### Scenario: List views in JSON format
- **WHEN** the user runs `itr view list --output json`
- **THEN** the system SHALL output a JSON array where each element contains `id`, `description`, `items`, and `archived` fields

#### Scenario: List views in text format
- **WHEN** the user runs `itr view list --output text`
- **THEN** the system SHALL output one line per view with tab-separated fields: id, description, items count, archived count

#### Scenario: No views defined
- **WHEN** the user runs `itr view list` and no view YAML files exist in `BACKLOG/_views/`
- **THEN** the system SHALL display a friendly message indicating no views are defined (not an empty table or error)

#### Scenario: View without description
- **WHEN** a view YAML file has no `description` field
- **THEN** the system SHALL display an empty string for that view's description column

#### Scenario: Specify product explicitly
- **WHEN** the user runs `itr view list --product <id>`
- **THEN** the system SHALL list views for the specified product regardless of the working directory

#### Scenario: Product not found
- **WHEN** the user runs `itr view list` outside a product directory and without `--product`
- **THEN** the system SHALL exit with a clear error message indicating product resolution failed

#### Scenario: Archived item count
- **WHEN** listing views, some items in a view are archived
- **THEN** the system SHALL display the count of archived items (items in `view.Items` that also appear in the archived backlog items list)
