namespace Itr.Domain

/// Effect<'deps, 'a> - a computation requiring a dependency environment.
/// This is essentially the Reader monad, allowing dependency injection
/// without mutable state.
type Effect<'deps, 'a> = Effect of ('deps -> 'a)

[<RequireQualifiedAccess>]
module Effect =
    /// Run an effect with the given dependencies
    let run (deps: 'deps) (Effect f) : 'a = f deps

    /// Map a function over the result of an effect
    let map (f: 'a -> 'b) (Effect g) : Effect<'deps, 'b> = Effect(g >> f)

    /// Bind an effect to a function that produces another effect
    let bind (f: 'a -> Effect<'deps, 'b>) (Effect g) : Effect<'deps, 'b> =
        Effect(fun deps ->
            let a = g deps
            let (Effect h) = f a
            h deps)

    /// Get the entire dependency environment
    let ask<'deps> : Effect<'deps, 'deps> = Effect id

    /// Get a projection of the dependency environment
    let asks (f: 'deps -> 'a) : Effect<'deps, 'a> = Effect f

    /// Lift a pure value into an effect
    let pure' (x: 'a) : Effect<'deps, 'a> = Effect(fun _ -> x)

/// Computation expression builder for Effect
type EffectBuilder() =
    member _.Return(x) = Effect(fun _ -> x)
    member _.ReturnFrom(x) = x
    member _.Bind(x, f) = Effect.bind f x
    member _.Zero() = Effect(fun _ -> ())

    member _.Combine(Effect f, Effect g) =
        Effect(fun deps ->
            f deps |> ignore
            g deps)

    member _.Delay(f) = f

    member _.Run(f) = f ()

[<AutoOpen>]
module EffectBuilderInstance =
    let effect = EffectBuilder()

/// EffectResult<'deps, 'a, 'err> - Effect combined with Result.
/// Usecases typically return this type to handle both dependency injection
/// and error propagation in a single pipeline.
type EffectResult<'deps, 'a, 'err> = Effect<'deps, Result<'a, 'err>>

[<RequireQualifiedAccess>]
module EffectResult =
    /// Lift a successful value into an EffectResult
    let succeed (x: 'a) : EffectResult<'deps, 'a, 'err> = Effect(fun _ -> Ok x)

    /// Lift an error into an EffectResult
    let fail (err: 'err) : EffectResult<'deps, 'a, 'err> = Effect(fun _ -> Error err)

    /// Lift a Result into an EffectResult
    let ofResult (r: Result<'a, 'err>) : EffectResult<'deps, 'a, 'err> = Effect(fun _ -> r)

    /// Bind an EffectResult to a function that produces another EffectResult
    let bind
        (f: 'a -> EffectResult<'deps, 'b, 'err>)
        (eff: EffectResult<'deps, 'a, 'err>)
        : EffectResult<'deps, 'b, 'err> =
        Effect(fun deps ->
            match Effect.run deps eff with
            | Ok a -> Effect.run deps (f a)
            | Error e -> Error e)

    /// Map a function over the success value of an EffectResult
    let map (f: 'a -> 'b) (eff: EffectResult<'deps, 'a, 'err>) : EffectResult<'deps, 'b, 'err> = bind (f >> succeed) eff

    /// Map a function over the error value of an EffectResult
    let mapError (f: 'err1 -> 'err2) (eff: EffectResult<'deps, 'a, 'err1>) : EffectResult<'deps, 'a, 'err2> =
        Effect(fun deps ->
            match Effect.run deps eff with
            | Ok a -> Ok a
            | Error e -> Error(f e))

    /// Get a projection of the dependency environment as a successful result
    let asks (f: 'deps -> 'a) : EffectResult<'deps, 'a, 'err> = Effect(fun deps -> Ok(f deps))

    /// Get the entire dependency environment as a successful result
    let ask<'deps, 'err> : EffectResult<'deps, 'deps, 'err> =
        Effect(fun deps -> Ok deps)

    /// Run an effect and lift its result into an EffectResult
    let liftEffect (eff: Effect<'deps, 'a>) : EffectResult<'deps, 'a, 'err> =
        Effect(fun deps -> Ok(Effect.run deps eff))

    /// Require a condition, failing with the given error if false
    let require (condition: bool) (err: 'err) : EffectResult<'deps, unit, 'err> =
        if condition then succeed () else fail err

/// Computation expression builder for EffectResult
type EffectResultBuilder() =
    member _.Return(x) = EffectResult.succeed x
    member _.ReturnFrom(x) = x
    member _.Bind(x, f) = EffectResult.bind f x
    member _.Zero() = EffectResult.succeed ()

    member _.Combine(eff1: EffectResult<'deps, unit, 'err>, eff2: EffectResult<'deps, 'a, 'err>) =
        EffectResult.bind (fun () -> eff2) eff1

    member _.Delay(f) = f

    member _.Run(f) = f ()

    /// Allow binding plain Results inside effectResult { }
    member _.Source(r: Result<'a, 'err>) : EffectResult<'deps, 'a, 'err> = EffectResult.ofResult r

[<AutoOpen>]
module EffectResultBuilderInstance =
    let effectResult = EffectResultBuilder()
