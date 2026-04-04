## Context

The `itr` CLI manages tasks stored as YAML files on the filesystem at `<coordRoot>/BACKLOG/<backlogId>/tasks/<taskId>/task.yaml`. A companion `plan.md` may exist in the same directory.

Currently `task list` and `task info` output task data (id, state, repo, backlog) but omit filesystem paths. Users and tooling (AI agents, editors) must reconstruct paths from IDs, which is error-prone. The `backlog-item-path` change already established the pattern of surfacing paths from the adapter layer for backlog items — this change follows the same pattern for tasks.

The `ITaskStore.ListTasks` interface currently returns `Result<ItrTask list, BacklogError>`. The `YamlAdapter` already holds the `taskFile` local variable during scanning but discards it.

## Goals / Non-Goals

**Goals:**
- Surface absolute `task.yaml` and `plan.md` paths through the data pipeline from adapter to CLI output
- Follow the existing `backlog-item-path` pattern (path constructed in adapter, threaded through use-case, emitted in output)
- Update all three output formats (text, JSON, table) for both `task list` and `task info`

**Non-Goals:**
- Changes to `ItrTask` domain type
- Changes to task create/write/archive commands
- Filesystem structure changes or data migrations
- New path derivation logic in the domain — paths come directly from the adapter scan

## Decisions

### Decision: Paths constructed at adapter layer, not domain layer

**Choice**: The `YamlAdapter` returns `(ItrTask * string) list` where the string is the absolute `task.yaml` path. The `plan.md` path is derived in the use-case from the `task.yaml` directory.

**Rationale**: The adapter already holds the full path during filesystem scanning (`let taskFile = Path.Combine(subdir, "task.yaml")`). Returning it avoids a second filesystem traversal or fragile ID-to-path reconstruction. This is identical to the `backlog-item-path` pattern already in production.

**Alternative considered**: Derive paths in the domain from `ItrTask` fields. Rejected because it couples domain logic to filesystem layout and duplicates path construction logic already present in the adapter.

### Decision: `ITaskStore.ListTasks` return type change (breaking)

**Choice**: Change `Result<ItrTask list, BacklogError>` to `Result<(ItrTask * string) list, BacklogError>`.

**Rationale**: Cleanest extension — caller sites that don't need paths discard with `List.map fst`. The F# compiler will catch all missed call sites at build time, making the refactor safe.

**Alternative considered**: Add a separate `ListTasksWithPaths` method. Rejected as unnecessary API surface; the tuple approach is idiomatic F# and self-documenting.

### Decision: `planMdPath` as `string option` in domain, empty string in text output

**Choice**: `PlanMdPath: string option` in `TaskSummary`/`TaskDetail`. Text output uses `""` when `None`; JSON uses `null` or omits the field; table shows empty cell.

**Rationale**: Preserves semantic distinction between "path exists" and "no plan" without requiring consumers to parse sentinel strings.

## Risks / Trade-offs

- **`ITaskStore` interface is breaking** → Mitigated by F# compiler: all call sites fail to compile if not updated, preventing silent regressions.
- **`task list` text output column count changes** (4 → 6, `planApproved` removed) → Any scripts parsing the text output will break. Documented as a breaking change in the proposal.
- **`plan.md` existence check adds I/O per task** → Acceptable for CLI use; tasks are already scanned from disk. No caching needed at this scale.
