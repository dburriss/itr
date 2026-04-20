## Context

`BacklogItemType` is a discriminated union in `Domain.fs` that classifies backlog items. Currently it supports `Feature | Bug | Chore | Spike`. The entire parsing/serialization pipeline, CLI arguments, TUI prompts, sort ordering, and YAML adapter all reference this type explicitly. Adding a new case requires touching all of these layers consistently.

A secondary issue exists in `YamlAdapter.fs`: unknown type strings silently fall back to `Feature` instead of returning an error. This masks data corruption and should be fixed as part of this change.

## Goals / Non-Goals

**Goals:**
- Add `Refactor` as a first-class `BacklogItemType` case
- Ensure `tryParse`/`toString` round-trip correctly for `"refactor"`
- Fix silent fallback in `YamlAdapter.fs` — unknown types should return a structured error
- Update all user-facing surfaces (CLI, TUI, error messages, sort order)
- Add unit tests covering parse, stringify, and error messages

**Non-Goals:**
- No data migration (YAML files with `"refactor"` will just work after the change)
- No changes to portfolio or task domain models
- No reordering of existing type priorities beyond inserting Refactor between Chore and Spike

## Decisions

**1. Position in sort order: after Chore (3), before Spike (4)**
Refactor is operationally similar to Chore (maintenance work) but distinct in intent. Placing it between Chore and Spike groups related maintenance types together. Spike remains last as it is the most exploratory/uncertain.

**2. Fix YamlAdapter silent fallback as part of this change**
The fallback is a latent bug independent of adding Refactor. Since we're already touching this code, fixing it now avoids a separate cleanup. Failing with a structured `Result` error is consistent with domain-layer conventions.

**3. No intermediate migration tooling**
YAML files with `"refactor"` type were previously rejected (or silently misclassified). After this change they parse correctly. No migration script needed.

## Risks / Trade-offs

- **YamlAdapter fix breaks previously-"working" invalid YAML** → Mitigation: This is desired behavior. Items that were silently falling back to `Feature` were already wrong. Explicit failure surfaces the corruption rather than hiding it.
- **Exhaustive match warnings if cases are missed** → Mitigation: F# compiler will emit warnings/errors on non-exhaustive matches, making it easy to find all sites that need updating.

