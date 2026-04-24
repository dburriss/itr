namespace Itr.Domain

type BacklogItemType =
    | Feature
    | Bug
    | Chore
    | Refactor
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

/// Computed status of a backlog item derived from its tasks
type BacklogItemStatus =
    | Created
    | Planning
    | Planned
    | Approved
    | InProgress
    | Completed
    | Archived

/// Summary of a backlog item including computed status
type BacklogItemSummary =
    { Item: BacklogItem
      Status: BacklogItemStatus
      ViewId: string option
      TaskCount: int
      Path: string }

/// Detailed view of a single backlog item including its tasks
type BacklogItemDetail =
    { Item: BacklogItem
      Status: BacklogItemStatus
      ViewId: string option
      Tasks: ItrTask list
      Path: string }

/// A named view that groups backlog items
type BacklogView =
    { Id: string
      Description: string option
      Items: string list }

/// Snapshot of all backlog items loaded for a given coordination root
type BacklogSnapshot =
    { Items: BacklogItemSummary list
      Views: BacklogView list }

type BacklogError =
    | ProductConfigNotFound of coordRoot: string
    | ProductConfigParseError of path: string * message: string
    | BacklogItemNotFound of BacklogId
    | RepoNotInProduct of RepoId
    | DuplicateBacklogId of BacklogId
    | InvalidItemType of value: string
    | MissingTitle

[<RequireQualifiedAccess>]
module BacklogItemStatus =
    /// Compute status from a list of tasks.
    /// Priority order:
    ///   Archived > Completed > InProgress > Approved > Planned > Planning > Created
    let compute (tasks: ItrTask list) : BacklogItemStatus =
        if tasks.IsEmpty then
            Created
        else
            let states = tasks |> List.map (fun t -> t.State)
            let allArchived = states |> List.forall (fun s -> s = TaskState.Archived)

            if allArchived then
                Archived
            else
                let allDone =
                    states
                    |> List.forall (fun s ->
                        s = TaskState.Implemented || s = TaskState.Validated || s = TaskState.Archived)

                if allDone then
                    Completed
                else
                    let anyInProgress = states |> List.exists (fun s -> s = TaskState.InProgress)

                    if anyInProgress then
                        InProgress
                    else
                        let allApprovedOrBeyond =
                            states
                            |> List.forall (fun s ->
                                s = TaskState.Approved
                                || s = TaskState.Implemented
                                || s = TaskState.Validated
                                || s = TaskState.Archived)

                        if allApprovedOrBeyond then
                            Approved
                        else
                            let allPlannedOrBeyond =
                                states
                                |> List.forall (fun s ->
                                    s = TaskState.Planned
                                    || s = TaskState.Approved
                                    || s = TaskState.Implemented
                                    || s = TaskState.Validated
                                    || s = TaskState.Archived)

                            if allPlannedOrBeyond then Planned else Planning

[<RequireQualifiedAccess>]
module BacklogItemType =
    let tryParse (value: string) : Result<BacklogItemType, BacklogError> =
        match value with
        | null
        | ""
        | "feature" -> Ok Feature
        | "bug" -> Ok Bug
        | "chore" -> Ok Chore
        | "refactor" -> Ok Refactor
        | "spike" -> Ok Spike
        | other -> Error(InvalidItemType other)

    let toString (t: BacklogItemType) : string =
        match t with
        | Feature -> "feature"
        | Bug -> "bug"
        | Chore -> "chore"
        | Refactor -> "refactor"
        | Spike -> "spike"

[<RequireQualifiedAccess>]
module BacklogItem =
    let itemDir (coordRoot: string) (backlogId: BacklogId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId)

    let itemFile (coordRoot: string) (backlogId: BacklogId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId, "item.yaml")

/// Capability interface for loading backlog items
type IBacklogStore =
    /// Load a backlog item from <coordRoot>/BACKLOG/<backlog-id>/item.yaml
    abstract LoadBacklogItem: coordRoot: string -> backlogId: BacklogId -> Result<BacklogItem * string, BacklogError>

    /// Load an archived backlog item by scanning <coordRoot>/BACKLOG/_archive/ for a folder whose item.yaml has a matching id.
    /// Returns Ok(Some(item, path)) if found, Ok None if not present, Error on parse failure.
    abstract LoadArchivedBacklogItem:
        coordRoot: string -> backlogId: BacklogId -> Result<(BacklogItem * string) option, BacklogError>

    /// Archive a backlog item by moving <coordRoot>/BACKLOG/<backlog-id>/ to <coordRoot>/BACKLOG/_archive/<date>-<backlog-id>/
    abstract ArchiveBacklogItem: coordRoot: string -> backlogId: BacklogId -> date: string -> Result<unit, BacklogError>
    /// Check whether a backlog item already exists at <coordRoot>/BACKLOG/<backlog-id>/item.yaml
    abstract BacklogItemExists: coordRoot: string -> backlogId: BacklogId -> bool
    /// Write a backlog item to <coordRoot>/BACKLOG/<backlog-id>/item.yaml
    abstract WriteBacklogItem: coordRoot: string -> item: BacklogItem -> Result<unit, BacklogError>
    /// List all backlog items under <coordRoot>/BACKLOG/, skipping names starting with '_'
    abstract ListBacklogItems: coordRoot: string -> Result<(BacklogItem * string) list, BacklogError>
    /// List all archived backlog items under <coordRoot>/BACKLOG/_archive/; returns Ok [] if absent
    abstract ListArchivedBacklogItems: coordRoot: string -> Result<(BacklogItem * string) list, BacklogError>

/// Capability interface for reading views from <coordRoot>/BACKLOG/_views/
type IViewStore =
    /// List all views from <coordRoot>/BACKLOG/_views/*.yaml; returns Ok [] if directory absent
    abstract ListViews: coordRoot: string -> Result<BacklogView list, BacklogError>
