# Plan: Profile Selection

## Description

Standardize active profile resolution so that all commands resolve profiles consistently via a defined precedence chain: `--profile` flag → `ITR_PROFILE` env var → `defaultProfile` from config. Return a clear, typed error when no profile can be resolved.

The core logic (`resolveActiveProfile`) already exists in `src/features/Portfolio/PortfolioUsecase.fs:29–49`. This task is primarily about verifying correctness, filling test-coverage gaps, and confirming the end-to-end wiring in the CLI entry point.

---

## Scope

### 1. Verify implementation correctness

Review `resolveActiveProfile` at `src/features/Portfolio/PortfolioUsecase.fs:29–49` against all three acceptance criteria:

- `--profile` flag → `ITR_PROFILE` → `defaultProfile` precedence
- Missing resolved profile returns `ProfileNotFound` with the attempted name (or `"<none>"`)
- Case-insensitive lookup via `tryFindProfileCaseInsensitive` in `Domain.fs:131`

No changes are expected here; the logic appears correct. Confirm and document.

### 2. Fill test-coverage gaps

Add tests to `tests/communication/PortfolioDomainTests.fs` covering uncovered branches in `resolveActiveProfile`:

| Scenario | Expected result |
|---|---|
| Whitespace-only flag falls through to env var | Env var profile resolved |
| Whitespace-only flag falls through to default | Default profile resolved |
| Whitespace-only `ITR_PROFILE` falls through to default | Default profile resolved |
| Flag names a non-existent profile | `ProfileNotFound "xyz"` |
| Env var names a non-existent profile | `ProfileNotFound "xyz"` |
| Case-insensitive lookup via `ITR_PROFILE=WORK` | Profile `"work"` resolved |
| Case-insensitive lookup via `defaultProfile` | Profile resolved |

Verify existing coverage remains green.

### 3. Verify end-to-end CLI wiring

Confirm that `--profile` (`-p`) is extracted and passed to `resolveActiveProfile` in every command branch in `src/cli/Program.fs:1401–1786`. Verify `IEnvironment` is satisfied in `AppDeps` (it is — `EnvironmentAdapter` at `Program.fs:265`).

No CLI changes expected; audit only.

### 4. Add or verify acceptance test

Confirm `tests/acceptance/PortfolioAcceptanceTests.fs` has at least one test that exercises the full resolution path (flag → env → default) end-to-end through the adapters, not just domain logic. Add one if missing.

---

## Dependencies / Prerequisites

- `settings-bootstrap` is archived (complete). `bootstrapIfMissing` and `loadPortfolio` are in place. No blocking dependencies.

---

## Impact on Existing Code

- No domain or feature changes anticipated.
- New tests only, plus possible minor additions to acceptance tests.
- If a gap or bug is found during audit, a targeted fix goes in `PortfolioUsecase.fs` with a corresponding regression test.

---

## Acceptance Criteria

1. `resolveActiveProfile` applies precedence: `--profile` → `ITR_PROFILE` → `defaultProfile`.
2. A missing resolved profile returns `ProfileNotFound` with the name attempted (or `"<none>"`).
3. All the test scenarios in the coverage table above have passing tests.
4. `dotnet test` passes with no regressions.

---

## Testing Strategy

- **Communication tests** (`tests/communication/PortfolioDomainTests.fs`): unit-level, pure domain logic, covering all branches of `resolveActiveProfile`.
- **Acceptance tests** (`tests/acceptance/PortfolioAcceptanceTests.fs`): at least one end-to-end scenario exercising resolution through `IEnvironment` and the real adapter.
- Run `dotnet test` before and after changes.

---

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| The whitespace-guard branches in `resolveActiveProfile` are untested but assumed correct — a subtle bug may exist | Cover them with tests first; fix before proceeding |
| CLI entry point has many command branches; one may not thread `profile` correctly | Grep `Program.fs` for all `resolveActiveProfile` call sites and confirm all pass the flag |

---

## Open Questions

None — the dependency is resolved, the implementation exists, and the scope is bounded to testing and verification.
