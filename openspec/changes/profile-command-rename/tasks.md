## 1. Rename Argu types in Program.fs

- [ ] 1.1 Rename `ProfilesAddArgs` type to `ProfileAddArgs`
- [ ] 1.2 Rename `ProfilesArgs` type to `ProfileArgs`
- [ ] 1.3 Rename `Profiles of ParseResults<ProfilesArgs>` DU case to `Profile of ParseResults<ProfileArgs>` in `CliArgs`
- [ ] 1.4 Update all handler code references to use the new type and case names

## 2. Update Documentation

- [ ] 2.1 Update `README.md` - change `itr profiles add` to `itr profile add` in examples
- [ ] 2.2 Update `docs/cli-reference.md` - rename `### profiles` to `### profile`, `#### profiles add` to `#### profile add`, and update all command examples

## 3. Update Tests

- [ ] 3.1 Update test function names and section comments in `tests/acceptance/PortfolioAcceptanceTests.fs` from `profiles add` to `profile add`

## 4. Update Specs

- [ ] 4.1 Update `openspec/specs/profile-add/spec.md` - replace all `itr profiles add` with `itr profile add`

## 5. Verify

- [ ] 5.1 Run `dotnet build` and confirm no errors
- [ ] 5.2 Run `dotnet test` and confirm all tests pass
