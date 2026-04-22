module Itr.Domain.Portfolios.Query

open Itr.Domain

type DomainPortfolio = Itr.Domain.Portfolio

/// Combined dependencies for full portfolio pipeline
type IPortfolioDeps =
    inherit IPortfolioConfig
    inherit IEnvironment
    inherit IFileSystem
    inherit IProductConfig

/// Find a profile case-insensitively in a portfolio
let private tryFindProfileCaseInsensitive (portfolio: DomainPortfolio) (name: string) =
    Itr.Domain.Portfolio.tryFindProfileCaseInsensitive name portfolio

/// Load and parse a portfolio from the config path
let load<'deps when 'deps :> IPortfolioConfig>
    (configPath: string option)
    : EffectResult<'deps, DomainPortfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig
        let path = defaultArg configPath (config.ConfigPath())
        config.LoadConfig path)

/// Resolve the active profile based on precedence: flag > env > default
let resolveActiveProfile<'deps when 'deps :> IEnvironment>
    (portfolio: DomainPortfolio)
    (flagProfile: string option)
    : EffectResult<'deps, Profile, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let env = deps :> IEnvironment

        let resolvedName =
            match flagProfile with
            | Some name when not (System.String.IsNullOrWhiteSpace(name)) -> Some name
            | _ ->
                match env.GetEnvVar "ITR_PROFILE" with
                | Some envName when not (System.String.IsNullOrWhiteSpace(envName)) -> Some envName
                | _ -> portfolio.DefaultProfile |> Option.map ProfileName.value

        match resolvedName with
        | None -> Error(ProfileNotFound "<none>")
        | Some profileName ->
            match tryFindProfileCaseInsensitive portfolio profileName with
            | Some profile -> Ok profile
            | None -> Error(ProfileNotFound profileName))

/// Load all product definitions from registered roots, detecting duplicates.
/// Returns a list of (ProductRef * ProductDefinition) pairs.
let loadAllDefinitions<'deps when 'deps :> IProductConfig and 'deps :> IFileSystem>
    (profile: Profile)
    (deps: 'deps)
    : Result<(ProductRef * ProductDefinition) list, PortfolioError> =
    let productConfig = deps :> IProductConfig

    profile.Products
    |> List.fold
        (fun state productRef ->
            state
            |> Result.bind (fun (acc, seenIds) ->
                let (ProductRoot root) = productRef.Root

                match productConfig.LoadProductConfig root with
                | Error e -> Error e
                | Ok definition ->
                    let idStr = ProductId.value definition.Id

                    match Map.tryFind idStr seenIds with
                    | Some _ -> Error(DuplicateProductId(ProfileName.value profile.Name, idStr))
                    | None ->
                        let newSeenIds = Map.add idStr true seenIds
                        Ok((productRef, definition) :: acc, newSeenIds)))
        (Ok([], Map.empty))
    |> Result.map (fun (pairs, _) -> List.rev pairs)

/// Resolve a product by ID within a profile.
let resolveProduct<'deps when 'deps :> IProductConfig and 'deps :> IFileSystem>
    (profile: Profile)
    (productId: string)
    : EffectResult<'deps, ResolvedProduct, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let fs = deps :> IFileSystem

        loadAllDefinitions profile deps
        |> Result.bind (fun pairs ->
            match pairs |> List.tryFind (fun (_, def) -> ProductId.value def.Id = productId) with
            | None -> Error(ProductNotFound productId)
            | Some(productRef, definition) ->
                let expectedPath = definition.CoordRoot.AbsolutePath

                if fs.DirectoryExists expectedPath then
                    Ok
                        { Profile = profile
                          Product = productRef
                          Definition = definition
                          CoordRoot = definition.CoordRoot }
                else
                    Error(CoordRootNotFound(productId, expectedPath))))
