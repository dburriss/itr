## MODIFIED Requirements

### Requirement: Invalid type is rejected
The system SHALL reject the `--item-type` flag when its value is not one of `feature`, `bug`, `chore`, `refactor`, or `spike`. When rejected, the command SHALL fail with `InvalidItemType <value>` and list all valid types including `refactor` in the error message.

#### Scenario: Invalid type is rejected
- **WHEN** `--item-type invalid-type` is specified
- **THEN** the command fails with `InvalidItemType invalid-type`

#### Scenario: Refactor type is accepted
- **WHEN** `--item-type refactor` is specified
- **THEN** the item is created with `type: refactor` in `item.yaml`
