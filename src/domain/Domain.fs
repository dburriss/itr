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

/// Wrapper for a product root directory path
type ProductRoot = ProductRoot of string

type GitIdentity = { Name: string; Email: string option }

type ProductId = private ProductId of string

/// Coordination configuration as read from product.yaml
type CoordinationConfig =
    { Mode: string
      Repo: string option
      Path: string option }

/// Canonical product definition loaded from product.yaml
type ProductDefinition =
    { Id: ProductId
      Repos: Map<string, RepoConfig>
      Docs: Map<string, string>
      Coordination: CoordinationConfig
      CoordRoot: CoordinationRoot }

and RepoConfig = { Path: string; Url: string option }

type ProductRef = { Root: ProductRoot }

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
      Definition: ProductDefinition
      CoordRoot: CoordinationRoot }

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
    let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)
    let private rules = "must match [a-z0-9][a-z0-9-]*"

    let tryCreate (value: string) : Result<ProfileName, PortfolioError> =
        if System.String.IsNullOrWhiteSpace(value) then
            Error(InvalidProfileName(value, rules))
        elif slugRegex.IsMatch(value) then
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

type BacklogItemType =
    | Feature
    | Bug
    | Chore
    | Spike

type BacklogItem =
    { Id: BacklogId
      Title: string
      Repos: RepoId list
      Type: BacklogItemType
      Priority: string option
      Summary: string option
      AcceptanceCriteria: string list
      Dependencies: BacklogId list
      CreatedAt: System.DateOnly }

type ItrTask =
    { Id: TaskId
      SourceBacklog: BacklogId
      Repo: RepoId
      State: TaskState
      CreatedAt: System.DateOnly }

type ProductConfig =
    { Id: ProductId
      Repos: Map<RepoId, RepoConfig> }

type BacklogError =
    | ProductConfigNotFound of coordRoot: string
    | ProductConfigParseError of path: string * message: string
    | BacklogItemNotFound of BacklogId
    | RepoNotInProduct of RepoId
    | TaskIdConflict of TaskId
    | TaskIdOverrideRequiresSingleRepo
    | DuplicateBacklogId of BacklogId
    | InvalidItemType of value: string
    | MissingTitle

[<RequireQualifiedAccess>]
module BacklogItemType =
    let tryParse (value: string) : Result<BacklogItemType, BacklogError> =
        match value with
        | null | "" | "feature" -> Ok Feature
        | "bug" -> Ok Bug
        | "chore" -> Ok Chore
        | "spike" -> Ok Spike
        | other -> Error(InvalidItemType other)

    let toString (t: BacklogItemType) : string =
        match t with
        | Feature -> "feature"
        | Bug -> "bug"
        | Chore -> "chore"
        | Spike -> "spike"

[<RequireQualifiedAccess>]
module BacklogId =
    let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)
    let private rules = "must match [a-z0-9][a-z0-9-]*"

    let tryCreate (value: string) : Result<BacklogId, BacklogError> =
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

    let tryCreate (value: string) : Result<TaskId, BacklogError> =
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
