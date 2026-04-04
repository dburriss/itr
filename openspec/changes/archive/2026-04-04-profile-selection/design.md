## Context

`resolveActiveProfile` in `src/features/Portfolio/PortfolioUsecase.fs:29–49` implements the full precedence chain (`--profile` → `ITR_PROFILE` → `defaultProfile`) as specified in `openspec/specs/profile-resolution/spec.md`. The implementation uses whitespace guards, case-insensitive lookup, and typed `ProfileNotFound` errors — all as required.

The gap is test coverage. Several branches are exercised by existing tests but the edge cases (whitespace-only values falling through, case-insensitive lookup via env/default paths, `ProfileNotFound` via env/default) have no dedicated tests. An end-to-end acceptance test for the full resolution path through real adapters may also be missing.

The CLI entry point (`src/cli/Program.fs:1401–1786`) wires `AppDeps` with `EnvironmentAdapter`; no structural changes are expected there.

## Goals / Non-Goals

**Goals:**
- Add unit tests covering all untested branches of `resolveActiveProfile` in `tests/communication/PortfolioDomainTests.fs`
- Audit `Program.fs` command branches to confirm `--profile` is threaded to `resolveActiveProfile` everywhere
- Add or verify an acceptance test in `tests/acceptance/PortfolioAcceptanceTests.fs` for the full resolution path
- Fix any implementation gap found during audit (targeted, with regression test)

**Non-Goals:**
- Changing the `resolveActiveProfile` signature or precedence logic (unless a bug is found)
- Adding new CLI flags or configuration fields
- Modifying any spec requirements

## Decisions

### Decision: Test via injected `readEnv` / `TestEnvDeps`, not real process environment

The existing `TestEnvDeps` in `PortfolioDomainTests.fs` implements `IEnvironment` via a `Map<string, string>`. All new unit tests will use this stub rather than setting real environment variables, keeping tests deterministic and isolated.

*Alternative considered*: `Environment.SetEnvironmentVariable` in test setup/teardown — rejected because it mutates global process state and can leak between parallel test runs.

### Decision: One test per untested scenario from the coverage table

Each scenario in the plan's coverage table maps to exactly one `[<Fact>]` in `PortfolioDomainTests.fs`. This makes failures pinpoint a single branch.

### Decision: Acceptance test uses `EnvironmentAdapter` stub, not real env vars

For the acceptance test, wrap the real adapters but inject a controlled `IEnvironment` implementation to avoid process-level side-effects while still exercising the adapter boundary.

## Risks / Trade-offs

- [Whitespace guard is silently broken] → Covered first by tests; if a bug is found a targeted fix goes in `PortfolioUsecase.fs` before the acceptance test is added.
- [CLI audit may reveal a branch that skips `--profile`] → If found, a fix plus a focused CLI integration test is added; scope is still small.
- [Acceptance test complexity] → Keep it narrow: one scenario per resolution path (flag, env, default); do not duplicate unit-level edge cases.
