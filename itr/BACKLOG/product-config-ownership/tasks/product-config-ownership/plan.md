# Plan: product-config-ownership

**Status:** Draft  
**Backlog item:** `product-config-ownership`  
**Repo scope:** `itr`

---

## Description

Make `product.yaml` the canonical product definition and reduce `itr.json` to machine-local profile settings plus product root registrations. The implementation needs to stop duplicating product ids and coordination settings across the two files, load the canonical product id from each registered product root, and update the checked-in `itr` product definition and docs to match.

---

## Context

The docs already describe the target ownership model in `docs/config-files.md`, but the code still uses the old shape. `src/adapters/PortfolioAdapter.fs` deserializes `itr.json` into `ProductRefDto` records containing an embedded product `id` plus `CoordinationRootConfig`, and `src/features/Portfolio/PortfolioUsecase.fs` resolves products by trusting that duplicated data and appending `.itr` directly. In parallel, `src/adapters/YamlAdapter.fs` only parses `id` and `repos` from `product.yaml`, and it currently expects the file under the coordination root rather than the product root directory.

This leaves the canonical data split across two sources and creates drift with the checked-in `itr/product.yaml`, which still contains a `profile` field that no longer matches the documented model.

---

## Scope

### 1. Reshape persisted portfolio config around product root paths

- Replace the portfolio DTO shape in `src/adapters/PortfolioAdapter.fs` so `profiles.<id>.products[]` stores only a root directory path.
- Remove JSON serialization concerns tied to `CoordinationRootConfig` from `itr.json`; that information should no longer be persisted there.
- Add the inverse mapping needed to write the new DTO shape back to disk once save flows exist.
- Update bootstrap/round-trip fixtures and acceptance tests to use the new JSON structure.

### 2. Make product loading come from `product.yaml` at the product root

- Expand the `product.yaml` domain/DTO model in `src/domain/Domain.fs` and `src/adapters/YamlAdapter.fs` to include the fields now treated as canonical: `id`, `repos`, `docs`, and `coordination`.
- Change product loading so callers provide a product root directory and the adapter loads `<product-root>/product.yaml`.
- Introduce the mapping logic needed to compute the effective coordination root from the canonical `coordination` section and repo paths.
- Keep existing task-taking behavior working by continuing to expose repo mappings from the same canonical product definition.

### 3. Refactor portfolio resolution to use canonical product metadata

- Update `src/features/Portfolio/PortfolioUsecase.fs` so profile product registrations are path-based and product resolution loads `product.yaml` before matching ids or building a `ResolvedProduct`.
- Detect duplicate registrations using the canonical product id loaded from `product.yaml`, not a persisted id in `itr.json`.
- Decide whether duplicate detection belongs in domain validation, a portfolio usecase, or registration-specific validation, then keep that rule covered by communication tests.
- Update `ResolvedProduct` and any related domain types if they need both the registered root path and the loaded canonical definition.

### 4. Align CLI and adapter boundaries with the new ownership model

- Update capability interfaces in `src/domain/Interfaces.fs` where signatures still assume coordination-root-based product loading.
- Adjust `src/cli/Program.fs` so product selection and backlog commands resolve the coordination root through the loaded product definition instead of the old `CoordinationRootConfig` stored in `itr.json`.
- Preserve the existing profile-selection precedence and bootstrap behavior while swapping in the new product resolution path.

### 5. Refresh checked-in product metadata and user docs

- Update `itr/product.yaml` to match the documented canonical schema and remove fields that belong in machine-local config.
- Reconcile `docs/config-files.md` and `docs/lifecycles.md` with the final implementation details, especially around product root registration and resolution.
- Ensure examples consistently show `itr.json` storing product root directories only.

---

## Dependencies / Prerequisites

- `settings-bootstrap` is already in place and provides the baseline `itr.json` lifecycle.
- `profile-selection` is already represented in the current active-profile flow and must keep working unchanged.
- `product-register` and `product-init` are not implemented yet, so this task should establish the persisted shape and shared resolution helpers those commands will rely on.

