# Product: Portfolio-Orchestrated Coordination System

## 1. Purpose

Provide a deterministic, machine-readable workflow system for:

- Single-repo libraries
- Multi-repo distributed systems
- Team-based task coordination
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

### Task

Execution unit derived from backlog.
Single owner.
Maps to exactly one repository.
A backlog item may produce one or more tasks (via promotion or split).

---

## 3. Task Lifecycle

1. Backlog item created.
2. Item added to delivery view.
3. Promoted to task (one task per repo).
4. Owner assigned.
5. Plan generated.
6. Plan approved.
7. State → in-progress.
8. Code changes in mapped repo.
9. Branch merged.
10. State → implemented.
11. State → validated.
12. Archived.

---

## 4. MVP Scope

MVP implements the inner loop:

- Product configuration
- Backlog system
- View system
- Task promotion
- Task state machine
- Branch mapping
- CI validation
- Archiving

No adversarial agents.
No automated branch orchestration.
No cross-product dependencies.

---

## 5. Success Criteria

- Works for single and multi-repo products.
- Prevents duplicate task ownership.
- Deterministic state transitions.
- CI-enforced invariants.
- Portable coordination root.