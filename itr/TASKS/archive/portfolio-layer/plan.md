# Portfolio Layer — Implementation Plan

## Decisions captured

| Decision | Choice |
|---|---|
| Domain project | New `Itr.Domain` project |
| Config format | JSON |
| Profile selection | `ITR_PROFILE` env var + `--profile` CLI flag (flag wins) |
| Portfolio config location | `~/.config/itr/portfolio.json` + `ITR_HOME` override |
| Coordination root modes | All three: `standalone`, `primary-repo`, `control-repo` |

---

## 1. Project structure changes

A new `Itr.Domain` project is introduced as the innermost layer. All existing projects shift one layer outward.

```
src/
  domain/          ← NEW (Itr.Domain.fsproj)
    Domain.fs
  commands/        ← depends on Itr.Domain (new)
    Library.fs
  adapters/        ← depends on Itr.Domain + Itr.Commands
    Library.fs
  cli/ tui/ mcp/ server/  ← unchanged
```

Updated dependency graph:

```
Itr.Domain  (no dependencies)
     ^
Itr.Commands  → Itr.Domain
     ^
Itr.Adapters  → Itr.Commands + Itr.Domain
     ^
Cli / Tui / Mcp / Server  → Itr.Commands + Itr.Adapters
```

---

## 2. Domain model (`Itr.Domain` — `Domain.fs`)

```fsharp
// === Core identity types ===
type ProfileName   = ProfileName of string
type ProductId     = ProductId of string          // slug, e.g. "my-lib"
type RepoPath      = RepoPath of string           // absolute filesystem path

// === Coordination root ===
type CoordinationMode =
    | Standalone                                  // product dir IS the root; .itr/ sits directly inside
    | PrimaryRepo                                 // .itr/ lives inside the product's primary git repo
    | ControlRepo                                 // .itr/ lives in a dedicated coordination-only repo

type CoordinationRoot = {
    Mode : CoordinationMode
    AbsolutePath : string                         // resolved, validated absolute path to the .itr/ dir
}

// === Product reference ===
// Minimal navigational handle stored in portfolio config.
// Full product model resolved separately from the coordination root.
type ProductRef = {
    Id          : ProductId
    DisplayName : string
    CoordRoot   : CoordinationRootConfig          // raw config before path resolution
}

// Raw config shape (before filesystem resolution)
and CoordinationRootConfig =
    | StandaloneConfig  of dir: string            // explicit dir; expands ~ and env vars
    | PrimaryRepoConfig of repoDir: string        // path to the git repo root
    | ControlRepoConfig of repoDir: string        // path to the coordination-only repo root

// === Profile ===
type Profile = {
    Name        : ProfileName
    Products    : ProductRef list
    GitIdentity : GitIdentity option
    RepoRoots   : string list                     // scan roots for discovery (future)
}

and GitIdentity = {
    Name  : string
    Email : string
}

// === Portfolio ===
type Portfolio = {
    DefaultProfile : ProfileName option
    Profiles       : Map<ProfileName, Profile>
}

// === Resolution outputs ===
type ResolvedProduct = {
    Ref             : ProductRef
    CoordRoot       : CoordinationRoot            // validated, absolute path
}

// === Errors ===
type PortfolioError =
    | ConfigNotFound      of path: string
    | ConfigParseError    of path: string * message: string
    | ProfileNotFound     of ProfileName
    | ProductNotFound     of ProductId
    | CoordRootNotFound   of ProductId * resolvedPath: string
    | AmbiguousItrDir     of ProductId * resolvedPath: string   // .itr/ dir missing inside repo
    | InvalidProductId    of string * reason: string
```

**Invariants enforced in domain:**

- `ProductId` must match `[a-z0-9][a-z0-9\-]*` (slug pattern)
- Profile names are case-insensitive for lookup but stored as-given
- A `Portfolio` with no profiles is valid (empty state)
- `ResolvedProduct.CoordRoot.AbsolutePath` must end with `.itr` and that directory must exist

---

## 3. Configuration schema (JSON)

### `~/.config/itr/portfolio.json`

```json
{
  "defaultProfile": "work",
  "profiles": {
    "work": {
      "products": [
        {
          "id": "platform-api",
          "displayName": "Platform API",
          "root": {
            "mode": "primary-repo",
            "repoDir": "~/work/repos/platform-api"
          }
        },
        {
          "id": "infra-control",
          "displayName": "Infra Control",
          "root": {
            "mode": "control-repo",
            "repoDir": "~/work/repos/infra-coordination"
          }
        }
      ],
      "gitIdentity": {
        "name": "Devon Burriss",
        "email": "devon@work.example"
      }
    },
    "personal": {
      "products": [
        {
          "id": "my-lib",
          "displayName": "My Library",
          "root": {
            "mode": "standalone",
            "dir": "~/personal/my-lib"
          }
        }
      ]
    }
  }
}
```

**Schema rules:**

