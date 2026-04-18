# Architecture: Coordination System Implementation

## 1. Technology Stack

- Language: F#
- CLI parsing: Argu
- Testing: xUnit v3
- Terminal UI: Spectre.Console
- CI: GitHub Actions
- LLM Harness Target: OpenCode

---

## 2. Architectural Principles

### Stratified Design
Layers with strict dependency direction. Outer layers depend inward only.

### Vertical Slices
Features organized by capability, not technical layer. Each feature contains its usecase pipeline and any feature-specific logic together.

### Pure Core, Impure Boundary
Maximize pure logic in usecases. Push IO operations to the boundary. Domain has zero infrastructure dependencies. Usecases do not call IO directly; all effects are injected.

### Effect-Based Dependency Injection
Reader-style computation expression threads a dependency environment through usecase pipelines. Dependencies are supplied at the composition root (entry point). The compiler enforces that all required capabilities are satisfied.

---

## 3. Layers

1. **Core** (`Itr.Domain`)
   - Domain model (Product, Backlog, Task, Feature)
   - State machine and validation rules
   - `Effect<'deps, 'a>` type and computation expression
   - `EffectResult<'deps, 'a, 'err>` type and computation expression
   - Capability interfaces (`IFileSystem`, `IGitService`, `IYamlService`, etc.)
   - No infrastructure dependencies

2. **Features** (`Itr.Features`)
   - One module per vertical slice (Portfolio, Backlog, Product, etc.)
   - Usecases are pure pipelines using `effectResult { }` CE
   - Depend only on Core
   - No direct IO
   - Usecases cannot depend on each other; they are independent entry points into the domain logic. Coordination is done at the entry point layer, not between usecases.
   - Each entry point uses at most one usecase. If functionality is needed in more than one entry point, it is pulled out the usecase and shared across them. This keeps usecases focused and reusable without coupling them together. A usecase represents a usage of a capability in a specific context, not a reusable library function.

3. **Adapters** (`Itr.Adapters`)
   - Concrete implementations of Core interfaces
   - Filesystem, Git, YAML, environment/profile resolution
   - No domain logic

4. **Entry Points** (`Itr.Cli`, `Itr.Tui`, `Itr.Mcp`, `Itr.Server`)
   - Composition root: wire adapters into a deps record satisfying required interfaces
   - Parse input, run effect, handle output
   - No business logic

Dependency rule:
```
Entry Points → Features → Core
Entry Points → Adapters → Core
```

---

## 4. Project Structure

```
src/
  Itr.Domain/
    Domain/
      Product.fs
      Backlog.fs
      Task.fs
      Feature.fs
      StateMachine.fs
      Validation.fs
    Effect.fs          # Effect<'deps,'a>, EffectResult, CEs
    Interfaces.fs      # IFileSystem, IGitService, IYamlService, etc.

  Itr.Features/
    Portfolio/
      PortfolioUsecase.fs
    Backlog/
      BacklogUsecase.fs
    Product/
      ProductUsecase.fs

  Itr.Adapters/
    Filesystem.fs
    Git.fs
    Yaml.fs
    Environment.fs

  Itr.Cli/
  Itr.Tui/
  Itr.Mcp/
  Itr.Server/          # Future

tests/
  acceptance/
  building/
  communication/
```

---

## 5. Effect Pattern

### Core Types

```fsharp
// Effect<'deps, 'a> - a computation requiring a dependency environment
type Effect<'deps, 'a> = Effect of ('deps -> 'a)

module Effect =
    let run deps (Effect f) = f deps
    let map f (Effect g) = Effect (g >> f)
    let bind f (Effect g) =
        Effect (fun deps ->
            let a = g deps
            let (Effect h) = f a
            h deps)
    let ask<'deps> : Effect<'deps, 'deps> = Effect id
    let asks (f: 'deps -> 'a) : Effect<'deps, 'a> = Effect f

type EffectBuilder() =
    member _.Return(x) = Effect (fun _ -> x)
    member _.ReturnFrom(x) = x
    member _.Bind(x, f) = Effect.bind f x
    member _.Zero() = Effect (fun _ -> ())

let effect = EffectBuilder()
```

### EffectResult — Effect Combined with Result

Usecases typically return `EffectResult<'deps, 'a, 'err>` to handle both dependency injection and error propagation in a single pipeline.

