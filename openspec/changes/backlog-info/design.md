## Context

The `itr` CLI currently supports `backlog list` (tabular overview), `backlog add` (create), `backlog take` (create tasks), and `backlog archive`. There is no way to inspect a single item in detail without opening the raw YAML file. The `BacklogItem` domain type already carries all relevant fields (`Summary`, `AcceptanceCriteria`, `Dependencies`, `Priority`, `Repos`), and tasks are loaded via `ITaskStore.ListTasks`. The computed status logic already exists in `BacklogItemStatus.compute`.

## Goals / Non-Goals

**Goals:**
- Add `itr backlog info <id>` subcommand that shows all fields of a single backlog item
- Display computed status and associated tasks (id, repo, state)
- Support `--output json` for machine-readable output
- Return non-zero exit when the item is not found

**Non-Goals:**
- Editing or mutating the backlog item
- Showing task file contents or implementation notes
- Filtering or paging (single-item command)

## Decisions

**1. New `BacklogItemDetail` record in domain vs. reuse `BacklogItemSummary`**

`BacklogItemSummary` was designed for the list view and omits `Summary`, `AcceptanceCriteria`, `Dependencies`, and the full task list. Rather than overloading it, add a `BacklogItemDetail` type that composes the full `BacklogItem`, computed status, view id, and the task list. This keeps the list-path lightweight and avoids polluting `BacklogItemSummary` with fields that are irrelevant at list scale.

**2. `getBacklogItemDetail` in `BacklogUsecase.fs`**

The handler logic (load item, load tasks, compute status, look up view) is orchestration that belongs in the feature layer, not the CLI. A pure `getBacklogItemDetail` function taking stores + coordRoot + id mirrors the pattern used by `loadSnapshot` / `listBacklogItems`. The CLI handler becomes a thin dispatcher.

**3. Table output layout**

Human output uses a Spectre.Console `Table` for structured fields (label/value pairs) and a separate `Table` for the tasks section, consistent with `backlog list`. JSON output mirrors the field set as a flat object with a `tasks` array.

## Risks / Trade-offs

- [Risk] View lookup requires loading all views to find membership for a single item → Mitigation: reuse `IViewStore.ListViews` (already small YAML files); no caching needed at this scale.
- [Risk] Archived items stored under `BACKLOG/_archive/` may not be reachable via `IBacklogStore.LoadBacklogItem` → Mitigation: spec will require non-archived path only for now; archived lookup is out of scope.
