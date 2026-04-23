module Itr.Cli.ErrorFormatting

open Itr.Domain
open Itr.Domain.Tasks
open Itr.Cli.CliParsers

let formatBacklogError (err: BacklogError) : string =
    match err with
    | ProductConfigNotFound root -> $"product.yaml not found at: {root}"
    | ProductConfigParseError(path, msg) -> $"Failed to parse product config at {path}: {msg}"
    | BacklogItemNotFound id -> $"Backlog item not found: {BacklogId.value id}"
    | RepoNotInProduct id ->
        if RepoId.value id = "" then
            "--repo is required when the product has multiple repos"
        else
            $"Repo '{RepoId.value id}' is not listed in product.yaml"
    | DuplicateBacklogId id -> $"Backlog item '{BacklogId.value id}' already exists"
    | InvalidItemType value -> $"Invalid item type '{value}': must be feature | bug | chore | refactor | spike"
    | MissingTitle -> "--title is required"

let formatTaskError (err: TaskError) : string =
    match err with
    | TaskNotFound id -> $"Task not found: {TaskId.value id}"
    | TaskIdConflict id -> $"Task id '{TaskId.value id}' already exists"
    | TaskIdOverrideRequiresSingleRepo -> "--task-id can only be used with single-repo backlog items"
    | InvalidTaskState(id, current) ->
        let stateStr = taskStateToDisplayString current
        $"Invalid state transition for task '{TaskId.value id}': current state is '{stateStr}'"
    | MissingPlanArtifact id -> $"Cannot approve task '{TaskId.value id}': plan artifact does not exist"
    | TaskStoreError(_, msg) -> msg

let formatPortfolioError (err: PortfolioError) : string =
    match err with
    | BootstrapWriteError(path, msg) -> $"Could not create itr.json at {path}: {msg}"
    | DuplicateProfileName name -> $"Profile '{name}' already exists."
    | InvalidProfileName(value, rules) -> $"Invalid profile name '{value}': {rules}"
    | InvalidProductId(value, rules) -> $"Invalid product id '{value}': {rules}"
    | ProductConfigError(root, msg) -> $"Product config error at '{root}': {msg}"
    | ProductNotFound id -> $"Product '{id}' not found."
    | CoordRootNotFound(id, path) -> $"Coordination root for '{id}' not found at: {path}"
    | DuplicateProductId(profile, id) -> $"Product '{id}' is already registered in profile '{profile}'."
    | ProfileNotFound name -> $"Profile '{name}' not found. Run 'profile add {name}' to create it."
    | other -> $"%A{other}"
