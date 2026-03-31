# Initialize itr settings

**Task ID:** itr-init
**Backlog Item:** itr-init
**Repo:** itr

## Description

Initialise itr.json with settings, either interactively or with submitted values.

## Scope

<!-- Define the boundaries of this task: what is included and what is explicitly excluded. -->

**Included:**
- A new top-level `init` CLI command
- Interactive mode using Spectre.Console prompts for each setting
- Non-interactive mode with explicit CLI arguments for all settings
- Support for global configuration (`~/.config/itr/itr.json`)
- Support for local configuration (`.itr/itr.json`)
- Settings for profile creation: name, git identity
- Settings for agent protocol configuration (opencode-http, acp)
- Validation of all submitted values
- Confirmation prompt when overwriting existing itr.json
- Product root detection and registration prompt

**Explicitly Excluded:**
- Creating products (handled by `product init`)
- Modifying existing profiles or products (handled by `profiles add`, `product register`)
- The local agent config (`<productRoot>/itr.json`) which is already handled via existing mechanisms
- Any product.yaml creation or modification
- Changing bootstrap behavior (default itr.json creation)

## Steps

<!-- Break the implementation into concrete, ordered steps. -->

1. **Add CLI argument types** to `src/cli/Program.fs`:
   - Create `InitArgs` discriminated union with flags for interactive mode (`--interactive`/`-i`), local mode (`--local`), profile name (`--profile`), git name (`--git-name`), git email (`--git-email`), agent protocol (`--agent-protocol`), agent command (`--agent-command`), agent args (`--agent-args`), and force overwrite (`--force`)
   - Add `Init` to the `CliArgs` union with `CliPrefix.None`
   - Add interface implementation for `IArgParserTemplate`

2. **Create init use-case** in `src/features/Portfolio/PortfolioUsecase.fs`:
   - Add `InitSettingsInput` type: `{ Local: bool; ProfileName: string; GitName: string option; GitEmail: string option; AgentProtocol: string; AgentCommand: string; AgentArgs: string list; Force: bool }`
   - Add `initSettings` function that creates/updates itr.json with profile and agent config
   - Add validation for profile name (slug format), agent protocol (opencode-http or acp)

3. **Create init handler** in `src/cli/Program.fs`:
   - Add `handleInit` function that:
     - Determines config path (global vs local based on `--local` flag)
     - Checks if itr.json exists and prompts for overwrite confirmation if `--force` not set
     - Detects if current directory is within a product root and prompts for registration
     - Validates all submitted values
     - Runs the init use-case
     - Prints success message

4. **Add dispatch logic** in `Program.fs`:
   - Add case for `Init` command in the main dispatch function
   - Wire up `handleInit` to be called when `itr init` is invoked

5. **Add validation helpers**:
   - Add `validateAgentProtocol` function that checks for "opencode-http" or "acp"
   - Update error formatting in `formatPortfolioError` to handle new error variants

6. **Add unit tests**:
   - Test init use-case with all valid combinations
   - Test validation errors for invalid profile names
   - Test validation errors for invalid agent protocols
   - Test idempotency when config already exists with `--force`
   - Test local vs global path resolution

7. **Add acceptance tests**:
   - Test `itr init --local` creates `.itr/itr.json`
   - Test `itr init` creates `~/.config/itr/itr.json`
   - Test interactive flow prompts for required fields
   - Test overwrite confirmation when itr.json exists

## Dependencies

- none

## Acceptance Criteria

- A command initializes itr.json with provided settings when values are submitted
- A command interactively prompts for settings and initializes itr.json when no values are submitted if --interactive flag is provided
- The command validates submitted settings and returns clear errors for invalid input
- The command does not overwrite existing itr.json without confirmation
- The command detects if in an existing product root and prompts to register that product root directory if it is not already registered
- The command can create a local settings file or a global settings file based on user choice
- The command creates itr.json in ~/.config/itr/itr.json for global settings
- The command creates itr.json in ./.itr/itr.json for local settings (--local flag)
- The command supports initializing settings for the agent protocol (opencode-http or acp)

## Impact

<!-- Describe the impact on the system: files changed, interfaces affected, data migrations, etc. -->

**Files Changed:**
- `src/cli/Program.fs` - Add InitArgs, handleInit, and dispatch case
- `src/features/Portfolio/PortfolioUsecase.fs` - Add initSettings use-case function
- `src/domain/Domain.fs` - Potentially add new error variants if needed
- `tests/acceptance/PortfolioAcceptanceTests.fs` - Add acceptance tests

**Interfaces Affected:**
- `IPortfolioConfig` interface remains unchanged
- `IPortfolioDeps` remains unchanged
- No changes to existing data storage formats

**New Commands:**
- `itr init` - Global initialization (creates/updates `~/.config/itr/itr.json`)
- `itr init --local` - Local initialization (creates/updates `.itr/itr.json`)

**Data Migrations:**
- None - this is a new feature that creates new configuration files
- Existing itr.json files are read-compatible

## Risks

<!-- List potential risks and mitigations. -->

1. **Risk:** Confusing the local itr.json (`.itr/itr.json`) with existing local agent config (`<productRoot>/itr.json`)
   - **Mitigation:** Document clearly that `--local` creates `.itr/itr.json` for portfolio settings, while the existing `<productRoot>/itr.json` is for per-product agent overrides

2. **Risk:** Overwriting existing itr.json accidentally
   - **Mitigation:** Require `--force` flag or interactive confirmation before overwriting

3. **Risk:** Profile name conflicts when initializing in a directory that already has a profile
   - **Mitigation:** Check for existing profiles and warn/merge rather than replace

4. **Risk:** Interactive mode in non-TTY environments
   - **Mitigation:** Require explicit CLI arguments when input is redirected (follow pattern from InteractivePrompts.fs)

## Open Questions

<!-- List any open questions that need to be resolved before or during implementation. -->

1. Should `itr init` require at least one profile, or can itr.json be initialized with just agent settings and profiles added later via `profiles add`?

2. When creating a local config (`.itr/itr.json`), should it inherit settings from the global config as defaults, or be completely standalone?

3. What should be the default profile name when not specified interactively? (e.g., "default", or prompt for one?)

4. Should `itr init` also create a default product.yaml when run in a fresh directory, similar to `product init`? (Current scope says no, but worth confirming)

5. How should the product root detection work? Walk up from current directory looking for product.yaml, or require explicit path argument?