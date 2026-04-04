## ADDED Requirements

### Requirement: Whitespace-only flag falls through to lower-priority sources
When the `--profile` flag is provided but contains only whitespace, the system SHALL treat it as absent and fall through to the next source in the precedence chain (`ITR_PROFILE`, then `defaultProfile`).

#### Scenario: Whitespace flag falls through to env var
- **WHEN** `--profile` is `"   "` (whitespace only) and `ITR_PROFILE=personal` is set
- **THEN** the `personal` profile is selected

#### Scenario: Whitespace flag falls through to default profile
- **WHEN** `--profile` is `"   "` (whitespace only), `ITR_PROFILE` is unset, and `defaultProfile` is `"work"`
- **THEN** the `work` profile is selected

### Requirement: Whitespace-only ITR_PROFILE falls through to defaultProfile
When `ITR_PROFILE` is set but contains only whitespace, the system SHALL treat it as absent and fall through to `defaultProfile`.

#### Scenario: Whitespace env var falls through to default profile
- **WHEN** `--profile` is absent, `ITR_PROFILE` is `"   "` (whitespace only), and `defaultProfile` is `"work"`
- **THEN** the `work` profile is selected

### Requirement: ProfileNotFound returned when env var names a non-existent profile
The system SHALL return a `ProfileNotFound` error containing the attempted name when `ITR_PROFILE` resolves to a name that does not match any profile.

#### Scenario: Env var names a non-existent profile
- **WHEN** `--profile` is absent and `ITR_PROFILE=xyz` is set but no `xyz` profile exists
- **THEN** a `ProfileNotFound "xyz"` error is returned

### Requirement: ProfileNotFound returned when defaultProfile names a non-existent profile
The system SHALL return a `ProfileNotFound` error containing the attempted name when `defaultProfile` resolves to a name that does not match any profile.

#### Scenario: Default profile names a non-existent profile
- **WHEN** `--profile` is absent, `ITR_PROFILE` is unset, and `defaultProfile` is `"xyz"` but no `xyz` profile exists
- **THEN** a `ProfileNotFound "xyz"` error is returned

### Requirement: Case-insensitive lookup via ITR_PROFILE
The system SHALL resolve profiles case-insensitively when the name comes from `ITR_PROFILE`.

#### Scenario: Uppercase env var matches lowercase profile
- **WHEN** `--profile` is absent, `ITR_PROFILE=WORK` is set, and the portfolio contains a profile named `"work"`
- **THEN** the `work` profile is returned

### Requirement: Case-insensitive lookup via defaultProfile
The system SHALL resolve profiles case-insensitively when the name comes from `defaultProfile`.

#### Scenario: Mixed-case defaultProfile matches stored profile
- **WHEN** `--profile` is absent, `ITR_PROFILE` is unset, `defaultProfile` is `"Work"`, and the portfolio contains a profile named `"work"`
- **THEN** the `work` profile is returned
