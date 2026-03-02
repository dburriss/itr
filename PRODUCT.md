# Product: Portfolio-Orchestrated Product Coordination System

## Purpose

Provide a structured, machine-readable coordination layer that supports:

- Single-repo libraries
- Monorepos
- Multi-repo distributed systems
- Multiple products under a portfolio
- Multiple identity profiles (work/personal)
- Optional coordination storage modes

The system separates:

- Planning (Backlog + Views)
- Execution (Features)
- Architecture constraints
- Repository topology
- Storage location

This product is not a code runtime system.
It is a deterministic workflow layer.

---

## Core Concepts

### Portfolio

A personal aggregation layer referencing multiple products.

### Product

A deployable system consisting of one or more repositories.
Products are the team boundary.

### Profile

Identity + repo resolution configuration (git identity, SSH, root paths).

### Coordination Root

Location where coordination files live:

- standalone
- control-repo
- primary-repo

### Backlog

Canonical pool of candidate work items.

### Views

Projections of backlog items (MVP, phase-1, etc).

### Feature

Execution unit derived from a backlog item.
Single owner.
Maps to one or more repositories.

---

## Non-Goals (MVP Scope)

- No adversarial agent workflow
- No automated merge orchestration
- No distributed transaction management
- No UI
- No mandatory external issue tracker

---

## Functional Requirements (MVP)

1. Define products and their repositories.
2. Support coordination storage modes.
3. Define backlog items (machine-readable).
4. Define delivery views (MVP, phase-1, etc).
5. Promote backlog item → feature.
6. Assign feature owner.
7. Map feature to repositories + branches.
8. Track feature state transitions.
9. Archive completed features.
10. Enforce validation rules via CI.

---

## Feature Lifecycle (MVP)

1. Backlog item created.
2. Added to a delivery view.
3. Promoted to feature.
4. Owner assigned.
5. PLAN.md generated.
6. Feature marked `in-progress`.
7. Work implemented in mapped repos.
8. All PRs merged.
9. Feature marked `done`.
10. Feature moved to archive.

---

## Coordination Modes

- standalone
- control-repo
- primary-repo

Mode is fixed per product.

---

## Backlog and Views

Backlog items are stable and ID-based.
Views reference items by ID.
Ordering is handled per view file.

---

## Release Strategy (Post-MVP)

Release manifests may group completed features.
Not required in MVP.

---

## Success Criteria (MVP)

- Works for single-repo library.
- Works for multi-repo distributed system.
- Prevents duplicate feature ownership.
- Prevents ambiguous backlog ordering.
- Deterministic and CI-validatable.