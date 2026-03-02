# Agent Guidelines

## General

- Prefer simple solutions
- Ask if unsure
- Keep answers concise
- Break solutions into small incremental steps
- Do one step at a time

## Tech stack

- .NET 10
- C# / F#
- Entity Framework Core
- xUnit for testing

## Build and test

- Run build and test before and after every change
- Build: `dotnet build`
- Test: `dotnet test`
- Clean: `dotnet clean`

## Structure

This is a .NET web application with the following structure:

- `src/` - Main source code
- `tests/` - Test projects
- `docs/` - Documentation