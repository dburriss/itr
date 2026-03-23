## Context

The `itr` tool manages a coordination-root-based backlog. Items live under `<coordRoot>/BACKLOG/<id>/item.yaml` and tasks live under `<coordRoot>/BACKLOG/<id>/tasks/<task-id>/task.yaml`. Currently there is no way to query the backlog from the CLI.

A pre-existing bug exists: `TaskState` has no `Planned` or `Approved` cases, so the adapter's `mapTaskState` silently coerces unknown strings (including `"planned"`) to `Planning`. This must be fixed first as it affects computed status.

Views optionally group items into named sets via `<coordRoot>/BACKLOG/_views/*.yaml`. The list command needs to be aware of them (read-only).

## Goals / Non-Goals

**Goals:**
- Expose `itr backlog list` to show all non-archived backlog items, sorted by creation date.
- Support `--view`, `--status`, `--type` filters and `--output json` for machine consumption.
- Compute `BacklogItemStatus` from task states (no filesystem `plan.md` heuristic).
- Fix `TaskState` bug (`planned` / `approved` silently mapped to `Planning`).
- Read views from `_views/*.yaml`; warn on multi-view membership.

**Non-Goals:**
- Writing or mutating views.
- Pagination or streaming for large backlogs.
- Archiving items (a future concern).

## Decisions

**Snapshot read model**
Load all items, views, and tasks once per invocation into a `BacklogSnapshot`. This avoids N+1 filesystem queries in the common case and gives future commands (`backlog next`, `backlog status`) a reusable primitive.
Alternative: lazy per-item load. Rejected — harder to sort and filter consistently.

**Status computed from `TaskState` only**
No filesystem `plan.md` check. Status is purely derived from task DU cases. Simpler, more consistent, and aligns with the intent of `TaskState`.

**First-match wins for multi-view**
If the same item appears in multiple views, the first view file (alphabetical by filename) wins. A warning is emitted to stderr. Write-time enforcement is deferred to future view commands.

**`IViewStore` as a separate interface**
Keeps `IBacklogStore` focused on items. View reads are orthogonal and may be no-op (empty result when `_views/` is absent).

**`BacklogListFilter` as a pure function argument**
`listBacklogItems` is a pure function over a loaded snapshot. Filtering is decoupled from IO.

## Risks / Trade-offs

- `TaskState` DU change is a **compile-time breaking change** — all exhaustive matches must be updated. Risk is low: the compiler surfaces every case.
- `loadSnapshot` does a full scan on every invocation — acceptable for MVP; snapshot design allows future caching.
- `_views/` absent is handled gracefully (returns `Ok []`), but malformed `item.yaml` surfaces the first error immediately (no silent skip).

## Migration Plan

1. Update `TaskState` and fix `mapTaskState` / `taskStateToString`.
2. Compile and fix all exhaustive match errors.
3. Add domain types, interfaces, adapter implementations, use-case functions.
4. Wire CLI and composition root.
5. No data migration needed — existing `task.yaml` files with `state: planned` will now parse correctly.
