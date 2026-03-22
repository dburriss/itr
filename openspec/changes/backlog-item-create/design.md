## Context

The `itr` CLI currently supports reading and taking backlog items but has no write path for creating them. Users must manually create `<coordRoot>/BACKLOG/<id>/item.yaml` files, which bypasses all validation. The domain `BacklogItem` record is also under-specified compared to the YAML schema — it lacks `Type`, `Priority`, `Summary`, `AcceptanceCriteria`, `Dependencies`, and `CreatedAt`.

The existing error type `TakeError` is scoped only to the take command. As more backlog operations are added it becomes a general `BacklogError` type, requiring a rename across all use sites.

## Goals / Non-Goals

**Goals:**
- Add `itr backlog add <id> --title <title>` CLI command
- Validate id, title, type, repos, and dependency ids before writing
- Auto-resolve repo when product has exactly one repo
- Extend `BacklogItem` record and `BacklogItemDto` to match `item.yaml` schema
- Rename `TakeError` → `BacklogError` and add new error cases
- Implement `WriteBacklogItem` and `BacklogItemExists` on `IBacklogStore`
- Accept tests and communication tests for the new command

**Non-Goals:**
- Editing or deleting backlog items
- Validating that dependency backlog ids actually exist on disk
- Interactive prompts for missing fields

## Decisions

### 1. Rename `TakeError` → `BacklogError`
**Decision**: Rename now, in a single compiler-guided pass, before adding new error cases.  
**Rationale**: Adding `DuplicateBacklogId` and `InvalidItemType` to `TakeError` would be semantically wrong. The compiler will surface every use site, making the rename safe and complete.  
**Alternative considered**: Introduce a separate `CreateError` type — rejected because it fragments error handling and complicates the `IBacklogStore` interface signature.

### 2. `--title` as `Mandatory` in Argu
**Decision**: Mark `Title` with `[<Mandatory>]` in the Argu `AddArgs` DU so the parser rejects missing titles at parse time.  
**Rationale**: Title is always required; failing at parse time gives the user the clearest error message with no extra validation code needed.

### 3. Repo auto-resolution
**Decision**: If `--repo` is omitted and the product has exactly one repo, use it silently. If omitted with multiple repos, fail with a clear error.  
**Rationale**: Single-repo products are the common case in this codebase; requiring `--repo` every time would be friction with no benefit.  
**Alternative considered**: Always require `--repo` — rejected as unnecessary for the common case.

### 4. Pure `createBacklogItem` function in new `BacklogUsecase.fs`
**Decision**: Place the creation logic as a pure function in `src/features/Backlog/BacklogUsecase.fs`, mirroring the existing `TaskUsecase.fs` pattern.  
**Rationale**: Keeps I/O out of business logic, simplifies unit testing, and follows the established vertical-slice structure.

### 5. Backward-compatible DTO extension
**Decision**: All new `BacklogItemDto` fields are optional (nullable in YamlDotNet terms) so existing `item.yaml` files without them still parse cleanly.  
**Rationale**: Existing backlog items on disk lack the new fields. The domain record uses `option` types for optional fields; `CreatedAt` defaults to `DateOnly.MinValue` when absent.

## Risks / Trade-offs

- `BacklogItem` record expansion → any exhaustive pattern matches on the record will fail to compile → **Mitigation**: compiler errors guide all update sites; run build after rename step.
- `TakeError` rename touches `Domain.fs`, `Interfaces.fs`, `YamlAdapter.fs`, `TaskUsecase.fs`, `Program.fs` → **Mitigation**: do rename in one commit before new code; compiler surfaces all sites.
- `created_at` format inconsistency → **Mitigation**: use `DateOnly.ToString("yyyy-MM-dd")` consistent with existing `ItrTaskDto.CreatedAt`.
