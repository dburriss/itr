## Context

The codebase currently constructs paths to `item.yaml` and related files via ad-hoc `Path.Combine` call sites scattered across `YamlAdapter.fs` and `Program.fs`. There is no single authoritative location for path construction logic, making it fragile when directory layout changes. Consumers of `backlog list` and `backlog info` output cannot easily locate the backing file without independently reconstructing the path.

The `IBacklogStore` interface today returns plain domain objects from all four read methods. Adding path information requires either a new separate interface method or changing existing return types.

## Goals / Non-Goals

**Goals:**
- Expose the absolute `item.yaml` path in `backlog list` and `backlog info` output (all formats: text, JSON, table)
- Centralise path construction for backlog items and tasks into `BacklogItem` and `ItrTask` modules in `Domain.fs`
- Replace all inline `Path.Combine` call sites in `YamlAdapter.fs` and `Program.fs` with the new modules
- Propagate path through `IBacklogStore` read methods so the path flows naturally from storage to output

**Non-Goals:**
- Exposing paths in task list/info output (separate backlog item)
- Persisting path information in YAML files
- Providing a dedicated `--path` flag or subcommand for resolving paths

## Decisions

**Decision: Change `IBacklogStore` read method return types to tuples**

Options considered:
1. Return `(item * string)` tuples — changes all four read methods simultaneously; callers that don't need the path use `fst` or pattern-match to discard
2. Add new parallel methods (e.g., `LoadBacklogItemWithPath`) — doubles the interface surface; the adapter must implement both

Chose option 1. The path is always available at the storage layer; there is no scenario where a caller wants the item but the path computation could fail independently. Tuple return is idiomatic F# for co-located data. Compile-time errors will surface all missed call sites immediately.

**Decision: Path construction modules in `Domain.fs`, not a separate module**

Path construction is pure logic over domain identifiers (`BacklogId`, `TaskId`) and a root string. Placing `BacklogItem` and `ItrTask` modules immediately after their corresponding types in `Domain.fs` keeps domain logic co-located without introducing a new file. The functions are simple one-liners that compose `Path.Combine`.

**Decision: Archived item paths come from the scan, not from a reconstruction function**

The archived item path is already computed during the directory scan in `YamlAdapter.fs` (it reflects the actual `_archive/{date}-{id}/item.yaml` location). Recomputing it via a module function would require knowing the archive folder name prefix, which is an adapter-level detail. The scan-derived path is threaded through as-is.

## Risks / Trade-offs

- [Interface change touches 7 files] → Mitigation: F# compiler will flag all missed call sites. No test fakes exist, so no fakes need updating — only the real adapter implements `IBacklogStore`.
- [Tuple return adds noise at call sites that ignore the path] → Mitigation: Pattern matching `let (item, _) = ...` is concise; `List.map fst` for list cases. Impact is limited to ~5 call sites.
- [Path is absolute and machine-specific] → Acceptable: the field is intended for tooling and scripts running on the same machine; not for interchange.
