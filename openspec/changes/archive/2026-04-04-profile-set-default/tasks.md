## 1. Usecase

- [x] 1.1 Add `SetDefaultProfileInput` record type to `PortfolioUsecase.fs`
- [x] 1.2 Implement `setDefaultProfile` usecase function in `PortfolioUsecase.fs` — load portfolio, validate profile exists case-insensitively, return updated `Portfolio` with new `DefaultProfile`

## 2. CLI Args

- [x] 2.1 Add `ProfileSetDefaultArgs` Argu DU to `Program.fs` with `--local` and `--global` flags and `<name>` main command argument
- [x] 2.2 Add `SetDefault of ParseResults<ProfileSetDefaultArgs>` case to `ProfileArgs` DU in `Program.fs`

## 3. CLI Dispatch

- [x] 3.1 Add `SetDefault` branch in the profile subcommand dispatch in `Program.fs`
- [x] 3.2 Implement `--global` path: load global config, call `setDefaultProfile`, save, print success with global path
- [x] 3.3 Implement `--local` path: resolve product root (error if not resolvable), load/create local config, call `setDefaultProfile`, save, print success with local path
- [x] 3.4 Implement auto-detect path (no flag): check if local `itr.json` exists → use local; else use global

## 4. Error Handling

- [x] 4.1 Fix `ProfileNotFound` case in `formatPortfolioError` — replace catch-all with explicit message `"Profile '{name}' not found. Run 'profile add {name}' to create it."`

## 5. Tests

- [x] 5.1 Add unit tests in `PortfolioDomainTests.fs` for `setDefaultProfile` — profile found, profile not found
- [x] 5.2 Add acceptance tests in `PortfolioAcceptanceTests.fs` for `profile set-default --global`
- [x] 5.3 Add acceptance tests in `PortfolioAcceptanceTests.fs` for `profile set-default --local` (including local file creation)
- [x] 5.4 Add acceptance tests in `PortfolioAcceptanceTests.fs` for auto-detect behaviour

## 6. Build & Verify

- [x] 6.1 Run `dotnet build` and ensure no errors
- [x] 6.2 Run `dotnet test` and ensure all tests pass
