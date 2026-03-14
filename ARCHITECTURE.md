# Architecture: Coordination System Implementation

## 1. Technology Stack

- Language: F#
- CLI parsing: Argu
- Testing: xUnit v3
- Terminal UI: Spectre.Console
- CI: GitHub Actions
- LLM Harness Target: OpenCode

---

## 2. Architectural Style

Stratified design with strict dependency direction.

Layers:

1. Domain
   - Product model
   - Backlog model
   - Task model
   - State machine
   - Validation rules

2. Application
   - Use cases (PromoteFeature, ArchiveFeature, ValidateProduct, etc.)
   - Orchestrates domain operations

3. Adapters
   - Filesystem IO
   - Git interaction
   - YAML serialization
   - Environment/profile resolution

4. Interfaces
   - CLI
   - TUI
   - MCP
   - Server (future)

Dependency rule:
Outer layers depend inward only.
Domain has zero infrastructure dependencies.

---

## 3. Project Structure

Repository layout:

src/
  cli/
  tui/
  mcp/
  server/
  commands/
  adapters/

tests/
  acceptance/
  building/
  communication/

---

## 4. Project Responsibilities

### cli
Command-line entry point.
Uses Argu.
Invokes shared commands.
Follow noun-verb command pattern (e.g. "backlog add", "product init").

### tui
Interactive terminal interface.
Uses Spectre.Console.
Invokes shared commands.

### mcp
Machine control protocol surface.
For LLM-driven automation.

### server
Optional HTTP orchestration surface.
Not required in MVP but structured for extension.

### commands (shared)
Application layer.
Contains use cases.
Shared by CLI, TUI, MCP, Server.

### adapters
Infrastructure layer:
- Filesystem
- YAML serialization
- Git integration
- Profile resolution

No domain logic allowed here.

---

## 5. Shared Command Model

All entry points (CLI, TUI, MCP, Server) call the same command handlers.

Command handler pattern:

- Parse input
- Load product context
- Execute use case
- Persist changes
- Return result model

No entry point contains business logic.

---

## 6. Test Strategy

Three test strata:

### 1. Acceptance Tests
End-to-end behavior.
Filesystem + real YAML.
Validate task lifecycle and state machine.
These define system correctness.

### 2. Building Tests
Low-level, exploratory, disposable.
Used while shaping domain model.
May be removed.

### 3. Communication Tests
Document domain rules and invariants.
Readable specifications.
Example:
- "Cannot mark task implemented unless branch is merged."

---

## 7. Domain Model Constraints

- Feature ID unique.
- Backlog ID unique.
- No ID exists in both backlog and active tasks.
- Feature must have exactly one owner.
- Valid state transitions only.
- All task repos must exist in product config.

State transitions enforced inside domain layer.

---

## 8. GitHub Actions (CI)

CI pipeline:

1. Build solution
2. Run tests
3. Validate sample product fixtures
4. Enforce formatting
5. Optionally validate coordination roots in repository

No deployment pipeline required in MVP.

---

## 9. LLM Harness: OpenCode

Primary automation surface targets OpenCode.

Design implications:

- Deterministic command outputs
- Machine-readable responses
- Explicit error types
- No hidden side effects
- Idempotent operations

CLI commands must support structured output mode (JSON).

---

## 10. Extensibility

Designed to support:

- Adversarial planning agents (future phase)
- Cross-product orchestration
- Release manifests
- Conflict detection

These are layered as additional application services,
not domain rewrites.