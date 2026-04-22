module Itr.Domain.Portfolios.SetDefaultProfile

open Itr.Domain

type Input = { Name: string }

/// Set an existing named profile as the default in the portfolio.
/// Looks up the profile case-insensitively and returns the updated Portfolio.
/// Returns ProfileNotFound if no profile with the given name exists.
/// The caller is responsible for persisting via SaveConfig.
let execute<'deps when 'deps :> IPortfolioConfig>
    (configPath: string)
    (input: Input)
    : EffectResult<'deps, Portfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig

        config.LoadConfig configPath
        |> Result.bind (fun portfolio ->
            match Portfolio.tryFindProfileCaseInsensitive input.Name portfolio with
            | None -> Error(ProfileNotFound input.Name)
            | Some profile ->
                Ok { portfolio with DefaultProfile = Some profile.Name }))
