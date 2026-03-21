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

// ---------------------------------------------------------------------------
// registerProduct use-case
// ---------------------------------------------------------------------------

type RegisterProductInput =
    { Path: string
      Profile: string }

/// Register an existing product root under a named profile in itr.json.
/// Validates the profile name, checks for duplicate roots, appends to profile, and saves.
let registerProduct<'deps when 'deps :> IPortfolioConfig>
    (configPath: string)
    (input: RegisterProductInput)
    : EffectResult<'deps, DomainPortfolio, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let config = deps :> IPortfolioConfig

        config.LoadConfig configPath
        |> Result.bind (fun portfolio ->
            match Portfolio.tryFindProfileCaseInsensitive input.Profile portfolio with
            | None -> Error(ProfileNotFound input.Profile)
            | Some profile ->
                let absPath = System.IO.Path.GetFullPath(input.Path)
                let rootStr = ProductRoot absPath

                let alreadyRegistered =
                    profile.Products |> List.exists (fun p -> p.Root = rootStr)

                if alreadyRegistered then
                    Error(DuplicateProductId(ProfileName.value profile.Name, absPath))
                else
                    let newRef = { Root = rootStr }
                    let updatedProfile = { profile with Products = profile.Products @ [ newRef ] }
                    let updatedProfiles = portfolio.Profiles |> Map.add profile.Name updatedProfile
                    let updatedPortfolio = { portfolio with Profiles = updatedProfiles }

                    config.SaveConfig configPath updatedPortfolio
                    |> Result.map (fun () -> updatedPortfolio)))

// ---------------------------------------------------------------------------
// initProduct use-case input type and function
// ---------------------------------------------------------------------------

type InitProductInput =
    { Id: string
      Path: string
      RepoId: string
      CoordPath: string
      CoordinationMode: string
      RegisterProfile: string option }

/// Scaffold a new product on disk: writes product.yaml, PRODUCT.md, ARCHITECTURE.md,
/// and creates the coordination directory sentinel. Optionally registers the product.
let initProduct<'deps when 'deps :> IFileSystem and 'deps :> IPortfolioConfig>
    (configPath: string)
    (input: InitProductInput)
    : EffectResult<'deps, DomainPortfolio option, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let fs = deps :> IFileSystem

        // 1.3 Validate product id
        match ProductId.tryCreate input.Id with
        | Error e -> Error e
        | Ok _ ->

        // 1.4 Check path exists
        let absPath = System.IO.Path.GetFullPath(input.Path)

        if not (fs.DirectoryExists absPath) then
            Error(ProductConfigError(absPath, $"Directory does not exist: {absPath}"))
        else

        // 1.5 Guard against overwriting product.yaml
        let productYamlPath = System.IO.Path.Combine(absPath, "product.yaml")
        let productMdPath = System.IO.Path.Combine(absPath, "PRODUCT.md")
        let archMdPath = System.IO.Path.Combine(absPath, "ARCHITECTURE.md")

        if fs.FileExists productYamlPath then
            Error(ProductConfigError(absPath, $"product.yaml already exists at: {productYamlPath}"))
        elif fs.FileExists productMdPath then
            Error(ProductConfigError(absPath, $"PRODUCT.md already exists at: {productMdPath}"))
        elif fs.FileExists archMdPath then
            Error(ProductConfigError(absPath, $"ARCHITECTURE.md already exists at: {archMdPath}"))
        else

        // 1.6 Generate product.yaml content
        let productYamlContent =
            let baseYaml =
                $"""id: {input.Id}
docs:
  product: PRODUCT.md
  architecture: ARCHITECTURE.md
repos:
  {input.RepoId}:
    path: .
coordination:
  mode: {input.CoordinationMode}
"""
            if input.CoordinationMode = "standalone" then
                baseYaml + $"  path: {input.CoordPath}\n"
            else
                baseYaml + $"  repo: {input.RepoId}\n  path: {input.CoordPath}\n"

        // 1.9 PRODUCT.md template
        let productMdContent =
            $"""# Product: {input.Id}

## Purpose

<!-- TODO: Describe the purpose of this product -->
"""

        // 1.10 ARCHITECTURE.md template
        let archMdContent =
            $"""# Architecture: {input.Id}

## Technology Stack

<!-- TODO: Describe the technology stack -->
"""

        // 1.7 Write product.yaml
        match fs.WriteFile productYamlPath productYamlContent with
        | Error ioErr ->
            let msg =
                match ioErr with
                | IoException(_, m) -> m
                | FileNotFound p -> $"File not found: {p}"
                | DirectoryNotFound p -> $"Directory not found: {p}"

            Error(ProductConfigError(absPath, msg))
        | Ok() ->

        // 1.8 Write coordination directory sentinel
        let coordPath = System.IO.Path.Combine(absPath, input.CoordPath, ".gitkeep")

        match fs.WriteFile coordPath "" with
        | Error ioErr ->
            let msg =
                match ioErr with
                | IoException(_, m) -> m
                | FileNotFound p -> $"File not found: {p}"
                | DirectoryNotFound p -> $"Directory not found: {p}"

            Error(ProductConfigError(absPath, msg))
        | Ok() ->

        // 1.9 Write PRODUCT.md
        match fs.WriteFile productMdPath productMdContent with
        | Error ioErr ->
            let msg =
                match ioErr with
                | IoException(_, m) -> m
                | FileNotFound p -> $"File not found: {p}"
                | DirectoryNotFound p -> $"Directory not found: {p}"

            Error(ProductConfigError(absPath, msg))
        | Ok() ->

        // 1.10 Write ARCHITECTURE.md
        match fs.WriteFile archMdPath archMdContent with
        | Error ioErr ->
            let msg =
                match ioErr with
                | IoException(_, m) -> m
                | FileNotFound p -> $"File not found: {p}"
                | DirectoryNotFound p -> $"Directory not found: {p}"

            Error(ProductConfigError(absPath, msg))
        | Ok() ->

        // 1.11 Delegate to registerProduct when RegisterProfile is Some
        // 1.12 Return None when RegisterProfile is None
        match input.RegisterProfile with
        | None -> Ok None
        | Some profile ->
            let regInput: RegisterProductInput = { Path = absPath; Profile = profile }

            registerProduct configPath regInput
            |> Effect.run deps
            |> Result.map Some)
