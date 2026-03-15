module Itr.Features.Portfolio

open System.IO
open Itr.Domain

type DomainPortfolio = Itr.Domain.Portfolio

/// Combined dependencies for full portfolio pipeline
type IPortfolioDeps =
    inherit IPortfolioConfig
    inherit IEnvironment
    inherit IFileSystem

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

let private rootDirFromConfig root =
    match root with
    | StandaloneConfig dir
    | PrimaryRepoConfig dir
    | ControlRepoConfig dir -> dir

let private modeFromConfig root =
    match root with
    | StandaloneConfig _ -> Standalone
    | PrimaryRepoConfig _ -> PrimaryRepo
    | ControlRepoConfig _ -> ControlRepo

/// Resolve a product by ID within a profile
let resolveProduct<'deps when 'deps :> IFileSystem>
    (profile: Profile)
    (productId: string)
    : EffectResult<'deps, ResolvedProduct, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let fs = deps :> IFileSystem

        ProductId.tryCreate productId
        |> Result.bind (fun validId ->
            let maybeProduct =
                profile.Products |> List.tryFind (fun product -> product.Id = validId)

            match maybeProduct with
            | None -> Error(ProductNotFound productId)
            | Some product ->
                let rootDir = rootDirFromConfig product.Root
                let expectedPath = Path.GetFullPath(Path.Combine(rootDir, ".itr"))

                if fs.DirectoryExists expectedPath then
                    Ok
                        { Profile = profile
                          Product = product
                          CoordRoot =
                            { Mode = modeFromConfig product.Root
                              AbsolutePath = expectedPath } }
                else
                    Error(CoordRootNotFound(ProductId.value validId, expectedPath))))

// Pipeline used by all interfaces:
// loadPortfolio >>= resolveActiveProfile >>= resolveProduct >>= executeProductCommand