| Field | Required | Notes |
|---|---|---|
| `defaultProfile` | No | If absent, profile must be specified per-command |
| `profiles` | Yes | At least one profile required for non-trivial use |
| `profile.products[].id` | Yes | Slug; validated against `[a-z0-9][a-z0-9\-]*` |
| `profile.products[].displayName` | No | Defaults to `id` |
| `profile.products[].root.mode` | Yes | `"standalone"`, `"primary-repo"`, `"control-repo"` |
| `profile.products[].root.dir` | For standalone | Absolute or `~`-relative |
| `profile.products[].root.repoDir` | For primary/control | Absolute or `~`-relative |
| `gitIdentity` | No | Profile-level identity override |

---

## 4. Filesystem layout

### Portfolio config location resolution

```
1. ITR_HOME env var set?  → use $ITR_HOME/portfolio.json
2. Otherwise             → ~/.config/itr/portfolio.json
```

### `.itr/` directory inside a coordination root

```
<coord-root>/
  .itr/
    product.json       ← product definition (id, repos, display name)
    backlog/
      items.json
    views/
      <view-name>.json
    features/
      active/
        <feature-id>.json
      archived/
        <feature-id>.json
```

### Mode-specific root resolution

| Mode | Config field | `.itr/` expected at |
|---|---|---|
| `standalone` | `dir` | `<dir>/.itr/` |
| `primary-repo` | `repoDir` | `<repoDir>/.itr/` |
| `control-repo` | `repoDir` | `<repoDir>/.itr/` |

All three modes resolve identically at the filesystem level (`<root>/.itr/`). The `mode` value is semantic — it communicates intent and enables future tooling (e.g., git clone detection) but does not change resolution logic in MVP.

---

## 5. Resolution rules

### Step 1: Locate portfolio config

```
configPath =
    env ITR_HOME  → $ITR_HOME/portfolio.json
    otherwise     → ~/.config/itr/portfolio.json

if file does not exist → PortfolioError.ConfigNotFound configPath
```

### Step 2: Parse and validate

```
parse JSON → Portfolio
if parse fails → PortfolioError.ConfigParseError(path, message)
validate all ProductId slugs → PortfolioError.InvalidProductId if any fail
```

### Step 3: Select active profile

```
activeProfileName =
    --profile flag                    (highest precedence)
    ITR_PROFILE env var
    portfolio.defaultProfile          (lowest precedence)
    none → PortfolioError.ProfileNotFound(ProfileName "")  [no profile resolved]

profile = Map.tryFind activeProfileName portfolio.Profiles
if None → PortfolioError.ProfileNotFound activeProfileName
```

### Step 4: Resolve a product's coordination root

```
for a given ProductId:
  productRef = List.tryFind (fun p -> p.Id = id) profile.Products
  if None → PortfolioError.ProductNotFound id

  resolvedDir =
    match productRef.CoordRoot with
    | StandaloneConfig dir  → expand(dir)
    | PrimaryRepoConfig dir → expand(dir)
    | ControlRepoConfig dir → expand(dir)

  itrPath = Path.Combine(resolvedDir, ".itr")

  if not (Directory.Exists itrPath)
    → PortfolioError.CoordRootNotFound(id, itrPath)

  ResolvedProduct { Ref = productRef; CoordRoot = { Mode = ...; AbsolutePath = itrPath } }
```

Path expansion rules: `~` → `$HOME`, environment variables expanded via `Environment.ExpandEnvironmentVariables`.

---

## 6. Application layer (`Itr.Commands`)

Three use cases for MVP:

### `Portfolio.loadPortfolio`

```fsharp
// Input: config path override option
// Output: Result<Portfolio, PortfolioError>
val loadPortfolio : configPath: string option -> Result<Portfolio, PortfolioError>
```

### `Portfolio.resolveActiveProfile`

```fsharp
// Input: portfolio, profile name option (from CLI flag), env var reader
// Output: Result<Profile, PortfolioError>
val resolveActiveProfile :
    portfolio:    Portfolio ->
    flagProfile:  string option ->
    readEnv:      (string -> string option) ->
    Result<Profile, PortfolioError>
```

The `readEnv` parameter is injected to keep the function pure and testable.

### `Portfolio.resolveProduct`

```fsharp
// Input: profile, product id string, dir existence checker
// Output: Result<ResolvedProduct, PortfolioError>
val resolveProduct :
    profile:    Profile ->
    productId:  string ->
    dirExists:  (string -> bool) ->
    Result<ResolvedProduct, PortfolioError>
```

`dirExists` is injected for testability (real: `Directory.Exists`; test: stub).

### Command pipeline pattern

Every command that operates on a product follows this pipeline:

```fsharp
loadPortfolio configOverride
>>= resolveActiveProfile flagProfile readEnv
>>= fun profile -> resolveProduct profile productId dirExists
>>= fun resolvedProduct -> executeProductCommand resolvedProduct ...
```

