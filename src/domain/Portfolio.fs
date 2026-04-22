namespace Itr.Domain

type ProfileName = ProfileName of string

/// Configuration for the AI agent harness
type AgentConfig =
    { Protocol: string
      Command: string
      Args: string list }

type ProductRef = { Root: ProductRoot }

type Profile =
    { Name: ProfileName
      Products: ProductRef list
      GitIdentity: GitIdentity option
      AgentConfig: AgentConfig }

type Portfolio =
    { DefaultProfile: ProfileName option
      Profiles: Map<ProfileName, Profile> }

type PortfolioError =
    | ConfigNotFound of expectedPath: string
    | ConfigParseError of path: string * message: string
    | InvalidProductId of value: string * rules: string
    | InvalidProfileName of value: string * rules: string
    | DuplicateProfileName of profileName: string
    | DuplicateProductId of profileName: string * productId: string
    | ProfileNotFound of profileName: string
    | ProductNotFound of productId: string
    | CoordRootNotFound of productId: string * expectedPath: string
    | BootstrapWriteError of path: string * message: string
    | ProductConfigError of productRoot: string * message: string

[<RequireQualifiedAccess>]
module ProfileName =
    let private rules = "must match [a-z0-9][a-z0-9-]*"

    let tryCreate (value: string) : Result<ProfileName, PortfolioError> =
        if Validation.isValidSlug value then
            Ok(ProfileName value)
        else
            Error(InvalidProfileName(value, rules))

    let value (ProfileName value) = value
    let normalize (ProfileName value) = value.Trim().ToLowerInvariant()
    let create value = ProfileName value

[<RequireQualifiedAccess>]
module Portfolio =
    let private duplicateProfileNameError (name: ProfileName) =
        DuplicateProfileName(ProfileName.value name)

    let tryCreate (defaultProfile: ProfileName option) (profiles: Profile list) : Result<Portfolio, PortfolioError> =
        let duplicateProfiles =
            profiles
            |> List.groupBy (fun profile -> ProfileName.normalize profile.Name)
            |> List.tryFind (fun (_, grouped) -> grouped.Length > 1)

        match duplicateProfiles with
        | Some(_, first :: _) -> Error(duplicateProfileNameError first.Name)
        | _ ->
            let profilesMap =
                profiles |> List.map (fun profile -> profile.Name, profile) |> Map.ofList

            Ok
                { DefaultProfile = defaultProfile
                  Profiles = profilesMap }

    let tryFindProfileCaseInsensitive (name: string) (portfolio: Portfolio) : Profile option =
        let normalized = name.Trim().ToLowerInvariant()

        portfolio.Profiles
        |> Map.toSeq
        |> Seq.tryPick (fun (profileName, profile) ->
            if ProfileName.normalize profileName = normalized then
                Some profile
            else
                None)

/// Capability interface for portfolio configuration loading
type IPortfolioConfig =
    /// Get the default config file path
    abstract ConfigPath: unit -> string
    /// Load portfolio from a config file path
    abstract LoadConfig: path: string -> Result<Portfolio, PortfolioError>
    /// Save portfolio to a config file path
    abstract SaveConfig: path: string -> portfolio: Portfolio -> Result<unit, PortfolioError>
