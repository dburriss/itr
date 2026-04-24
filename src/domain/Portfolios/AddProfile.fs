module Itr.Domain.Portfolios.AddProfile

open Itr.Domain

type Input =
    { Name: string
      GitIdentity: GitIdentity option
      SetAsDefault: bool }

/// Add a new named profile to the portfolio.
/// Validates the profile name, checks for duplicates, builds and returns the updated Portfolio.
/// The caller is responsible for persisting via SaveConfig.
let execute<'deps when 'deps :> IPortfolioConfig>
    (configPath: string)
    (input: Input)
    : EffectResult<'deps, Portfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig

        ProfileName.tryCreate input.Name
        |> Result.bind (fun profileName ->
            config.LoadConfig configPath
            |> Result.bind (fun portfolio ->
                let nameStr = ProfileName.value profileName
                let normalizedNew = nameStr.Trim().ToLowerInvariant()

                let isDuplicate =
                    portfolio.Profiles
                    |> Map.exists (fun k _ -> ProfileName.normalize k = normalizedNew)

                if isDuplicate then
                    Error(DuplicateProfileName nameStr)
                else
                    let newProfile =
                        { Name = profileName
                          Products = []
                          GitIdentity = input.GitIdentity
                          AgentConfig =
                            { Protocol = "opencode-http"
                              Command = "opencode"
                              Args = [] } }

                    let updatedProfiles = portfolio.Profiles |> Map.add profileName newProfile

                    let updatedDefault =
                        if input.SetAsDefault then
                            Some profileName
                        else
                            portfolio.DefaultProfile

                    Ok
                        { portfolio with
                            Profiles = updatedProfiles
                            DefaultProfile = updatedDefault })))
