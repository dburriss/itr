## 1. Usecase

- [ ] 1.1 Add `SetDefaultProfileInput` record type to `PortfolioUsecase.fs`
- [ ] 1.2 Implement `setDefaultProfile` usecase function in `PortfolioUsecase.fs` — load portfolio, validate profile exists case-insensitively, return updated `Portfolio` with new `DefaultProfile`

## 2. CLI Args

- [ ] 2.1 Add `ProfileSetDefaultArgs` Argu DU to `Program.fs` with `--local` and `--global` flags and `<name>` main command argument
- [ ] 2.2 Add `SetDefault of ParseResults<ProfileSetDefaultArgs>` case to `ProfileArgs` DU in `Program.fs`

## 3. CLI Dispatch

- [ ] 3.1 Add `SetDefault` branch in the profile subcommand dispatch in `Program.fs`
- [ ] 3.2 Implement `--global` path: load global config, call `setDefaultProfile`, save, print success with global path
- [ ] 3.3 Implement `--local` path: resolve product root (error if not resolvable), load/create local config, call `setDefaultProfile`, save, print success with local path
- [ ] 3.4 Implement auto-detect path (no flag): check if local `itr.json` exists → use local; else use global

## 4. Error Handling

- [ ] 4.1 Fix `ProfileNotFound` case in `formatPortfolioError` — replace catch-all with explicit message `"Profile '{name}' not found. Run 'profile add {name}' to create it."`

## 5. Tests

- [ ] 5.1 Add unit tests in `PortfolioDomainTests.fs` for `setDefaultProfile` — profile found, profile not found
- [ ] 5.2 Add acceptance tests in `PortfolioAcceptanceTests.fs` for `profile set-default --global`
- [ ] 5.3 Add acceptance tests in `PortfolioAcceptanceTests.fs` for `profile set-default --local` (including local file creation)
- [ ] 5.4 Add acceptance tests in `PortfolioAcceptanceTests.fs` for auto-detect behaviour

## 6. Build & Verify

- [ ] 6.1 Run `dotnet build` and ensure no errors
- [ ] 6.2 Run `dotnet test` and ensure all tests pass
