# mise ITR_BIN — switching between dotnet run and native binary

## Overview

All `mise` tasks that invoke the CLI use the `ITR_BIN` environment variable as a
command prefix. This allows switching between `dotnet run` and the pre-built native
binary by changing a single line in `mise.toml`.

## Configuration

In `mise.toml` under `[env]`, two options are provided — comment one out:

```toml
[env]
# dotnet run (always uses latest source, slower):
ITR_BIN = "dotnet run --no-build --project src/cli/Itr.Cli.fsproj --"

# native binary (fastest, requires prior build):
# ITR_BIN = "src/cli/bin/Debug/net10.0/Itr.Cli"
```

All CLI tasks then reference `$ITR_BIN`:

```toml
[tasks.backlog]
run = "$ITR_BIN backlog"
```

## Trade-offs

| Mode | Speed | Notes |
|---|---|---|
| `dotnet run` (no `--no-build`) | slowest | rebuilds on every invocation |
| `dotnet run --no-build` | moderate | skips build check, still has dotnet startup cost |
| native binary (`Itr.Cli`) | fastest | zero startup overhead; must run `mise run build` after code changes |

## Switching to Release build

To use a Release build instead of Debug:

1. Run `dotnet build -c Release`
2. Update `ITR_BIN` in `mise.toml`:

```toml
ITR_BIN = "src/cli/bin/Release/net10.0/Itr.Cli"
```

## Caveat

When using the native binary, tasks will run stale code if you forget to rebuild
after source changes. Run `mise run build` before switching to binary mode.
