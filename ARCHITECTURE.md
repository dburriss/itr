# Architecture: Coordination System

## System Model

The system is a filesystem-based deterministic workflow layer.

Everything is product-scoped.

Portfolio references products.
Products reference repositories.
Features reference repositories.
Backlog is independent of repo topology.

---

## Directory Structure

.product/
  PRODUCT.md
  ARCHITECTURE.md

  BACKLOG/
    items/
    views/

  FEATURES/
    active/
    archive/

  RELEASES/ (post-MVP)

---

## Product Configuration Schema

product.yaml

id: string
type: single-repo | multi-repo
profile: string
repos:
  - name: string
    path: string
coordination:
  mode: standalone | control-repo | primary-repo
  repo: string (required if primary-repo)
  path: string

---

## Backlog Item Schema

id: string
title: string
type: feature | tech-debt | spike | refactor
status: backlog | rejected
priority: low | medium | high
repos: [string]
summary: string
acceptance_criteria: [string]
constraints: [string]
dependencies: [string]
created_at: date

---

## View Schema

id: string
description: string
items:
  - backlog_item_id

Rules:
- Items must exist.
- No duplicates.
- Optional exclusivity for delivery views.

---

## Feature Schema

feature.yaml

id: string
derived_from: backlog_item_id
state: planned | in-progress | done | archived
owner: string
repos: [string]
branches:
  repo_name: branch_name
merge_status:
  repo_name: not-started | open | merged

Rules:
- Single owner.
- State transitions validated.
- All merge_status must be merged before state=done.

---

## State Transitions

Backlog → Feature(planned)
planned → in-progress
in-progress → done (all repos merged)
done → archived

Invalid transitions fail CI.

---

## CI Validation Rules (MVP)

- Unique backlog IDs.
- Unique feature IDs.
- View items must exist.
- No feature and backlog share same ID simultaneously.
- Single owner per feature.
- Feature repos must exist in product config.
- All branches defined.
- Valid state transitions only.

---

## Scaling Characteristics

Single-repo:

- One repo in product config.

Multi-repo:

- Feature maps to multiple repos.
- Branch mapping required per repo.

Architecture does not change.

---

## Migration Safety

Coordination root is relocatable.
Structure remains identical across modes.