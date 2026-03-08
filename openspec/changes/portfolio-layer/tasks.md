## 1. Project structure

- [ ] 1.1 Create `src/domain/` directory and `Itr.Domain.fsproj` (no project references, no packages)
- [ ] 1.2 Add `src/domain/Domain.fs` with all portfolio domain types (ProfileName, ProductId, RepoPath, CoordinationMode, CoordinationRoot, CoordinationRootConfig, ProductRef, Profile, GitIdentity, Portfolio, ResolvedProduct, PortfolioError)
- [ ] 1.3 Add `Itr.Domain.fsproj` to the solution file
- [ ] 1.4 Add `<ProjectReference>` to `Itr.Domain` in `Itr.Commands.fsproj`
- [ ] 1.5 Add `<ProjectReference>` to `Itr.Domain` and `Itr.Commands` in `Itr.Adapters.fsproj`

## 2. Domain layer (`Itr.Domain`)

- [ ] 2.1 Implement `ProductId` smart constructor validating `[a-z0-9][a-z0-9\-]*` slug pattern; return `InvalidProductId` error on failure
- [ ] 2.2 Implement `Portfolio` duplicate-detection: reject duplicate `ProductId` within a profile and duplicate profile names at construction/parse time
- [ ] 2.3 Confirm all types are pure F# records/DUs with no I/O references

## 3. Adapter layer (`Itr.Adapters`)

- [ ] 3.1 Create `src/adapters/PortfolioAdapter.fs`
- [ ] 3.2 Implement `PortfolioAdapter.configPath : unit -> string` resolving `ITR_HOME` env var or `~/.config/itr/portfolio.json`
- [ ] 3.3 Implement `~` and env-var expansion for path strings (centralised, never in domain/commands)
- [ ] 3.4 Implement `PortfolioAdapter.readConfig : path: string -> Result<Portfolio, PortfolioError>` using `System.Text.Json` with `camelCase` property names and unknown-field tolerance
- [ ] 3.5 Implement a custom `JsonConverter` for `CoordinationRootConfig` DU using the `mode` field to select the correct case
- [ ] 3.6 Add `Env.readVar : string -> string option` and `Fs.dirExists : string -> bool` concrete I/O functions in `PortfolioAdapter.fs`

## 4. Application layer (`Itr.Commands`)

- [ ] 4.1 Create `src/commands/Portfolio.fs`
- [ ] 4.2 Implement `Portfolio.loadPortfolio : configPath: string option -> Result<Portfolio, PortfolioError>` — calls adapter, accepts optional path override
- [ ] 4.3 Implement `Portfolio.resolveActiveProfile : portfolio: Portfolio -> flagProfile: string option -> readEnv: (string -> string option) -> Result<Profile, PortfolioError>` — precedence: flag > `ITR_PROFILE` > `defaultProfile`; case-insensitive lookup
- [ ] 4.4 Implement `Portfolio.resolveProduct : profile: Profile -> productId: string -> dirExists: (string -> bool) -> Result<ResolvedProduct, PortfolioError>` — validates slug, appends `/.itr`, checks existence
- [ ] 4.5 Document the command pipeline pattern in a code comment: `loadPortfolio >>= resolveActiveProfile >>= resolveProduct >>= executeProductCommand`

## 5. CLI integration (`Itr.Cli`)

- [ ] 5.1 Add `Profile of string` with `[<AltCommandLine("-p")>]` to the top-level Argu parser as a global arg
- [ ] 5.2 Add `Output of string` global flag for `--output json` machine-readable mode
- [ ] 5.3 Thread the resolved `--profile` value into `resolveActiveProfile` at the CLI dispatch layer

## 6. Tests — unit (`tests/communication/`)

- [ ] 6.1 Create `tests/communication/PortfolioDomainTests.fs` and add it to the communication test project
- [ ] 6.2 Test `ProductId` slug validation: valid slugs succeed, uppercase/special-char slugs return `InvalidProductId`
- [ ] 6.3 Test duplicate `ProductId` detection within a profile
- [ ] 6.4 Test `resolveActiveProfile` precedence: flag > env > default > error (using stub `readEnv`)
- [ ] 6.5 Test `resolveActiveProfile` case-insensitive lookup
- [ ] 6.6 Test `resolveProduct` with stub `dirExists` returning true (success path)
- [ ] 6.7 Test `resolveProduct` with stub `dirExists` returning false → `CoordRootNotFound`
- [ ] 6.8 Test `resolveProduct` with unknown product ID → `ProductNotFound`

## 7. Tests — acceptance (`tests/acceptance/`)

- [ ] 7.1 Create `tests/acceptance/PortfolioAcceptanceTests.fs` and add it to the acceptance test project
- [ ] 7.2 Write fixture that creates a real temp directory with a valid `portfolio.json` and a `.itr/` subdirectory
- [ ] 7.3 Test full pipeline: valid config → profile resolves → product resolves → `ResolvedProduct` returned
- [ ] 7.4 Test `ConfigNotFound` when config file is absent
- [ ] 7.5 Test `ConfigParseError` when config file contains malformed JSON
- [ ] 7.6 Test all three coordination modes (`standalone`, `primary-repo`, `control-repo`) resolve to `<dir>/.itr`

## 8. Build verification

- [ ] 8.1 Run `dotnet build` — zero warnings, zero errors
- [ ] 8.2 Run `dotnet test` — all new and existing tests pass