```fsharp
// Convenience alias
type EffectResult<'deps, 'a, 'err> = Effect<'deps, Result<'a, 'err>>

module EffectResult =
    let succeed x : EffectResult<'deps, 'a, 'err> =
        Effect (fun _ -> Ok x)

    let fail err : EffectResult<'deps, 'a, 'err> =
        Effect (fun _ -> Error err)

    let ofResult (r: Result<'a, 'err>) : EffectResult<'deps, 'a, 'err> =
        Effect (fun _ -> r)

    let bind (f: 'a -> EffectResult<'deps, 'b, 'err>) (eff: EffectResult<'deps, 'a, 'err>) =
        Effect (fun deps ->
            match Effect.run deps eff with
            | Ok a    -> Effect.run deps (f a)
            | Error e -> Error e)

    let map f eff = bind (f >> succeed) eff

    let asks (f: 'deps -> 'a) : EffectResult<'deps, 'a, 'err> =
        Effect (fun deps -> Ok (f deps))

type EffectResultBuilder() =
    member _.Return(x)      = EffectResult.succeed x
    member _.ReturnFrom(x)  = x
    member _.Bind(x, f)     = EffectResult.bind f x
    member _.Zero()         = EffectResult.succeed ()

let effectResult = EffectResultBuilder()
```

### Capability Interfaces

Use `#` (inferred inheritance / SRTP constraints) so F# can unify multiple interface requirements into a single `'deps` type parameter at the composition root:

```fsharp
type IFileSystem =
    abstract ReadFile      : string -> Result<string, IoError>
    abstract WriteFile     : string -> string -> Result<unit, IoError>
    abstract DirectoryExists : string -> bool

type IGitService =
    abstract CurrentBranch  : unit -> string
    abstract IsBranchMerged : string -> bool
```

### Usecase Example

```fsharp
module BacklogUsecase =

    let promoteFeature (featureId: FeatureId) : EffectResult<#IFileSystem * #IGitService, Feature, DomainError> =
        effectResult {
            let! fs  = EffectResult.asks (fun (deps: #IFileSystem, _) -> deps)
            let! git = EffectResult.asks (fun (_, deps: #IGitService) -> deps)

            let! content = fs.ReadFile "backlog.yaml" |> Result.mapError IoError |> EffectResult.ofResult
            let! backlog = Yaml.parse content          |> Result.mapError ParseError |> EffectResult.ofResult
            let! feature = Backlog.findFeature featureId backlog
                           |> Result.mapError (fun _ -> FeatureNotFound featureId)
                           |> EffectResult.ofResult

            if not (git.IsBranchMerged feature.Branch) then
                return! EffectResult.fail (BranchNotMerged feature.Branch)
            else
                return Feature.promote feature
        }
```

### Composition Root (Entry Point)

```fsharp
// In Itr.Cli or Itr.Tui
type AppDeps(fs: IFileSystem, git: IGitService) =
    interface IFileSystem with
        member _.ReadFile path         = fs.ReadFile path
        member _.WriteFile path c      = fs.WriteFile path c
        member _.DirectoryExists path  = fs.DirectoryExists path
    interface IGitService with
        member _.CurrentBranch()       = git.CurrentBranch()
        member _.IsBranchMerged branch = git.IsBranchMerged branch

let deps = AppDeps(RealFileSystem(), RealGitService())
let result = BacklogUsecase.promoteFeature featureId |> Effect.run (deps, deps)
```

---

## 6. Usecase Responsibilities

Each usecase module in `Itr.Features`:

- Accepts a typed command or parameters
- Reads required context via `EffectResult.asks`
- Executes pure domain logic
- Returns `EffectResult<'deps, 'a, DomainError>`
- Does not persist; returns the result to the entry point

Entry point responsibilities:

- Compose dependencies at startup
- Run the effect
- Persist state changes
- Format and emit output

No entry point contains business logic.

---

## 7. Domain Events (Optional Pattern)

For usecases with side effects that must be coordinated (e.g. notifying downstream systems), collect events during execution rather than raising them immediately:

```fsharp
type UsecaseResult<'a> = {
    Value  : 'a
    Events : DomainEvent list
}
```

Events are persisted atomically with state changes at the entry point. This avoids inconsistency if execution fails after an event is emitted.

---

## 8. Domain Model Constraints

- Feature ID unique.
- Backlog ID unique.
- No ID exists in both backlog and active tasks.
- Feature must have exactly one owner.
- Valid state transitions only.
- All task repos must exist in product config.

State transitions enforced inside Core domain layer.

---

## 9. Test Strategy

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

## 10. GitHub Actions (CI)

CI pipeline:

1. Build solution
2. Run tests
3. Validate sample product fixtures
4. Enforce formatting
5. Optionally validate coordination roots in repository

No deployment pipeline required in MVP.

---

## 11. LLM Harness: OpenCode

Primary automation surface targets OpenCode.

Design implications:

- Deterministic command outputs
- Machine-readable responses
- Explicit error types
- No hidden side effects
- Idempotent operations

CLI commands must support structured output mode (JSON).

---

## 12. Extensibility

Designed to support:

- Adversarial planning agents (future phase)
- Cross-product orchestration
- Release manifests
- Conflict detection

These are layered as additional feature slices, not domain rewrites.
