## ADDED Requirements

### Requirement: Select agent harness based on configured protocol
The system SHALL provide an `AgentHarnessSelector` module with a `selectHarness` function that returns the appropriate `IAgentHarness` implementation based on the protocol string from configuration.

#### Scenario: ACP protocol selected
- **WHEN** the configured protocol is `"acp"`
- **THEN** `selectHarness` returns an `AcpAdapter`-backed `IAgentHarness`

#### Scenario: OpenCode HTTP protocol selected
- **WHEN** the configured protocol is `"opencode-http"`
- **THEN** `selectHarness` returns an `OpenCodeAdapter`-backed `IAgentHarness`

#### Scenario: Unknown protocol
- **WHEN** the configured protocol string is not recognised
- **THEN** `selectHarness` returns an error or raises a descriptive exception

### Requirement: AgentHarnessSelector encapsulates all protocol dispatch
All protocol dispatch logic that was previously inline in `handleTaskPlan` SHALL be removed from `Program.fs` and handled solely by `AgentHarnessSelector`.

#### Scenario: handleTaskPlan delegates to selector
- **WHEN** `handleTaskPlan` needs an agent harness
- **THEN** it calls `AgentHarnessSelector.selectHarness` rather than containing inline protocol-dispatch logic
