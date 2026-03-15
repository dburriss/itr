# Agent Guidelines

## General

- Prefer simple solutions
- Ask if unsure
- Keep answers concise
- Break solutions into small incremental steps
- Do one step at a time
- Do not move to implementation unless I say the word "implement"

## Tech stack

- .NET 10
- F#
- xUnit for testing
- GitHub Actions for CI
- Spectre.Console for TUI
- Argu for CLI parsing
- OpenCode for LLM harness
- YAML for human-manageable configuration
- `simple-exec` for shell commands
- `Testably.Abstractions` for testable filesystem and process interactions

## Tooling

- Mise for tooling dependencies, tasks, and secret management
- [Worktrunk](https://worktrunk.dev/) for branch management
- [Git AI](https://usegitai.com/) for AI-assisted commit tracking

## Build and test

- Run build and test before and after every change
- Build: `dotnet build`
- Test: `dotnet test`
- Clean: `dotnet clean`
- Verify (pre-push): `mise run verify`
- Format: `mise run format`

## Structure

This is a .NET web application with the following structure:

- `src/` - Main source code
- `tests/` - Test projects
- `docs/` - Documentation