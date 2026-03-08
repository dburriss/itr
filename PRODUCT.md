# Product: Portfolio-Orchestrated Coordination System

## 1. Purpose

Provide a deterministic, machine-readable workflow system for:

- Single-repo libraries
- Multi-repo distributed systems
- Team-based feature coordination
- Portfolio-level aggregation
- Profile isolation (work/personal)

The system governs:

- Backlog management
- Delivery views (MVP, phase-1, etc.)
- Feature lifecycle
- Repository mapping
- State validation
- Archiving

This product does not execute application runtime logic.
It orchestrates planning and execution metadata.

---

## 2. Core Model

### Portfolio

Personal aggregation of multiple products.

### Product

Team-scoped deployable system consisting of one or more repositories.

### Profile

Identity configuration (git identity, SSH, repo roots).

### Coordination Root

Filesystem location where coordination artifacts live.

Modes:

- standalone
- primary-repo
- control-repo

### Backlog

Stable, ID-based pool of candidate work.

### Views

Explicit projections of backlog items for delivery phases.

### Feature

Execution unit derived from backlog.
Single owner.
Maps to one or more repositories.

---

## 3. Feature Lifecycle

1. Backlog item created.
2. Item added to delivery view.
3. Promoted to feature.
4. Owner assigned.
5. Plan generated.
6. State → in-progress.
7. Code changes across mapped repos.
8. All branches merged.
9. State → done.
10. Archived.

---

## 4. MVP Scope

MVP implements the inner loop:

- Product configuration
- Backlog system
- View system
- Feature promotion
- Feature state machine
- Branch mapping
- CI validation
- Archiving

No adversarial agents.
No automated branch orchestration.
No cross-product dependencies.

---

## 5. Success Criteria

- Works for single and multi-repo products.
- Prevents duplicate feature ownership.
- Deterministic state transitions.
- CI-enforced invariants.
- Portable coordination root.