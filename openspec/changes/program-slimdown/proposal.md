## Why

`src/cli/Program.fs` is still a large mixed-responsibility entry point even after the earlier entry-point refactor. That size makes command work slow, raises merge conflict risk, and keeps command orchestration, dependency wiring, formatting, and domain IO sequencing coupled in one place.

## What Changes

- Slim `src/cli/Program.fs` to a routing-focused module that keeps only opens, active patterns, product/profile resolution helpers, dispatch arms, and `main`.
- Extract Argu DU definitions, the CLI composition root, shared error formatting, and shared rendering helpers into dedicated CLI files.
- Move command orchestration out of `Program.fs` and into effectful domain usecases with intersection-constrained dependency surfaces.
- Add vertical-slice CLI adapters per command for `toInput` mapping and `Format.result` output rendering.
- Add in-memory test doubles and builder helpers so usecase tests target behavior at the natural boundary without structural assertions.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `domain-structure`: extend the architectural requirements so the CLI layer is also organized as vertical slices and `Program.fs` is reduced to pure routing while domain usecases own operation IO.

## Impact

- Affects `src/cli/Program.fs`, new `src/cli/*.fs` slice files, and `itr.fsproj` compile ordering.
- Affects existing domain usecase modules under `src/domain/` by tightening dependency signatures and moving more operation sequencing into `execute` or query functions.
- Adds test infrastructure under `tests/` for in-memory filesystem, stores, harness doubles, and small builder helpers.
- Preserves existing command behavior; this change is structural rather than a user-facing feature change.
