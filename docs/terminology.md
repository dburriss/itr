# Terminology

A glossary of terms used across `itr`. For state machines and transition rules, see [lifecycles.md](lifecycles.md). For config file schemas, see [config-files.md](config-files.md).

---

## Profile + Portfolio

This is the machine-level setup phase. Before any product work can begin, a portfolio config must exist and at least one profile must be registered. Commands: `itr settings bootstrap`, `itr profile add`.

- **portfolio** — the global collection of profiles on the local machine, stored in `itr.json`. The portfolio is the top-level config resolved on every command invocation.

- **profile** — a named context (e.g. `work`, `personal`) that groups one or more product registrations and an optional git identity. The active profile is resolved from the `--profile` flag, the `ITR_PROFILE` env var, or the `defaultProfile` field in `itr.json`.

- **defaultProfile** — the fallback profile name used when no flag or env var is set.

- **gitIdentity** — an optional per-profile name and email pair applied to git commits made under that profile.

---

## Product

This is the product initialisation phase. A product defines the repos, docs, and coordination layout for a unit of deliverable work. Commands: `itr product init`, `itr product register`.

- **product** — a unit of work composed of one or more repos, described by `product.yaml`. The product is the central context for all backlog and task operations.

- **product root** — the directory containing `product.yaml`. Registered in `itr.json` under a profile.

- **repo** — a logical repository scoped to a product. Each repo has an id, a path relative to the product root, and an optional remote URL and default branch.

- **coordination** — the `.itr/` directory where backlog items, tasks, and views are stored. Its location is determined by the coordination mode.

- **coordination mode** — controls where `.itr/` lives relative to the product root. Options:
  - `standalone` — `.itr/` sits directly under the product root.
  - `primary-repo` — `.itr/` lives inside one of the product's repos.
  - `control-repo` — `.itr/` lives inside a dedicated control repo.

---

## Backlog

This is the planning phase. Backlog items capture units of planned work before execution begins. Commands: `itr backlog add`, `itr backlog take`, `itr backlog view`.

- **backlog item** — a unit of planned work described by `item.yaml`. Contains a title, type, priority, repo scope, summary, acceptance criteria, and optional constraints and dependencies.

- **backlog item type** — classifies the nature of the work. Options: `feature`, `tech-debt`, `spike`, `refactor`.

- **backlog item priority** — optional urgency indicator. Options: `low`, `medium`, `high`.

- **backlog item state** — a calculated value derived from the tasks linked to the item. Not stored in `item.yaml`. See [lifecycles.md](lifecycles.md) for the full derivation rules.

- **backlog view** — an ordered projection of backlog item ids for a particular goal or milestone, stored in `.itr/BACKLOG/views/<view>.yaml`. Views do not affect item state.

---

## Task

This is the execution phase. Tasks are created from backlog items and track implementation progress per repo. Commands: `itr task plan`, `itr task approve`, `itr task start`, `itr task done`, `itr task validate`, `itr task archive`.

- **task** — a scoped execution unit created when a backlog item is taken via `backlog take`. One task is created per repo listed on the backlog item.

- **task state** — stored in `task.yaml`. Tracks where a task is in the execution lifecycle from `planning` through to `archived`. See [lifecycles.md](lifecycles.md) for all states and valid transitions.

- **plan artifact** — the `plan.md` file created or updated by `task plan`. Its presence advances a task from `planning` to `planned`.

- **task archive** — the act of moving a completed task folder from `<task-id>/` to `<date>-<task-id>/`. Marks the task as a historical record. A task must be in `validated` state before it can be archived.
