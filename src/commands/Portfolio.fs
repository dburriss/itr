module Itr.Commands.Portfolio

open System.IO
open Itr.Domain

type DomainPortfolio = Itr.Domain.Portfolio

module Runtime =
    let mutable private configPathImpl: unit -> string = fun () -> Path.Combine(".", "portfolio.json")

    let mutable private readConfigImpl: string -> Result<DomainPortfolio, PortfolioError> =
        fun path -> Error(ConfigNotFound path)

    let configure (configPath: unit -> string) (readConfig: string -> Result<DomainPortfolio, PortfolioError>) =
        configPathImpl <- configPath
        readConfigImpl <- readConfig

    let configPath () = configPathImpl ()
    let readConfig path = readConfigImpl path

let loadPortfolio (configPath: string option) : Result<DomainPortfolio, PortfolioError> =
    let path = defaultArg configPath (Runtime.configPath ())
    Runtime.readConfig path

let private tryFindProfileCaseInsensitive (portfolio: DomainPortfolio) (name: string) =
    Itr.Domain.Portfolio.tryFindProfileCaseInsensitive name portfolio

let resolveActiveProfile
    (portfolio: DomainPortfolio)
    (flagProfile: string option)
    (readEnv: string -> string option)
    : Result<Profile, PortfolioError> =
    let resolvedName =
        match flagProfile with
        | Some name when not (System.String.IsNullOrWhiteSpace(name)) -> Some name
        | _ ->
            match readEnv "ITR_PROFILE" with
            | Some envName when not (System.String.IsNullOrWhiteSpace(envName)) -> Some envName
            | _ -> portfolio.DefaultProfile |> Option.map ProfileName.value

    match resolvedName with
    | None -> Error(ProfileNotFound "<none>")
    | Some profileName ->
        match tryFindProfileCaseInsensitive portfolio profileName with
        | Some profile -> Ok profile
        | None -> Error(ProfileNotFound profileName)

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

let resolveProduct
    (profile: Profile)
    (productId: string)
    (dirExists: string -> bool)
    : Result<ResolvedProduct, PortfolioError> =
    ProductId.tryCreate productId
    |> Result.bind (fun validId ->
        let maybeProduct = profile.Products |> List.tryFind (fun product -> product.Id = validId)

        match maybeProduct with
        | None -> Error(ProductNotFound productId)
        | Some product ->
            let rootDir = rootDirFromConfig product.Root
            let expectedPath = Path.GetFullPath(Path.Combine(rootDir, ".itr"))

            if dirExists expectedPath then
                Ok
                    { Profile = profile
                      Product = product
                      CoordRoot =
                        { Mode = modeFromConfig product.Root
                          AbsolutePath = expectedPath } }
            else
                Error(CoordRootNotFound(ProductId.value validId, expectedPath)))

// Pipeline used by all interfaces:
// loadPortfolio >>= resolveActiveProfile >>= resolveProduct >>= executeProductCommand
