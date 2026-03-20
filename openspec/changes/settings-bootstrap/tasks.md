## 1. Rename portfolio.json → itr.json

- [ ] 1.1 Update `PortfolioConfigAdapter.ConfigPath()` in `src/adapters/PortfolioAdapter.fs` to return `itr.json` instead of `portfolio.json`
- [ ] 1.2 Update all `portfolio.json` string references in `tests/acceptance/PortfolioAcceptanceTests.fs` to `itr.json`
- [ ] 1.3 Run `dotnet build` to confirm no compile errors from the rename

## 2. Domain: Add BootstrapWriteError

- [ ] 2.1 Add `| BootstrapWriteError of path: string * message: string` to the `PortfolioError` DU in `src/domain/Domain.fs`

## 3. Adapter: Ensure WriteFile creates parent directories

- [ ] 3.1 Update `FileSystemAdapter.WriteFile` in `src/adapters/Library.fs` to call `Directory.CreateDirectory(Path.GetDirectoryName(path))` before writing

## 4. Feature: Implement bootstrapIfMissing

- [ ] 4.1 Add `bootstrapIfMissing : configPath: string -> EffectResult<#IFileSystem, bool, PortfolioError>` to `src/features/Portfolio/PortfolioUsecase.fs`
- [ ] 4.2 Implement: if file exists return `Ok false`; otherwise write default `itr.json` content, return `Ok true` on success, map `IoError` to `BootstrapWriteError` on failure

## 5. CLI: Wire bootstrap and extend error formatter

- [ ] 5.1 In `src/cli/Program.fs`, resolve `configPath` once at the top of `dispatch` via `(deps :> IPortfolioConfig).ConfigPath()`
- [ ] 5.2 Call `bootstrapIfMissing configPath |> Effect.run deps` before `loadPortfolio`, passing `configPath` through
- [ ] 5.3 Print informational message (including `configPath` and "itr init") only when `bootstrapIfMissing` returns `Ok true`
- [ ] 5.4 Add `| BootstrapWriteError(path, msg) -> $"Could not create itr.json at {path}: {msg}"` to the error formatter in `Program.fs`

## 6. Tests

- [ ] 6.1 Add acceptance test: bootstrap creates file and parent directory when both are absent
- [ ] 6.2 Add acceptance test: bootstrap is idempotent (existing file is not overwritten)
- [ ] 6.3 Add acceptance test: bootstrap returns `BootstrapWriteError` when write fails (unwritable path)
- [ ] 6.4 Run `dotnet test` to confirm all tests pass