---

## Impact on Existing Code

| Location | Change |
|---|---|
| `src/domain/Domain.fs` | Replace duplicated product registration data with path-based registration and richer canonical product definition types |
| `src/domain/Interfaces.fs` | Update product-loading capability to operate on product roots and expose the data needed for resolution |
| `src/adapters/PortfolioAdapter.fs` | Read/write the new `itr.json` product-path shape |
| `src/adapters/YamlAdapter.fs` | Parse canonical `product.yaml` fields and compute coordination metadata from product root + repo layout |
| `src/features/Portfolio/PortfolioUsecase.fs` | Resolve products from registered roots via canonical `product.yaml`; detect duplicate registrations by canonical id |
| `src/cli/Program.fs` | Route backlog/product commands through the new resolution pipeline |
| `tests/acceptance/PortfolioAcceptanceTests.fs` | Update fixtures to register product roots and verify canonical resolution |
| `tests/acceptance/TaskAcceptanceTests.fs` | Update fixtures to place `product.yaml` at product root while keeping coordination-root behavior correct |
| `docs/config-files.md` | Confirm examples and explanations match the implemented ownership rules |
| `itr/product.yaml` | Remove machine-local ownership and reflect canonical product metadata only |

---

## Acceptance Criteria

- `itr.json` stores registered product root directories under profiles without duplicating product ids or coordination settings.
- Product resolution loads `product.yaml` from each registered product root and uses it as the source of truth for product id and coordination layout.
- Duplicate product registration is rejected using the canonical id loaded from `product.yaml`.
- Shared read/write behavior is ready for product registration and init flows to persist the new path-based shape consistently.
- The checked-in `itr` product definition and user-facing config docs reflect the same ownership model.
- A product can be found and loaded successfully through the new model, and backlog execution still resolves the correct coordination root for task execution.
- A repository root can be found successfully through the new model, and backlog execution still resolves the correct coordination root for task execution.

---

## Testing Strategy

### Communication tests

- Update or add `tests/communication/PortfolioDomainTests.fs` coverage for duplicate detection based on canonical product ids rather than duplicated ids in `itr.json`.
- Add resolution tests that prove a registered product root is loaded through `product.yaml` and produces the expected coordination root.
- Add resolution tests that prove repo paths in `product.yaml` are correctly mapped to coordination roots for backlog execution.

### Acceptance tests

- Rewrite `tests/acceptance/PortfolioAcceptanceTests.fs` fixtures so `itr.json` stores only product root paths and each root contains a `product.yaml` describing its canonical id and coordination mode.
- Add an acceptance case for duplicate registrations where two different paths point to products with the same canonical id.
- Update task-taking acceptance tests so `product.yaml` lives at the product root and backlog execution still resolves the correct coordination root.

### Verification

- Run `dotnet build`.
- Run `dotnet test`.
- If formatting changes are needed, run `mise run format` before final verification.

---

## Risks / Challenges

| Risk | Mitigation |
|---|---|
| Refactor touches both portfolio resolution and task execution paths | Introduce the canonical product-loading model first, then migrate portfolio and task callers onto it |
| Coordination root calculation may regress for `primary-repo` and `control-repo` modes | Add fixture coverage for all supported modes and assert concrete filesystem paths |
| The old and new config shapes may be mixed during the transition | Keep adapter parsing and tests explicit about the final supported shape; avoid half-migration state in docs |
| `product-register` and `product-init` are not implemented yet | Build reusable save/load helpers now so those commands can adopt the shape directly when added |

---

## Open Questions

1. Should `ProductConfig` be extended in place to carry `docs` and `coordination`, or should portfolio resolution introduce a separate richer product-definition type while backlog-taking keeps a narrower projection?
2. Do we want temporary backward-compatibility for the old `itr.json` registration shape, or is this a clean break with fixtures/docs updated atomically?
