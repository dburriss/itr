## Context

The `itr` CLI already provides `task list` and `backlog` commands. Users can see all tasks but have no way to drill into a single task's details without cross-referencing the backlog manually. The domain model (`ItrTask`) already carries all the data needed; this change adds a presentation layer on top.

Key constraints:
- The plan file is an IO concern; the domain function must remain pure by accepting `planExists: bool` from the caller.
- `ListAllTasks` already scans both active and archive directories, so archived tasks are queryable without extra work.
- Adding `TaskNotFound` to `BacklogError` is a breaking change — all match sites must be updated.

## Goals / Non-Goals

**Goals:**
- Add `itr task info <id>` that renders the full detail view for a single task.
- Keep domain logic pure; all IO at the CLI handler level.
- Support `--output json` for machine consumption.
- Expose sibling tasks (tasks with the same `SourceBacklog`).

**Non-Goals:**
- Mutating task state — this is read-only.
- Searching by repo or backlog (use `task list` for that).
- Displaying the plan content — only existence and approval status.

## Decisions

### 1. `planExists` passed into the domain function
**Decision**: `getTaskDetail` accepts a `planExists: bool` parameter rather than performing any filesystem check itself.

**Rationale**: Keeps the domain layer free of IO abstractions. The handler resolves `SourceBacklog` by scanning `allTasks` first, constructs the path, then checks via `IFileSystem.FileExists` before calling `getTaskDetail`. This is already the pattern used in `takeBacklogItem`.

**Alternative considered**: Inject `IFileSystem` into `getTaskDetail` — rejected because the domain should not depend on infrastructure.

### 2. Single `getTaskDetail` function in `TaskUsecase.fs`
**Decision**: Add to the existing `TaskUsecase.fs` module rather than a new file.

**Rationale**: Follows the established pattern where task-related use-case logic lives in that single module. The function is short and cohesive with `listTasks`.

### 3. Sibling detection by `SourceBacklog` equality
**Decision**: Siblings = all tasks where `SourceBacklog = task.SourceBacklog` excluding `task` itself, across all tasks returned by `ListAllTasks`.

**Rationale**: Simple, correct, and consistent with the domain model. No additional storage required.

## Risks / Trade-offs

- **`TaskNotFound` breaks exhaustive matches** → Compiler will flag all sites; address `formatBacklogError` and any test match expressions. Low risk — the compiler catches it.
- **Plan path must be derived before `getTaskDetail`** → Caller must inline a scan of `allTasks` to get `SourceBacklog` prior to the filesystem check. Slightly more verbose handler code but avoids coupling domain to IO.
