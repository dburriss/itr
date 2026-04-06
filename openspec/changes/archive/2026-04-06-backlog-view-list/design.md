## Context

The `itr` CLI already has a consistent pattern for list commands (`product list`, `task list`, `backlog list`). The `IViewStore.ListViews` and `IBacklogStore.ListArchivedBacklogItems` interfaces already exist and are wired into `AppDeps`. The work is purely additive: new arg types, a new handler, and routing into the existing dispatch loop.

Views live at `<coordRoot>/BACKLOG/_views/*.yaml` and are modeled as `BacklogView { Id; Description option; Items: string list }`.

## Goals / Non-Goals

**Goals:**
- Add `itr view list` command displaying view ID, description, total item count, and archived item count
- Support `--output table|json|text` consistent with all other list commands
- Accept optional `--product <id>` flag with fallback to working-directory product resolution
- Handle empty view list with a friendly message

**Non-Goals:**
- Creating, editing, or deleting views
- Filtering or searching views
- Any changes to `IViewStore` or `IBacklogStore` interfaces

## Decisions

**1. Follow the `TaskListArgs` / `TaskArgs` pattern exactly.**  
`ViewListArgs` wraps `Output` (and optionally `Product`); `ViewArgs` has a single `List` case. This is the lowest-friction path to consistency. No alternatives considered — deviation would break user expectations.

**2. Item count derived from `view.Items.Length`; archived count from intersection with `IBacklogStore.ListArchivedBacklogItems`.**  
The `plan.md` prescribes this approach. `ListArchivedBacklogItems` returns `(BacklogItem * string) list` so we extract `.Id` from the first element of each tuple, then set-intersect with `view.Items`.

**3. No shared formatter abstraction.**  
All existing handlers inline the three-way `match format with` block. Introducing a shared formatter would be out of scope for this change and would require touching multiple existing handlers.

**4. Empty description renders as empty string `""`.**  
The `Description` field is `string option`; we use `Option.defaultValue ""` for display.

## Risks / Trade-offs

- [`ListArchivedBacklogItems` performance] If the archive is very large, intersecting per-view could be slow → Mitigation: load archived IDs once into a `Set<string>` before iterating views.
- [`View directory absent`] `ListViews` already returns `Ok []` when directory is missing, so empty-list handling covers this case transparently.
