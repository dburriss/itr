## 1. Rename Argu types in Program.fs

- [x] 1.1 Rename `ProfilesAddArgs` type to `ProfileAddArgs`
- [x] 1.2 Rename `ProfilesArgs` type to `ProfileArgs`
- [x] 1.3 Rename `Profiles of ParseResults<ProfilesArgs>` DU case to `Profile of ParseResults<ProfileArgs>` in `CliArgs`
- [x] 1.4 Update all handler code references to use the new type and case names

## 2. Update Documentation

- [x] 2.1 Update `README.md` - change `itr profiles add` to `itr profile add` in examples
- [x] 2.2 Update `docs/cli-reference.md` - rename `### profiles` to `### profile`, `#### profiles add` to `#### profile add`, and update all command examples

## 3. Update Tests

- [x] 3.1 Update test function names and section comments in `tests/acceptance/PortfolioAcceptanceTests.fs` from `profiles add` to `profile add`

## 4. Update Specs

- [x] 4.1 Update `openspec/specs/profile-add/spec.md` - replace all `itr profiles add` with `itr profile add`

## 5. Verify

- [x] 5.1 Run `dotnet build` and confirm no errors
- [x] 5.2 Run `dotnet test` and confirm all tests pass
