module Itr.Features.Portfolio

open System.IO
open Itr.Domain

type DomainPortfolio = Itr.Domain.Portfolio

/// Combined dependencies for full portfolio pipeline
type IPortfolioDeps =
    inherit IPortfolioConfig
    inherit IEnvironment
    inherit IFileSystem
    inherit IProductConfig

/// Load and parse a portfolio from the config path
let loadPortfolio<'deps when 'deps :> IPortfolioConfig>
    (configPath: string option)
    : EffectResult<'deps, DomainPortfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig
        let path = defaultArg configPath (config.ConfigPath())
        config.LoadConfig path)

/// Find a profile case-insensitively in a portfolio
let private tryFindProfileCaseInsensitive (portfolio: DomainPortfolio) (name: string) =
    Itr.Domain.Portfolio.tryFindProfileCaseInsensitive name portfolio

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
let private loadAllDefinitions<'deps when 'deps :> IProductConfig and 'deps :> IFileSystem>
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
/// Loads product.yaml from each registered root path, matches canonical id,
/// and returns a ResolvedProduct with the derived CoordinationRoot.
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

// Pipeline used by all interfaces:
// loadPortfolio >>= resolveActiveProfile >>= resolveProduct >>= executeProductCommand

/// Default content written to itr.json on first run
let private defaultConfigContent = """{"defaultProfile": null, "profiles": {}}"""

/// Check if config file exists; if absent, write default itr.json.
/// Returns Ok true if file was created, Ok false if it already existed,
/// or BootstrapWriteError if the write fails.
let bootstrapIfMissing<'deps when 'deps :> IFileSystem>
    (configPath: string)
    : EffectResult<'deps, bool, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let fs = deps :> IFileSystem

        if fs.FileExists configPath then
            Ok false
        else
            fs.WriteFile configPath defaultConfigContent
            |> Result.map (fun () -> true)
            |> Result.mapError (fun ioErr ->
                let msg =
                    match ioErr with
                    | IoException(_, m) -> m
                    | FileNotFound p -> $"File not found: {p}"
                    | DirectoryNotFound p -> $"Directory not found: {p}"

                BootstrapWriteError(configPath, msg)))

// ---------------------------------------------------------------------------
// addProfile use-case input type
// ---------------------------------------------------------------------------

type AddProfileInput =
    { Name: string
      GitIdentity: GitIdentity option
      SetAsDefault: bool }

/// Add a new named profile to the portfolio.
/// Validates the profile name, checks for duplicates, builds and returns the updated Portfolio.
/// The caller is responsible for persisting via SaveConfig.
let addProfile<'deps when 'deps :> IPortfolioConfig>
    (configPath: string)
    (input: AddProfileInput)
    : EffectResult<'deps, DomainPortfolio, PortfolioError> =
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
                          GitIdentity = input.GitIdentity }

                    let updatedProfiles = portfolio.Profiles |> Map.add profileName newProfile

                    let updatedDefault =
                        if input.SetAsDefault then Some profileName
                        else portfolio.DefaultProfile

                    Ok
                        { portfolio with
                            Profiles = updatedProfiles
                            DefaultProfile = updatedDefault })))
