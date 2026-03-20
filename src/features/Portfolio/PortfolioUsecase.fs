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
