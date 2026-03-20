# Config Files

## itr.json

Defines the global portfolio config, profiles, and each profile's registered products.

```json
{
  "defaultProfile": "work",
  "profiles": {
    "work": {
      "products": [
        {
          "id": "billing-system",
          "root": {
            "mode": "primary-repo",
            "repoDir": "~/dev/work/billing-system"
          }
        }
      ],
      "gitIdentity": {
        "name": "Jane Dev",
        "email": "jane@company.com"
      }
    }
  }
}
```

### Field meanings: Portfolio

#### defaultProfile

Optional default profile used when no `--profile` flag or `ITR_PROFILE` environment variable is provided.

#### profiles

Required object keyed by profile name, such as `work` or `personal`. This object may be empty.

#### profiles.<id>.products

List of products registered under that profile.

#### profiles.<id>.products[].id

Product identifier. Must match the slug pattern `[a-z0-9][a-z0-9-]*`.

#### profiles.<id>.products[].root

Coordination root configuration for the product.

#### profiles.<id>.products[].root.mode

Supported values:

```
standalone
primary-repo
control-repo
```

#### profiles.<id>.products[].root.dir

Used only when `mode` is `standalone`.

#### profiles.<id>.products[].root.repoDir

Used when `mode` is `primary-repo` or `control-repo`.

#### profiles.<id>.gitIdentity

Optional git identity override for work done under that profile.

#### profiles.<id>.gitIdentity.name

Display name for git commits.

#### profiles.<id>.gitIdentity.email

Optional email for git commits.

---

### Example

```json
{
  "defaultProfile": "personal",
  "profiles": {
    "personal": {
      "products": [
        {
          "id": "portfolio-tool",
          "root": {
            "mode": "standalone",
            "dir": "~/dev/personal/portfolio-tool"
          }
        }
      ]
    },
    "work": {
      "products": [
        {
          "id": "billing-system",
          "root": {
            "mode": "primary-repo",
            "repoDir": "~/dev/work/billing-system"
          }
        },
        {
          "id": "infra-control",
          "root": {
            "mode": "control-repo",
            "repoDir": "~/dev/work/infra-control"
          }
        }
      ],
      "gitIdentity": {
        "name": "Jane Dev",
        "email": "jane@company.com"
      }
    }
  }
}
```

---

## product.yaml

Defines a **product and its repositories**.

```yaml
id: string

profile: profile-id (optional)

docs:
  product: path (optional)
  architecture: path (optional)

repos:
  <repo-id>:
    path: path
    url: git-url (optional)
    default_branch: string (optional)

coordination:
  mode: standalone | primary-repo | control-repo
  repo: repo-id (required for primary-repo)
  path: path
```

---

### Field meanings

#### id

Product identifier.

Must match the portfolio registration.

---

#### profile

Optional override of portfolio profile.

---

#### docs

Optional references to product-level documentation files.

#### docs.product

Path to the primary product definition document.

Usually:

```
PRODUCT.md
```

---

#### docs.architecture

Path to the system architecture document.

Usually:

```
ARCHITECTURE.md
```

---

#### repos

Logical repo IDs mapped to local paths.

#### repos.<repo-id>.path

Path relative to product root.

Example:

```
billing-api
billing-worker
```

---

#### repos.<repo-id>.url

Optional remote repository.

Used for validation or automation.

---

#### repos.<repo-id>.default_branch

Default branch name.

Example:

```
main
develop
```

---

#### coordination.mode

Controls where coordination files live.

Options:

```
standalone
primary-repo
control-repo
```

---

#### coordination.repo

Required if mode is `primary-repo`.

Specifies which repo hosts `.product`.

---

#### coordination.path

Directory inside repo or filesystem containing coordination files.

Usually:

```
.product
```

---

### Example: product.yaml

```yaml
id: billing-system

profile: work

docs:
  product: PRODUCT.md
  architecture: ARCHITECTURE.md

repos:
  billing-api:
    path: billing-api
    url: git@github.com:company/billing-api.git
    default_branch: main

  billing-worker:
    path: billing-worker
    url: git@github.com:company/billing-worker.git

coordination:
  mode: primary-repo
  repo: billing-api
  path: .product
```

---

## backlog item

Each backlog item is a directory:

```
.product/BACKLOG/<id>/item.yaml
```

Schema:

```yaml
id: string

title: string

type: feature | tech-debt | spike | refactor

priority: low | medium | high (optional)

repos:
  - repo-id

summary: string

acceptance_criteria:
  - string

constraints:
  - string (optional)

dependencies:
  - backlog-id (optional)

created_at: date
```

---

### Example: backlog item

```yaml
id: invoice-retry

title: Retry failed invoice processing

type: feature

priority: high

repos:
  - billing-api
  - billing-worker

summary: >
  Retry transient failures during invoice processing.

acceptance_criteria:
  - Failed invoices retried up to 5 times
  - Retry state persisted
  - Metrics emitted

created_at: 2026-03-08
```

---

## backlog view

Location:

```
.product/BACKLOG/views/<view>.yaml
```

Schema:

```yaml
id: string

description: string (optional)

items:
  - backlog-id
```

---

### Example: backlog view

```yaml
id: mvp

description: Initial product capability

items:
  - product-config
  - backlog-system
  - task-promotion
  - task-state-machine
```

---

## task.yaml

Location:

```
.itr/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml
```

Schema:

```yaml
id: string

source:
  backlog: backlog-id

repo: repo-id

state: planning | planned | approved | in-progress | implemented | validated | archived

created_at: date
updated_at: date (optional)

depends_on:
  - task-id (optional)
```

---

### Example: tasks.yaml

```yaml
id: invoice-retry

source:
  backlog: invoice-retry

repo: billing-api

state: in-progress

created_at: 2026-03-08
```

---

## Directory Layout (for context)

```
itr.json

products/
  billing-system/
    product.yaml

    billing-api/
    billing-worker/

    .itr/
      BACKLOG/
        <backlog-id>/
          item.yaml
          tasks/
            <task-id>/
              task.yaml
              plan.md
            <date>-<task-id>/      (completed task)
              task.yaml
              plan.md
        archive/
          <date>-<backlog-id>/     (archived backlog item)
            item.yaml
            tasks/
              <date>-<task-id>/
                task.yaml
                plan.md
        views/
```

---

## Summary of Config Files

| File           | Purpose                                 |
| -------------- | --------------------------------------- |
| itr.json       | portfolio + profiles + per-profile products |
| product.yaml   | product + repo configuration            |
| backlog item   | planning artifact                       |
| backlog view   | backlog projection                      |
| tasks.yaml     | execution coordination                  |

---
