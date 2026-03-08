## ADDED Requirements

### Requirement: Profile selection precedence
The system SHALL select the active profile using the following precedence (highest to lowest): `--profile` CLI flag, `ITR_PROFILE` environment variable, `defaultProfile` field in the portfolio config. If none of these resolves to a profile name, a `ProfileNotFound` error SHALL be returned.

#### Scenario: CLI flag takes precedence
- **WHEN** `--profile work` is passed and `ITR_PROFILE=personal` is set
- **THEN** the `work` profile is selected

#### Scenario: Env var used when no flag
- **WHEN** `--profile` is not passed and `ITR_PROFILE=personal` is set
- **THEN** the `personal` profile is selected

#### Scenario: Default profile used as fallback
- **WHEN** neither `--profile` nor `ITR_PROFILE` is set and `defaultProfile` is `"work"` in the config
- **THEN** the `work` profile is selected

#### Scenario: No profile resolvable
- **WHEN** `--profile` is absent, `ITR_PROFILE` is unset, and `defaultProfile` is absent
- **THEN** a `ProfileNotFound` error is returned

### Requirement: Profile lookup is case-insensitive
The system SHALL look up profiles case-insensitively. Profile names SHALL be stored as given but matched by lowercased comparison.

#### Scenario: Case-insensitive flag match
- **WHEN** `--profile WORK` is passed and the portfolio contains a profile named `"work"`
- **THEN** the `work` profile is returned

### Requirement: Profile not found error
The system SHALL return a `ProfileNotFound` error when the resolved profile name does not match any profile in the portfolio.

#### Scenario: Unknown profile name
- **WHEN** `--profile staging` is passed but no `staging` profile exists
- **THEN** a `ProfileNotFound` error is returned containing the profile name

### Requirement: readEnv is injected for testability
The `resolveActiveProfile` use-case SHALL accept a `readEnv: string -> string option` function parameter for reading environment variables, so it can be called without real process environment in tests.

#### Scenario: Injected env reader used
- **WHEN** `resolveActiveProfile` is called with a stub `readEnv` returning `Some "personal"` for `"ITR_PROFILE"`
- **THEN** the `personal` profile is selected without accessing the real process environment
