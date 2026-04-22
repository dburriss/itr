namespace Itr.Domain

type TaskId = private TaskId of string

type TaskState =
    | Planning
    | Planned
    | Approved
    | InProgress
    | Implemented
    | Validated
    | Archived

type ItrTask =
    { Id: TaskId
      SourceBacklog: BacklogId
      Repo: RepoId
      State: TaskState
      CreatedAt: System.DateOnly }

type TaskError =
    | TaskNotFound of TaskId
    | InvalidTaskState of taskId: TaskId * current: TaskState
    | MissingPlanArtifact of TaskId
    | TaskIdConflict of TaskId
    | TaskIdOverrideRequiresSingleRepo
    | TaskStoreError of path: string * message: string

[<RequireQualifiedAccess>]
module TaskId =
    let tryCreate (value: string) : Result<TaskId, TaskError> =
        if Validation.isValidSlug value then
            Ok(TaskId value)
        else
            Error(TaskIdConflict(TaskId value))

    let create (value: string) : TaskId = TaskId value

    let value (TaskId v) = v

[<RequireQualifiedAccess>]
module ItrTask =
    let taskDir (coordRoot: string) (BacklogId backlogId) (TaskId taskId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskId)

    let taskFile (coordRoot: string) (BacklogId backlogId) (TaskId taskId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskId, "task.yaml")

    let planFile (coordRoot: string) (BacklogId backlogId) (TaskId taskId) =
        System.IO.Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskId, "plan.md")

/// Capability interface for reading and writing task files
type ITaskStore =
    /// List all tasks for a given backlog id from <coordRoot>/BACKLOG/<backlog-id>/tasks/
    /// Returns tuples of (task, taskYamlPath) where taskYamlPath is the absolute path to task.yaml
    abstract ListTasks: coordRoot: string -> backlogId: BacklogId -> Result<(ItrTask * string) list, TaskError>
    /// List all tasks for an archived backlog item by scanning <coordRoot>/BACKLOG/_archive/ for the matching folder
    abstract ListArchivedTasks: coordRoot: string -> backlogId: BacklogId -> Result<ItrTask list, TaskError>
    /// Write a task file to <coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml
    abstract WriteTask: coordRoot: string -> task: ItrTask -> Result<unit, TaskError>

    /// Archive a task by renaming <coordRoot>/BACKLOG/<backlog-id>/tasks/<task-id>/ to <coordRoot>/BACKLOG/<backlog-id>/tasks/<date>-<task-id>/
    abstract ArchiveTask:
        coordRoot: string -> backlogId: BacklogId -> taskId: TaskId -> date: string -> Result<unit, TaskError>

    /// List all tasks across all backlog items (active and archived) under <coordRoot>/BACKLOG/
    /// Returns tuples of (task, taskYamlPath) where taskYamlPath is the absolute path to task.yaml
    abstract ListAllTasks: coordRoot: string -> Result<(ItrTask * string) list, TaskError>
