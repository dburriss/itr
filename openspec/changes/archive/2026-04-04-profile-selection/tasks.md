## 1. Audit existing implementation

- [x] 1.1 Review `resolveActiveProfile` at `src/features/Portfolio/PortfolioUsecase.fs:29–49` and confirm it correctly implements the full precedence chain and whitespace guards
- [x] 1.2 Grep `src/cli/Program.fs` for all `resolveActiveProfile` call sites and confirm every command branch passes the `--profile` flag value

## 2. Unit tests — whitespace fallthrough

- [x] 2.1 Add test: whitespace-only `--profile` flag falls through to `ITR_PROFILE` env var → env var profile resolved
- [x] 2.2 Add test: whitespace-only `--profile` flag falls through to `defaultProfile` → default profile resolved
- [x] 2.3 Add test: whitespace-only `ITR_PROFILE` falls through to `defaultProfile` → default profile resolved

## 3. Unit tests — ProfileNotFound via env and default

- [x] 3.1 Add test: `ITR_PROFILE` names a non-existent profile → `ProfileNotFound "xyz"`
- [x] 3.2 Add test: `defaultProfile` names a non-existent profile → `ProfileNotFound "xyz"`

## 4. Unit tests — case-insensitive lookup via env and default

- [x] 4.1 Add test: `ITR_PROFILE=WORK` matches profile `"work"` → profile resolved
- [x] 4.2 Add test: `defaultProfile = "Work"` matches profile `"work"` → profile resolved

## 5. Acceptance test

- [x] 5.1 Check `tests/acceptance/PortfolioAcceptanceTests.fs` for an existing end-to-end profile resolution test
- [x] 5.2 Add an acceptance test exercising the full flag → env → default resolution path through `IEnvironment` adapter if one is missing

## 6. Verify

- [x] 6.1 Run `dotnet test` and confirm all new and existing tests pass