All entry points (CLI, TUI, MCP, Server) call the same pipeline. No entry point duplicates resolution logic.

---

## 7. Adapter layer (`Itr.Adapters`)

Two responsibilities:

### `PortfolioAdapter.readConfig`

```fsharp
// Reads and deserializes portfolio.json from disk
// Returns raw JSON → parsed Portfolio via domain deserialization
val readConfig : path: string -> Result<Portfolio, PortfolioError>
```

- Uses `System.Text.Json` (no extra package needed; already in .NET 10)
- JSON property names use `camelCase`
- Unknown fields ignored (forward-compatible)

### `PortfolioAdapter.configPath`

```fsharp
// Resolves config file path from environment
val configPath : unit -> string
```

Encapsulates the `ITR_HOME` → `~/.config/itr/portfolio.json` resolution.

### Real I/O functions to inject

```fsharp
module Env =
    let readVar (name: string) : string option =
        let v = System.Environment.GetEnvironmentVariable(name)
        if System.String.IsNullOrEmpty(v) then None else Some v

module Fs =
    let dirExists (path: string) : bool =
        System.IO.Directory.Exists(path)
```

These are the concrete implementations injected at the interface layer, never inside domain or application modules.

---

## 8. Interface layer integration

### CLI (`Itr.Cli` — Argu)

Add a global `--profile` argument to the top-level Argu parser:

```fsharp
type GlobalArgs =
    | [<AltCommandLine("-p")>] Profile of string
    interface IArgParserTemplate with ...
```

Profile flag is parsed before dispatching to any subcommand. It is threaded through to `resolveActiveProfile`.

Output mode: add `--output json` global flag to enable machine-readable output for all commands (requirement from `ARCHITECTURE.md §9`).

### MCP (`Itr.Mcp`)

Each MCP tool receives `profile` as an optional parameter. Passed directly to `resolveActiveProfile` as `flagProfile`. No MCP-specific resolution logic.

### TUI (`Itr.Tui`)

Profile selection can be interactive (Spectre.Console prompt) if `ITR_PROFILE` is unset and `--profile` not given, rather than returning an error. This is the only place non-deterministic UX is acceptable.

---

## 9. Error handling rules

| Error | Behaviour |
|---|---|
| `ConfigNotFound` | Fatal. Print path. Suggest `itr init`. Exit 1. |
| `ConfigParseError` | Fatal. Print path + message. Exit 1. |
| `ProfileNotFound` | Fatal. List available profiles. Exit 1. |
| `ProductNotFound` | Fatal. List products in active profile. Exit 1. |
| `CoordRootNotFound` | Fatal. Print expected path. Suggest `itr product init`. Exit 1. |
| `InvalidProductId` | Fatal at parse time. Print invalid value + slug rules. Exit 1. |

In JSON output mode (`--output json`), all errors are serialized as:

```json
{ "ok": false, "error": "ProfileNotFound", "detail": "Profile 'staging' not found. Available: work, personal" }
```

Successful results:

```json
{ "ok": true, "data": { ... } }
```

---

## 10. Validation rules summary

| Rule | Where enforced |
|---|---|
| ProductId matches `[a-z0-9][a-z0-9\-]*` | Domain layer (smart constructor) |
| No duplicate ProductId within a profile | Domain layer (portfolio parse) |
| No duplicate profile names | Domain layer (portfolio parse) |
| CoordRoot path must exist | Adapter layer (injected `dirExists`) |
| Profile must resolve (env or flag or default) | Application layer |
| JSON schema unknown fields ignored | Adapter layer |
| `~` and env vars in paths expanded | Adapter layer (never domain) |

---

## 11. File-by-file implementation checklist

```
src/domain/
  Domain.fs                ← all types above; no I/O

src/commands/
  Portfolio.fs             ← loadPortfolio, resolveActiveProfile, resolveProduct
  Library.fs               ← re-export / placeholder (keep existing)

src/adapters/
  PortfolioAdapter.fs      ← readConfig, configPath, Env, Fs modules
  Library.fs               ← re-export / placeholder (keep existing)

src/cli/
  Program.fs               ← add GlobalArgs with --profile; wire pipeline

tests/communication/
  PortfolioDomainTests.fs  ← slug validation, duplicate product id, profile resolution rules

tests/acceptance/
  PortfolioAcceptanceTests.fs  ← temp dir fixtures, real JSON, real Directory.Exists
```

New `.fsproj` files:

- `src/domain/Itr.Domain.fsproj` — no project references, no packages
- `src/commands/Itr.Commands.fsproj` — add `<ProjectReference>` to `Itr.Domain`

---

## 12. Out of scope for this plan

- Product-level backlog, feature, and view operations (separate layer, build on top of this)
- `itr init` / `itr product init` scaffolding commands
- Git interaction (clone, branch detection for `control-repo` validation)
- Profile-level `repoRoots` discovery scanning
- Cross-product aggregation
