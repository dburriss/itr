# Path Construction for Backlog and Task Files

## Problem

Path construction for backlog and task files is currently duplicated inline across
`YamlAdapter.fs` and `Program.fs` using raw `Path.Combine` calls. The same paths
will also be needed when passing plan files to an ACP agent harness, and as a
path column in `--output text` for television channel editor integration.

## ACP Requirement

The [ACP protocol spec](https://agentclientprotocol.com/protocol/overview#argument-requirements)
requires all file paths to be **absolute**. Since `coordRoot` is already
`CoordinationRoot.AbsolutePath` (an absolute path), all constructed paths are
absolute by composition. No relative path variant is needed.

## Decision

Centralise path construction as `[<RequireQualifiedAccess>]` modules in `Domain.fs`,
placed immediately after the type each module corresponds to. This follows the
existing convention (`BacklogId`, `TaskId`, `BacklogItemType`, etc.).

`System.IO.Path.Combine` is used directly — path construction is pure string
manipulation with no I/O, so it does not go through `IFileSystem`. Testably
abstractions only cover file read/write/exists operations.

## New modules

### `module BacklogItem` (after `BacklogItem` type)

```fsharp
[<RequireQualifiedAccess>]
module BacklogItem =
    let itemFile (coordRoot: string) (id: BacklogId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value id, "item.yaml")

    let itemDir (coordRoot: string) (id: BacklogId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value id)
```

### `module ItrTask` (after `ItrTask` type)

```fsharp
[<RequireQualifiedAccess>]
module ItrTask =
    let taskFile (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId, "tasks", TaskId.value taskId, "task.yaml")

    let planFile (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId, "tasks", TaskId.value taskId, "plan.md")

    let taskDir (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId, "tasks", TaskId.value taskId)
```

`itemDir` and `taskDir` are needed by the adapter for archive move operations.
`planFile` is needed by `handleTaskPlan` and future ACP plan-passing.

## Call sites to replace

### `src/adapters/YamlAdapter.fs`

| Operation | Old | New |
|---|---|---|
| `LoadBacklogItem` | `Path.Combine(coordRoot, "BACKLOG", id, "item.yaml")` | `BacklogItem.itemFile coordRoot backlogId` |
| `BacklogItemExists` | `Path.Combine(coordRoot, "BACKLOG", id, "item.yaml")` | `BacklogItem.itemFile coordRoot backlogId` |
| `WriteBacklogItem` | `Path.Combine(coordRoot, "BACKLOG", id, "item.yaml")` | `BacklogItem.itemFile coordRoot backlogId` |
| `ArchiveBacklogItem` source dir | `Path.Combine(coordRoot, "BACKLOG", id)` | `BacklogItem.itemDir coordRoot backlogId` |
| `ListTasks` task.yaml | `Path.Combine(coordRoot, "BACKLOG", ..., "task.yaml")` | `ItrTask.taskFile coordRoot backlogId taskId` |
| `WriteTask` | `Path.Combine(coordRoot, "BACKLOG", ..., "task.yaml")` | `ItrTask.taskFile coordRoot backlogId taskId` |
| `ArchiveTask` dir | `Path.Combine(coordRoot, "BACKLOG", ..., taskId)` | `ItrTask.taskDir coordRoot backlogId taskId` |

### `src/cli/Program.fs`

| Handler | Old | New |
|---|---|---|
| `handleTaskInfo` plan path | inline `Path.Combine` | `ItrTask.planFile coordRoot backlogId taskId` |
| `handleTaskPlan` plan write path | inline `Path.Combine` | `ItrTask.planFile coordRoot backlogId taskId` |
| `handleTaskApprove` plan exists check | inline `Path.Combine` | `ItrTask.planFile coordRoot backlogId taskId` |

## Path column in `--output text`

Append the absolute path as the **last column** so existing `{split:\t:N}` indices
in television channel definitions are unaffected.

**`backlog list --output text`** — 9 columns:
```
id\ttype\tpriority\tstatus\tview\ttaskCount\tcreatedAt\ttitle\tpath
```
Path value: `BacklogItem.itemFile coordRoot item.Id`

**`task list --output text`** — 6 columns:
```
taskId\tbacklogId\trepoId\tstate\thasAiPlan\tpath
```
Path value: `ItrTask.taskFile coordRoot task.SourceBacklog task.Id`

## Television editor action pattern

With the path column available, a channel can open the item in `$EDITOR`:

```toml
[keybindings]
ctrl-e = "actions:edit"

[actions.edit]
description = "Open in editor"
command = "${EDITOR:-vim} '{split:\t:8}'"   # col 8 = path (backlog)
shell = "bash"
mode = "execute"
```

For tasks, use `{split:\t:5}` (col 5 = path).
