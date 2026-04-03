## Why

The `profile-resolution` spec defines the required precedence chain (`--profile` → `ITR_PROFILE` → `defaultProfile`) and the `ProfileNotFound` error, but test coverage for edge-case branches (whitespace-only values, case-insensitive fallback paths, missing-profile errors via env/default) is absent. Closing this gap ensures the feature is verifiably correct and prevents silent regressions.

## What Changes

- Add unit tests to `tests/communication/PortfolioDomainTests.fs` covering all untested branches of `resolveActiveProfile`
- Audit every command branch in `src/cli/Program.fs` to confirm `--profile` is threaded correctly (no code changes expected)
- Add or verify an end-to-end acceptance test in `tests/acceptance/PortfolioAcceptanceTests.fs` exercising the full resolution path through real adapters
- If an implementation gap is found during audit, add a targeted fix to `src/features/Portfolio/PortfolioUsecase.fs` with a regression test

## Capabilities

### New Capabilities

<!-- None - this change adds tests and performs an audit; no new product capabilities -->

### Modified Capabilities

- `profile-resolution`: Filling test-coverage gaps for whitespace-fallthrough, case-insensitive lookup via env/default, and ProfileNotFound via env/default paths. No requirement changes — existing spec already captures these scenarios.

## Impact

- `tests/communication/PortfolioDomainTests.fs` — new tests added
- `tests/acceptance/PortfolioAcceptanceTests.fs` — new or verified acceptance test
- `src/features/Portfolio/PortfolioUsecase.fs` — possible targeted fix if audit reveals a gap
- `src/cli/Program.fs` — audit only, no changes expected
