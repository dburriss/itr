## 1. CLI Arguments

- [ ] 1.1 Add `ProfileListArgs` type with `Output` option (string) to `src/cli/Program.fs`
- [ ] 1.2 Add `List of ParseResults<ProfileListArgs>` case to `ProfileArgs` discriminated union
- [ ] 1.3 Add usage string for the new `List` case in `ProfileArgs.IArgParserTemplate`

## 2. Handler Implementation

- [ ] 2.1 Add `handleProfileList` function that accepts `configPath: string`, `portfolio: Portfolio`, and `listArgs: ParseResults<ProfileListArgs>`
- [ ] 2.2 Resolve output format via `parseOutputFormat` from `listArgs.TryGetResult ProfileListArgs.Output`
- [ ] 2.3 Implement `TableOutput` branch: Spectre.Console `Table` with columns Name, Git Name, Git Email, Products; prefix default profile name with `*`
- [ ] 2.4 Implement `JsonOutput` branch: print JSON array with fields `name`, `isDefault`, `gitName`, `gitEmail`, `productCount`
- [ ] 2.5 Implement `TextOutput` branch: one line per profile, format `[*]<name> | <gitName> | <gitEmail> | <productCount>` (`*` prefix only for default)
- [ ] 2.6 Handle missing git identity fields as empty string in all output formats

## 3. Dispatch

- [ ] 3.1 In the profile command handler block, load portfolio via `Portfolio.loadPortfolio (Some configPath)` and dispatch `ProfileArgs.List` to `handleProfileList`
- [ ] 3.2 Update the `| None ->` fallback branch to suggest `profile list` along with `profile add`

## 4. Tests

- [ ] 4.1 Add acceptance test `profile list returns all profiles` in `tests/acceptance/PortfolioAcceptanceTests.fs` covering table output with default marker
- [ ] 4.2 Add acceptance test `profile list --output json returns JSON array`
- [ ] 4.3 Add acceptance test `profile list with empty portfolio returns empty list`

## 5. Verification

- [ ] 5.1 Run `dotnet build` and fix any compilation errors
- [ ] 5.2 Run `dotnet test` and fix any failing tests
