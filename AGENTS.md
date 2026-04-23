# Agent Guidelines

## General

- Prefer simple solutions
- Ask if unsure
- Keep answers concise
- Break solutions into small incremental steps
- Do one step at a time
- Do not move to implementation unless I say the word "implement"
- Use only English

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
- Use TDD when building new features or fixing a bug
- Prefer Acceptance tests for usecase behavior and Communication tests for IO,
  mapping, and formatting contracts
- Acceptance tests should use in-memory test doubles where practical so they
  stay fast and reliable
- Avoid structural assertions on collaborator calls or internal orchestration
  in usecase tests
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
