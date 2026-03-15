namespace Itr.Domain

open System.Text.RegularExpressions

type ProfileName = ProfileName of string

type RepoPath = RepoPath of string

type CoordinationMode =
    | Standalone
    | PrimaryRepo
    | ControlRepo

type CoordinationRoot =
    { Mode: CoordinationMode
      AbsolutePath: string }

type CoordinationRootConfig =
    | StandaloneConfig of string
    | PrimaryRepoConfig of string
    | ControlRepoConfig of string

type GitIdentity = { Name: string; Email: string option }

type ProductId = private ProductId of string

type ProductRef =
    { Id: ProductId
      Root: CoordinationRootConfig }

type Profile =
    { Name: ProfileName
      Products: ProductRef list
      GitIdentity: GitIdentity option }

type Portfolio =
    { DefaultProfile: ProfileName option
      Profiles: Map<ProfileName, Profile> }

type ResolvedProduct =
    { Profile: Profile
      Product: ProductRef
      CoordRoot: CoordinationRoot }

type PortfolioError =
    | ConfigNotFound of expectedPath: string
    | ConfigParseError of path: string * message: string
    | InvalidProductId of value: string * rules: string
    | DuplicateProfileName of profileName: string
    | DuplicateProductId of profileName: string * productId: string
    | ProfileNotFound of profileName: string
    | ProductNotFound of productId: string
    | CoordRootNotFound of productId: string * expectedPath: string

[<RequireQualifiedAccess>]
module ProductId =
    let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)
    let private rules = "must match [a-z0-9][a-z0-9-]*"

    let tryCreate (value: string) : Result<ProductId, PortfolioError> =
        if System.String.IsNullOrWhiteSpace(value) then
            Error(InvalidProductId(value, rules))
        elif slugRegex.IsMatch(value) then
            Ok(ProductId value)
        else
            Error(InvalidProductId(value, rules))

    let value (ProductId value) = value

[<RequireQualifiedAccess>]
module ProfileName =
    let value (ProfileName value) = value
    let normalize (ProfileName value) = value.Trim().ToLowerInvariant()
    let create value = ProfileName value

[<RequireQualifiedAccess>]
module Portfolio =
    let private duplicateProfileNameError (name: ProfileName) =
        DuplicateProfileName(ProfileName.value name)

    let private duplicateProductIdError (profileName: ProfileName) (productId: ProductId) =
        DuplicateProductId(ProfileName.value profileName, ProductId.value productId)

    let private tryEnsureDistinctProducts (profile: Profile) =
        let dupes =
            profile.Products
            |> List.groupBy (fun product -> ProductId.value product.Id)
            |> List.tryFind (fun (_, products) -> products.Length > 1)

        match dupes with
        | Some(_, products) -> Error(duplicateProductIdError profile.Name products.Head.Id)
        | None -> Ok profile

    let tryCreate (defaultProfile: ProfileName option) (profiles: Profile list) : Result<Portfolio, PortfolioError> =
        let duplicateProfiles =
            profiles
            |> List.groupBy (fun profile -> ProfileName.normalize profile.Name)
            |> List.tryFind (fun (_, grouped) -> grouped.Length > 1)

        match duplicateProfiles with
        | Some(_, first :: _) -> Error(duplicateProfileNameError first.Name)
        | _ ->
            let distinctProductsCheck =
                profiles |> List.map tryEnsureDistinctProducts |> List.tryFind Result.isError

            match distinctProductsCheck with
            | Some(Error error) -> Error error
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

// ---------------------------------------------------------------------------
// Backlog / Task domain types
// ---------------------------------------------------------------------------

type BacklogId = private BacklogId of string

type TaskId = private TaskId of string

type RepoId = RepoId of string

type TaskState =
    | Planning
    | InProgress
    | Implemented
    | Validated

type BacklogItem =
    { Id: BacklogId
      Title: string
      Repos: RepoId list }

type ItrTask =
    { Id: TaskId
      SourceBacklog: BacklogId
      Repo: RepoId
      State: TaskState
      CreatedAt: System.DateOnly }

type RepoConfig = { Path: string; Url: string option }

type ProductConfig =
    { Id: ProductId
      Repos: Map<RepoId, RepoConfig> }

type TakeError =
    | ProductConfigNotFound of coordRoot: string
    | ProductConfigParseError of path: string * message: string
    | BacklogItemNotFound of BacklogId
    | RepoNotInProduct of RepoId
    | TaskIdConflict of TaskId
    | TaskIdOverrideRequiresSingleRepo

[<RequireQualifiedAccess>]
module BacklogId =
    let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)
    let private rules = "must match [a-z0-9][a-z0-9-]*"

    let tryCreate (value: string) : Result<BacklogId, TakeError> =
        if System.String.IsNullOrWhiteSpace(value) then
            Error(BacklogItemNotFound(BacklogId value))
        elif slugRegex.IsMatch(value) then
            Ok(BacklogId value)
        else
            Error(BacklogItemNotFound(BacklogId value))

    let value (BacklogId v) = v

[<RequireQualifiedAccess>]
module TaskId =
    let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)

    let tryCreate (value: string) : Result<TaskId, TakeError> =
        if System.String.IsNullOrWhiteSpace(value) then
            Error(TaskIdConflict(TaskId value))
        elif slugRegex.IsMatch(value) then
            Ok(TaskId value)
        else
            Error(TaskIdConflict(TaskId value))

    let create (value: string) : TaskId = TaskId value

    let value (TaskId v) = v

[<RequireQualifiedAccess>]
module RepoId =
    let value (RepoId v) = v
