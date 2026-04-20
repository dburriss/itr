## 1. Domain Layer

- [ ] 1.1 Add `| Refactor` case to `BacklogItemType` DU in `src/domain/Domain.fs`
- [ ] 1.2 Add `"refactor" -> Ok Refactor` to `tryParse` in `Domain.fs`
- [ ] 1.3 Add `| Refactor -> "refactor"` to `toString` in `Domain.fs`

## 2. YAML Adapter

- [ ] 2.1 Fix YAML read path 1 in `src/adapters/YamlAdapter.fs` (~line 331) to return error instead of falling back to `Feature` on unknown type
- [ ] 2.2 Fix YAML read path 2 in `YamlAdapter.fs` (~line 401) with same fix
- [ ] 2.3 Fix YAML read path 3 in `YamlAdapter.fs` (~line 572) with same fix

## 3. Sort Ordering

- [ ] 3.1 Add `| Refactor -> 3` to `typeOrder` in `src/features/Backlog/BacklogUsecase.fs` and shift `| Spike -> 4`

## 4. CLI and TUI

- [ ] 4.1 Add `"refactor"` to all three error messages listing valid types in `src/cli/Program.fs` (~lines 56, 76, 443)
- [ ] 4.2 Add `"refactor"` to the type selection array in `src/cli/InteractivePrompts.fs` (~line 132)

## 5. Tests

- [ ] 5.1 Add unit test: `BacklogItemType.tryParse "refactor"` returns `Ok Refactor`
- [ ] 5.2 Add unit test: `BacklogItemType.toString Refactor` returns `"refactor"`
- [ ] 5.3 Add unit test: error message for invalid type includes `"refactor"`

## 6. Verify

- [ ] 6.1 Run `dotnet build` and confirm zero errors
- [ ] 6.2 Run `dotnet test` and confirm all tests pass
