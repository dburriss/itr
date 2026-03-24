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
    /// Save portfolio to a config file path
    abstract SaveConfig: path: string -> portfolio: Portfolio -> Result<unit, PortfolioError>

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

/// Capability interface for loading product configuration from product.yaml
type IProductConfig =
    /// Load product config from <productRoot>/product.yaml
    abstract LoadProductConfig: productRoot: string -> Result<ProductDefinition, PortfolioError>

/// Capability interface for loading backlog items
type IBacklogStore =
    /// Load a backlog item from <coordRoot>/BACKLOG/<backlog-id>/item.yaml
    abstract LoadBacklogItem: coordRoot: string -> backlogId: BacklogId -> Result<BacklogItem, BacklogError>
    /// Load an archived backlog item by scanning <coordRoot>/BACKLOG/_archive/ for a folder whose item.yaml has a matching id.
    /// Returns Ok(Some item) if found, Ok None if not present, Error on parse failure.
    abstract LoadArchivedBacklogItem: coordRoot: string -> backlogId: BacklogId -> Result<BacklogItem option, BacklogError>
    /// Archive a backlog item by moving <coordRoot>/BACKLOG/<backlog-id>/ to <coordRoot>/BACKLOG/_archive/<date>-<backlog-id>/
    abstract ArchiveBacklogItem: coordRoot: string -> backlogId: BacklogId -> date: string -> Result<unit, BacklogError>
    /// Check whether a backlog item already exists at <coordRoot>/BACKLOG/<backlog-id>/item.yaml
    abstract BacklogItemExists: coordRoot: string -> backlogId: BacklogId -> bool
    /// Write a backlog item to <coordRoot>/BACKLOG/<backlog-id>/item.yaml
    abstract WriteBacklogItem: coordRoot: string -> item: BacklogItem -> Result<unit, BacklogError>
    /// List all backlog items under <coordRoot>/BACKLOG/, skipping names starting with '_'
    abstract ListBacklogItems: coordRoot: string -> Result<BacklogItem list, BacklogError>
    /// List all archived backlog items under <coordRoot>/BACKLOG/_archive/; returns Ok [] if absent
    abstract ListArchivedBacklogItems: coordRoot: string -> Result<BacklogItem list, BacklogError>

/// A named view that groups backlog items
type BacklogView =
    { Id: string
      Description: string option
      Items: string list }

/// Capability interface for reading views from <coordRoot>/BACKLOG/_views/
type IViewStore =
    /// List all views from <coordRoot>/BACKLOG/_views/*.yaml; returns Ok [] if directory absent
    abstract ListViews: coordRoot: string -> Result<BacklogView list, BacklogError>

/// Capability interface for reading and writing task files
type ITaskStore =
    /// List all tasks for a given backlog id from <coordRoot>/BACKLOG/<backlog-id>/tasks/
    abstract ListTasks: coordRoot: string -> backlogId: BacklogId -> Result<ItrTask list, BacklogError>
    /// List all tasks for an archived backlog item by scanning <coordRoot>/BACKLOG/_archive/ for the matching folder
    abstract ListArchivedTasks: coordRoot: string -> backlogId: BacklogId -> Result<ItrTask list, BacklogError>
    /// Write a task file to <coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml
    abstract WriteTask: coordRoot: string -> task: ItrTask -> Result<unit, BacklogError>

    /// Archive a task by renaming <coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/ to <coordRoot>/BACKLOG/<backlog-id>/tasks/<date>-<task-id>/
    abstract ArchiveTask:
        coordRoot: string -> backlogId: BacklogId -> taskId: TaskId -> date: string -> Result<unit, BacklogError>
