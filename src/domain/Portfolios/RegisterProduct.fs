module Itr.Domain.Portfolios.RegisterProduct

open Itr.Domain
open Itr.Domain.Portfolios.Query

type Input =
    { Path: string
      Profile: string option }

/// Register an existing product root under a named profile in itr.json.
/// Validates directory existence, loads product.yaml to read canonical id,
/// detects duplicate canonical ids, appends ProductRef and returns updated Portfolio.
/// The caller is responsible for persisting via SaveConfig.
let execute<'deps when 'deps :> IPortfolioConfig and 'deps :> IProductConfig and 'deps :> IFileSystem>
    (configPath: string)
    (input: Input)
    : EffectResult<'deps, Portfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig
        let fs = deps :> IFileSystem
        let productConfig = deps :> IProductConfig

        config.LoadConfig configPath
        |> Result.bind (fun portfolio ->
            let profileName =
                match input.Profile with
                | Some name when not (System.String.IsNullOrWhiteSpace(name)) -> Some name
                | _ -> portfolio.DefaultProfile |> Option.map ProfileName.value

            match profileName with
            | None -> Error(ProfileNotFound "<none>")
            | Some name ->
                match Portfolio.tryFindProfileCaseInsensitive name portfolio with
                | None -> Error(ProfileNotFound name)
                | Some profile ->

                if System.String.IsNullOrWhiteSpace(input.Path) then
                    Error(ProductConfigError(input.Path, "Path must not be empty"))
                else

                let resolvedPath = System.IO.Path.GetFullPath(input.Path)

                if not (fs.DirectoryExists resolvedPath) then
                    Error(ProductConfigError(resolvedPath, $"Directory does not exist: {resolvedPath}"))
                else

                match productConfig.LoadProductConfig resolvedPath with
                | Error e -> Error e
                | Ok definition ->

                let newId = ProductId.value definition.Id

                match loadAllDefinitions profile deps with
                | Error e -> Error e
                | Ok existingPairs ->
                    let isDuplicate = existingPairs |> List.exists (fun (_, def) -> ProductId.value def.Id = newId)

                    if isDuplicate then
                        Error(DuplicateProductId(ProfileName.value profile.Name, newId))
                    else

                    let newRef = { Root = ProductRoot resolvedPath }
                    let updatedProfile = { profile with Products = profile.Products @ [ newRef ] }
                    let updatedProfiles = portfolio.Profiles |> Map.add profile.Name updatedProfile
                    Ok { portfolio with Profiles = updatedProfiles }))
