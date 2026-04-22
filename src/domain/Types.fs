namespace Itr.Domain

open System.Text.RegularExpressions

type RepoPath = RepoPath of string

type ProductRoot = ProductRoot of string

type GitIdentity = { Name: string; Email: string option }

type CoordinationMode =
    | Standalone
    | PrimaryRepo
    | ControlRepo

type CoordinationRoot =
    { Mode: CoordinationMode
      AbsolutePath: string }

type RepoId = RepoId of string

type BacklogId = BacklogId of string

[<RequireQualifiedAccess>]
module RepoId =
    let value (RepoId v) = v

[<RequireQualifiedAccess>]
module BacklogId =
    let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)

    let value (BacklogId v) = v

    /// Returns Ok(BacklogId) or Error(message string). Error type is string to avoid
    /// depending on domain-level error DUs (BacklogError) not yet defined at this point.
    let tryCreate (value: string) : Result<BacklogId, string> =
        if System.String.IsNullOrWhiteSpace(value) || not (slugRegex.IsMatch(value)) then
            Error $"Invalid backlog id '{value}': must match [a-z0-9][a-z0-9-]*"
        else
            Ok(BacklogId value)

/// Error type for IO operations
type IoError =
    | FileNotFound of path: string
    | DirectoryNotFound of path: string
    | IoException of path: string * message: string

/// Capability interface for filesystem operations
type IFileSystem =
    abstract ReadFile: path: string -> Result<string, IoError>
    abstract WriteFile: path: string -> content: string -> Result<unit, IoError>
    abstract FileExists: path: string -> bool
    abstract DirectoryExists: path: string -> bool

/// Capability interface for environment variable access
type IEnvironment =
    abstract GetEnvVar: name: string -> string option
    abstract HomeDirectory: unit -> string

/// Capability interface for YAML serialization
type IYamlService =
    abstract Parse<'a> : content: string -> Result<'a, string>
    abstract Serialize<'a> : value: 'a -> string

/// Capability interface for Git operations
type IGitService =
    abstract CurrentBranch: unit -> string
    abstract IsBranchMerged: branch: string -> bool

/// Capability interface for AI agent harness interactions
type IAgentHarness =
    abstract Prompt: prompt: string -> debug: bool -> Result<string, string>
