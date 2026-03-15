## Context

`itr` already has a portfolio-resolution pipeline that can load a portfolio config, resolve a profile, and resolve a product down to a `CoordinationRoot`. The next step is bridging from that resolved product to per-repo task files.

The codebase has `IYamlService` as a stub and no concrete YAML adapter yet. `Domain.fs` and `Interfaces.fs` have the portfolio layer's types and interfaces; new domain types for the backlog/task layer will be appended to these files.

The coordination root filesystem layout uses uppercase directory names (`BACKLOG/`, `TASKS/`) as established by the plan.

## Goals / Non-Goals

**Goals:**
- Add domain types for backlog items, tasks, and product config
- Fill in `IYamlService` and add `IProductConfig`, `IBacklogStore`, `ITaskStore` interfaces
- Implement a pure `takeBacklogItem` use-case returning `ItrTask` list
- Implement `YamlAdapter` with `YamlDotNet` for all YAML-backed interfaces
- Wire the `backlog take <backlog-id>` CLI command end-to-end
- Support `--task-id` override for single-repo items
- Support JSON output via `--output json`

**Non-Goals:**
- Multi-repo coordination / inter-repo task synchronisation
- `product-init` or `backlog-item-create` commands (fixture files exist for testing)
- Task state transitions (only `planning` state is written here)
- Updating or deleting existing task files

## Decisions

**D1 — YAML library: `YamlDotNet`**
Chosen because it is the de-facto .NET YAML library, has CLIMutable / attribute-driven snake_case support, and is already implied by the stub interface. Alternative (hand-rolled serializer) rejected: too brittle for evolving schemas.

**D2 — DTOs are CLIMutable record types in `YamlAdapter.fs`**
DTOs live inside the adapter, not in the domain. They are not exposed beyond the adapter boundary. This keeps the domain pure F# types while handling YAML mapping concerns (snake_case, nullable fields) in one place.

**D3 — `IYamlService` generic interface is kept but not used by new stores**
The stores (`IProductConfig`, `IBacklogStore`, `ITaskStore`) have concrete domain return types rather than calling `IYamlService<DTO>` at call sites. `IYamlService` is preserved because the plan requires filling in its placeholder; the adapter implements it but it is not the primary seam for the new stores.

**D4 — `TaskId` generation is pure (use-case responsibility)**
`takeBacklogItem` derives task IDs from existing task lists passed in. The entry point (CLI) re-reads existing IDs immediately before writing to guard against TOCTOU races from external edits (per the risk log in the plan).

**D5 — Error union `TakeError` is defined in `Domain.fs` alongside domain types**
Keeps error handling co-located with the domain that produces it, consistent with `PortfolioError`.

## Risks / Trade-offs

- **YamlDotNet snake_case quirks** → Use `[<YamlMember(Alias="...")>]` on every DTO field; add targeted unit tests while building the adapter.
- **TOCTOU on task ID collision** → CLI entry point re-reads existing task IDs from disk immediately before writing (not just at use-case planning time).
- **`product.yaml` schema evolution** → Keep DTOs minimal; configure `YamlDotNet` to ignore unknown fields.
- **`--task-id` on multi-repo items is an error** → Surfaced as `TaskIdOverrideRequiresSingleRepo` in `TakeError`; no silent truncation.
