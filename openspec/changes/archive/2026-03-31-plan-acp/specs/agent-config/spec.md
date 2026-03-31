## ADDED Requirements

### Requirement: Agent config schema in profile
The global portfolio config profile SHALL support an optional `agent` section containing `protocol` (string), `command` (string), and `args` (string array). If absent, the system SHALL default to `{ protocol: "opencode-http", command: "opencode", args: [] }`.

#### Scenario: Agent config loaded from profile
- **WHEN** `itr.json` contains `"agent": { "protocol": "acp", "command": "opencode", "args": [] }` in a profile
- **THEN** the loaded profile has `AgentConfig` with `Protocol = "acp"`, `Command = "opencode"`, `Args = []`

#### Scenario: Missing agent section uses defaults
- **WHEN** a profile in `itr.json` has no `agent` section
- **THEN** the loaded profile has `AgentConfig` with `Protocol = "opencode-http"`, `Command = "opencode"`, `Args = []`

#### Scenario: Agent config round-trips losslessly
- **WHEN** a portfolio with an `agent` section is saved and reloaded
- **THEN** all `AgentConfig` fields are preserved exactly

### Requirement: Local project agent config override
The system SHALL read an optional `<productRoot>/itr.json` file containing an `agent` section. When present, its `agent` values SHALL override the global profile agent config. When absent, the global profile agent config is used unchanged.

#### Scenario: Local config overrides global protocol
- **WHEN** a local `itr.json` at the product root contains `"agent": { "protocol": "acp", "command": "my-agent", "args": [] }`
- **AND** the global profile has `{ "protocol": "opencode-http", ... }`
- **THEN** the resolved agent config has `Protocol = "acp"` and `Command = "my-agent"`

#### Scenario: No local config uses global
- **WHEN** no `itr.json` exists at the product root
- **THEN** the resolved agent config is taken from the global profile unchanged

#### Scenario: Local config with missing fields uses global defaults
- **WHEN** the local `itr.json` exists but contains no `agent` section
- **THEN** the resolved agent config is taken from the global profile unchanged
