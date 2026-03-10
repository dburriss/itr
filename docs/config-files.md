# Config Files

## itr.json

Defines the **portfolio root**, profiles, and registered products.

```yaml
portfolio:
  default_profile: string

profiles:
  <profile-id>:
    repo_root: path
    git_user: string (optional)
    git_email: string (optional)

products:
  <product-id>:
    path: path
    profile: profile-id (optional)
```

### Field meanings: Portfolio

#### portfolio.default_profile

Default profile used when a product does not specify one.

#### profiles

Defines environment contexts such as `work` or `personal`.

#### profiles.<id>.repo_root

Root directory where repositories live.

Example:

```bash
~/dev/work
~/dev/personal
```

#### products

Registers products known to the portfolio.

#### products.<id>.path

Filesystem path to the product root.

This can be:

```
~/dev/work/billing-system
~/projects/my-library
```

#### products.<id>.profile

Overrides the portfolio default profile.

---

### Example

```yaml
portfolio:
  default_profile: personal

profiles:
  personal:
    repo_root: ~/dev/personal

  work:
    repo_root: ~/dev/work
    git_user: Jane Dev
    git_email: jane@company.com

products:
  billing-system:
    path: ~/dev/work/billing-system
    profile: work

  portfolio-tool:
    path: ~/dev/personal/portfolio-tool
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

Each backlog item is a file:

```
.product/BACKLOG/items/<id>.yaml
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
  - feature-promotion
  - feature-state-machine
```

---

## feature.yaml

Location:

```
.product/FEATURES/active/<feature-id>/feature.yaml
```

Schema:

```yaml
id: string

source:
  backlog: backlog-id

state: planned | in-progress | done

repos:
  - repo-id

repo_status:
  <repo-id>: not-started | in-progress | complete

created_at: date
updated_at: date (optional)
```

---

### Example: feature.yaml

```yaml
id: invoice-retry

source:
  backlog: invoice-retry

state: in-progress

repos:
  - billing-api
  - billing-worker

repo_status:
  billing-api: complete
  billing-worker: in-progress

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

    .product/
      BACKLOG/
        items/
        views/

      FEATURES/
        active/
        archive/
```

---

## Summary of Config Files

| File           | Purpose                                 |
| -------------- | --------------------------------------- |
| itr.json       | portfolio + profiles + product registry |
| product.yaml   | product + repo configuration            |
| backlog item   | planning artifact                       |
| backlog view   | backlog projection                      |
| feature.yaml   | execution coordination                  |

---
