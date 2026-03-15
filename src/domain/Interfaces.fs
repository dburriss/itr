namespace Itr.Domain

/// Error type for IO operations
type IoError =
    | FileNotFound of path: string
    | DirectoryNotFound of path: string
    | IoException of path: string * message: string

/// Capability interface for filesystem operations
type IFileSystem =
    /// Read file contents as string
    abstract ReadFile: path: string -> Result<string, IoError>
    /// Write string contents to file
    abstract WriteFile: path: string -> content: string -> Result<unit, IoError>
    /// Check if a file exists
    abstract FileExists: path: string -> bool
    /// Check if a directory exists
    abstract DirectoryExists: path: string -> bool

/// Capability interface for environment variable access
type IEnvironment =
    /// Get an environment variable, returns None if not set or empty
    abstract GetEnvVar: name: string -> string option
    /// Get the user's home directory
    abstract HomeDirectory: unit -> string

/// Capability interface for portfolio configuration loading
type IPortfolioConfig =
    /// Get the default config file path
    abstract ConfigPath: unit -> string
    /// Load portfolio from a config file path
    abstract LoadConfig: path: string -> Result<Portfolio, PortfolioError>

/// Capability interface for YAML serialization (placeholder for future)
type IYamlService =
    /// Parse YAML content into a typed object
    abstract Parse<'a> : content: string -> Result<'a, string>
    /// Serialize an object to YAML
    abstract Serialize<'a> : value: 'a -> string

/// Capability interface for Git operations (placeholder for future)
type IGitService =
    /// Get the current branch name
    abstract CurrentBranch: unit -> string
    /// Check if a branch has been merged to main/master
    abstract IsBranchMerged: branch: string -> bool
