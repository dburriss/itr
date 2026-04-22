module Itr.Adapters.YamlAdapter

open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open YamlDotNet.Core
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions
open Itr.Domain

// ---------------------------------------------------------------------------
// DTOs (CLIMutable for YamlDotNet deserialization)
// ---------------------------------------------------------------------------

[<CLIMutable>]
type RepoConfigDto =
    { [<YamlMember(Alias = "path")>]
      Path: string
      [<YamlMember(Alias = "url")>]
      Url: string }

[<CLIMutable>]
type CoordinationConfigDto =
    { [<YamlMember(Alias = "mode")>]
      Mode: string
      [<YamlMember(Alias = "repo")>]
      Repo: string
      [<YamlMember(Alias = "path")>]
      Path: string }

[<CLIMutable>]
type ProductConfigDto =
    { [<YamlMember(Alias = "id")>]
      Id: string
      [<YamlMember(Alias = "description")>]
      Description: string
      [<YamlMember(Alias = "repos")>]
      Repos: Dictionary<string, RepoConfigDto>
      [<YamlMember(Alias = "docs")>]
      Docs: Dictionary<string, string>
      [<YamlMember(Alias = "coordination")>]
      Coordination: CoordinationConfigDto }

[<CLIMutable>]
type BacklogItemDto =
    { [<YamlMember(Alias = "id")>]
      Id: string
      [<YamlMember(Alias = "title")>]
      Title: string
      [<YamlMember(Alias = "repos")>]
      Repos: ResizeArray<string>
      [<YamlMember(Alias = "type")>]
      Type: string
      [<YamlMember(Alias = "priority")>]
      Priority: string
      [<YamlMember(Alias = "summary", ScalarStyle = ScalarStyle.Literal)>]
      Summary: string
      [<YamlMember(Alias = "acceptance_criteria")>]
      AcceptanceCriteria: ResizeArray<string>
      [<YamlMember(Alias = "dependencies")>]
      Dependencies: ResizeArray<string>
      [<YamlMember(Alias = "created_at")>]
      CreatedAt: string }

[<CLIMutable>]
type TaskSourceDto =
    { [<YamlMember(Alias = "backlog")>]
      Backlog: string }

[<CLIMutable>]
type ItrTaskDto =
    { [<YamlMember(Alias = "id")>]
      Id: string
      [<YamlMember(Alias = "source")>]
      Source: TaskSourceDto
      [<YamlMember(Alias = "repo")>]
      Repo: string
      [<YamlMember(Alias = "state")>]
      State: string
      [<YamlMember(Alias = "created_at")>]
      CreatedAt: string }

// ---------------------------------------------------------------------------
// YAML helpers
// ---------------------------------------------------------------------------

let private deserializer =
    DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties().Build()

let private serializer =
    SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build()

let private parseYaml<'a> (content: string) : Result<'a, string> =
    try
        Ok(deserializer.Deserialize<'a>(content))
    with ex ->
        Error ex.Message

let private serializeYaml<'a> (value: 'a) : string = serializer.Serialize(value)

// ---------------------------------------------------------------------------
// Domain mapping helpers
// ---------------------------------------------------------------------------

let private slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)

let private validateSlug (value: string) (path: string) : Result<ProductId, PortfolioError> =
    if String.IsNullOrWhiteSpace(value) then
        Error(InvalidProductId(value, "must match [a-z0-9][a-z0-9-]*"))
    elif slugRegex.IsMatch(value) then
        ProductId.tryCreate value
    else
        Error(InvalidProductId(value, "must match [a-z0-9][a-z0-9-]*"))

let private deriveCoordinationRoot
    (productRoot: string)
    (repos: Map<string, RepoConfig>)
    (coord: CoordinationConfigDto)
    (path: string)
    : Result<CoordinationRoot, PortfolioError> =
    if isNull coord.Mode then
        Error(ProductConfigError(productRoot, "coordination.mode is required"))
    else
        match coord.Mode with
        | "standalone" ->
            let coordPath =
                if isNull coord.Path || coord.Path = "" then
                    ".itr"
                else
                    coord.Path

            let absPath = Path.GetFullPath(Path.Combine(productRoot, coordPath))

            Ok
                { Mode = Standalone
                  AbsolutePath = absPath }

        | "primary-repo"
        | "control-repo" ->
            if isNull coord.Repo || coord.Repo = "" then
                Error(ProductConfigError(productRoot, $"coordination.repo is required for mode '{coord.Mode}'"))
            else
                match Map.tryFind coord.Repo repos with
                | None -> Error(ProductConfigError(productRoot, $"coordination.repo '{coord.Repo}' not found in repos"))
                | Some repoConfig ->
                    let coordSubPath =
                        if isNull coord.Path || coord.Path = "" then
                            ".itr"
                        else
                            coord.Path

                    let absPath =
                        Path.GetFullPath(Path.Combine(productRoot, repoConfig.Path, coordSubPath))

                    let mode =
                        if coord.Mode = "primary-repo" then
                            PrimaryRepo
                        else
                            ControlRepo

                    Ok { Mode = mode; AbsolutePath = absPath }

        | unknown -> Error(ProductConfigError(productRoot, $"Unknown coordination mode '{unknown}'"))

let private mapRepos (dto: Dictionary<string, RepoConfigDto>) : Map<string, RepoConfig> =
    if isNull dto then
        Map.empty
    else
        dto
        |> Seq.map (fun kvp ->
            let url =
                if isNull kvp.Value.Url || kvp.Value.Url = "" then
                    None
                else
                    Some kvp.Value.Url

            kvp.Key, ({ Path = kvp.Value.Path; Url = url }: RepoConfig))
        |> Map.ofSeq

let private mapDocs (dto: Dictionary<string, string>) : Map<string, string> =
    if isNull dto then
        Map.empty
    else
        dto |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq

let private mapTaskState (s: string) : TaskState =
    match s with
    | "planned" -> TaskState.Planned
    | "approved" -> TaskState.Approved
    | "in_progress" -> TaskState.InProgress
    | "implemented" -> TaskState.Implemented
    | "validated" -> TaskState.Validated
    | "archived" -> TaskState.Archived
    | _ -> TaskState.Planning

let private taskStateToString (state: TaskState) : string =
    match state with
    | TaskState.Planning -> "planning"
    | TaskState.Planned -> "planned"
    | TaskState.Approved -> "approved"
    | TaskState.InProgress -> "in_progress"
    | TaskState.Implemented -> "implemented"
    | TaskState.Validated -> "validated"
    | TaskState.Archived -> "archived"

let private mapTaskDto (dto: ItrTaskDto) : Result<ItrTask, TaskError> =
    match BacklogId.tryCreate dto.Source.Backlog with
    | Error _ -> Error(TaskStoreError("<task>", $"Invalid backlog id: {dto.Source.Backlog}"))
    | Ok backlogId ->
        Ok
            { Id = TaskId.create dto.Id
              SourceBacklog = backlogId
              Repo = RepoId dto.Repo
              State = mapTaskState dto.State
              CreatedAt =
                match DateOnly.TryParse(dto.CreatedAt) with
                | true, d -> d
                | _ -> DateOnly.FromDateTime(DateTime.UtcNow) }

let private taskToDto (task: ItrTask) : ItrTaskDto =
    { Id = TaskId.value task.Id
      Source = { Backlog = BacklogId.value task.SourceBacklog }
      Repo = RepoId.value task.Repo
      State = taskStateToString task.State
      CreatedAt = task.CreatedAt.ToString("yyyy-MM-dd") }

// ---------------------------------------------------------------------------
// IYamlService implementation
// ---------------------------------------------------------------------------

type YamlServiceAdapter() =
    interface IYamlService with
        member _.Parse<'a>(content: string) : Result<'a, string> = parseYaml<'a> content
        member _.Serialize<'a>(value: 'a) : string = serializeYaml value

// ---------------------------------------------------------------------------
// IProductConfig implementation
// ---------------------------------------------------------------------------

type ProductConfigAdapter() =
    interface IProductConfig with
        member _.LoadProductConfig(productRoot: string) =
            let path = Path.Combine(productRoot, "product.yaml")

            if not (File.Exists(path)) then
                Error(ProductConfigError(productRoot, $"product.yaml not found at: {path}"))
            else
                try
                    let content = File.ReadAllText(path)

                    match parseYaml<ProductConfigDto> content with
                    | Error msg -> Error(ProductConfigError(path, msg))
                    | Ok dto when isNull dto.Id -> Error(ProductConfigError(path, "Missing required field 'id'"))
                    | Ok dto ->
                        match validateSlug dto.Id path with
                        | Error e -> Error e
                        | Ok productId ->
                            let repos = mapRepos dto.Repos
                            let docs = mapDocs dto.Docs

                            let coordDto =
                                if box dto.Coordination = null then
                                    { Mode = "standalone"
                                      Repo = null
                                      Path = ".itr" }
                                else
                                    dto.Coordination

                            match deriveCoordinationRoot productRoot repos coordDto path with
                            | Error e -> Error e
                            | Ok coordRoot ->
                                let coordConfig: CoordinationConfig =
                                    { Mode = coordDto.Mode
                                      Repo =
                                        if isNull coordDto.Repo || coordDto.Repo = "" then
                                            None
                                        else
                                            Some coordDto.Repo
                                      Path =
                                        if isNull coordDto.Path || coordDto.Path = "" then
                                            None
                                        else
                                            Some coordDto.Path }

                                Ok
                                    { Id = productId
                                      Description =
                                        if isNull dto.Description || dto.Description = "" then
                                            None
                                        else
                                            Some dto.Description
                                      Repos = repos
                                      Docs = docs
                                      Coordination = coordConfig
                                      CoordRoot = coordRoot }
                with ex ->
                    Error(ProductConfigError(productRoot, ex.Message))

// ---------------------------------------------------------------------------
// IBacklogStore implementation
// ---------------------------------------------------------------------------

type BacklogStoreAdapter() =
    interface IBacklogStore with
        member _.LoadBacklogItem (coordRoot: string) (backlogId: BacklogId) =
            let id = BacklogId.value backlogId
            let path = BacklogItem.itemFile coordRoot backlogId

            if not (File.Exists(path)) then
                Error(BacklogItemNotFound backlogId)
            else
                try
                    let content = File.ReadAllText(path)

                    match parseYaml<BacklogItemDto> content with
                    | Error msg -> Error(ProductConfigParseError(path, msg))
                    | Ok dto ->
                        let repos =
                            if isNull dto.Repos then
                                []
                            else
                                dto.Repos |> Seq.toList |> List.map RepoId

                        let itemTypeResult =
                            if isNull dto.Type || dto.Type = "" then
                                Ok Feature
                            else
                                BacklogItemType.tryParse dto.Type

                        match itemTypeResult with
                        | Error e -> Error e
                        | Ok itemType ->

                        let deps =
                            if isNull dto.Dependencies then
                                []
                            else
                                dto.Dependencies
                                |> Seq.toList
                                |> List.choose (fun d ->
                                    match BacklogId.tryCreate d with
                                    | Ok bid -> Some bid
                                    | Error _ -> None)

                        let ac =
                            if isNull dto.AcceptanceCriteria then []
                            else dto.AcceptanceCriteria |> Seq.toList

                        let createdAt =
                            match DateOnly.TryParse(dto.CreatedAt) with
                            | true, d -> d
                            | _ -> DateOnly.MinValue

                        Ok(
                            { Id = backlogId
                              Title = if isNull dto.Title then id else dto.Title
                              Repos = repos
                              Type = itemType
                              Priority = if isNull dto.Priority || dto.Priority = "" then None else Some dto.Priority
                              Summary = if isNull dto.Summary || dto.Summary = "" then None else Some dto.Summary
                              AcceptanceCriteria = ac
                              Dependencies = deps
                              CreatedAt = createdAt },
                            path)
                with ex ->
                    Error(ProductConfigParseError(path, ex.Message))

        member _.LoadArchivedBacklogItem (coordRoot: string) (backlogId: BacklogId) =
            let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive")

            if not (Directory.Exists(archiveDir)) then
                Ok None
            else
                try
                    let idStr = BacklogId.value backlogId
                    let dirs = Directory.GetDirectories(archiveDir)

                    let rec tryDirs remaining =
                        match remaining with
                        | [] -> Ok None
                        | dir :: rest ->
                            let path = Path.Combine(dir, "item.yaml")
                            if not (File.Exists(path)) then
                                tryDirs rest
                            else
                                try
                                    let content = File.ReadAllText(path)
                                    match parseYaml<BacklogItemDto> content with
                                    | Error msg -> Error(ProductConfigParseError(path, msg))
                                    | Ok dto ->
                                        if dto.Id <> idStr then
                                            tryDirs rest
                                        else
                                             let repos =
                                                if isNull dto.Repos then []
                                                 else dto.Repos |> Seq.toList |> List.map RepoId
                                             let itemTypeResult =
                                                if isNull dto.Type || dto.Type = "" then Ok Feature
                                                else BacklogItemType.tryParse dto.Type
                                             match itemTypeResult with
                                             | Error e -> Error e
                                             | Ok itemType ->
                                             let deps =
                                                if isNull dto.Dependencies then []
                                                else
                                                    dto.Dependencies
                                                    |> Seq.toList
                                                    |> List.choose (fun d ->
                                                        match BacklogId.tryCreate d with
                                                        | Ok bid -> Some bid
                                                        | Error _ -> None)
                                             let ac =
                                                if isNull dto.AcceptanceCriteria then []
                                                else dto.AcceptanceCriteria |> Seq.toList
                                             let createdAt =
                                                match DateOnly.TryParse(dto.CreatedAt) with
                                                | true, d -> d
                                                | _ -> DateOnly.MinValue
                                             let item : BacklogItem =
                                                { Id = backlogId
                                                  Title = if isNull dto.Title then idStr else dto.Title
                                                  Repos = repos
                                                  Type = itemType
                                                  Priority = if isNull dto.Priority || dto.Priority = "" then None else Some dto.Priority
                                                  Summary = if isNull dto.Summary || dto.Summary = "" then None else Some dto.Summary
                                                  AcceptanceCriteria = ac
                                                  Dependencies = deps
                                                  CreatedAt = createdAt }
                                             Ok(Some(item, path))
                                with ex ->
                                    Error(ProductConfigParseError(path, ex.Message))

                    tryDirs (dirs |> Array.toList)
                with ex ->
                    Error(ProductConfigParseError(archiveDir, ex.Message))

        member _.ArchiveBacklogItem (coordRoot: string) (backlogId: BacklogId) (date: string) =
            let id = BacklogId.value backlogId
            let sourcePath = BacklogItem.itemDir coordRoot backlogId
            let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive")
            let destPath = Path.Combine(archiveDir, $"{date}-{id}")

            if not (Directory.Exists(sourcePath)) then
                Error(BacklogItemNotFound backlogId)
            else
                try
                    Directory.CreateDirectory(archiveDir) |> ignore
                    Directory.Move(sourcePath, destPath)
                    // Rewrite all task.yaml files under the archived folder to state: archived
                    let tasksDir = Path.Combine(destPath, "tasks")
                    if Directory.Exists(tasksDir) then
                        for taskFile in Directory.GetFiles(tasksDir, "task.yaml", SearchOption.AllDirectories) do
                            let content = File.ReadAllText(taskFile)
                            match parseYaml<ItrTaskDto> content with
                            | Ok dto ->
                                let updated = { dto with State = "archived" }
                                File.WriteAllText(taskFile, serializeYaml updated)
                            | Error _ -> () // leave as-is if unparseable
                    Ok()
                with ex ->
                    Error(ProductConfigParseError(sourcePath, ex.Message))

        member _.BacklogItemExists (coordRoot: string) (backlogId: BacklogId) =
            let path = BacklogItem.itemFile coordRoot backlogId
            File.Exists(path)

        member _.WriteBacklogItem (coordRoot: string) (item: BacklogItem) =
            let id = BacklogId.value item.Id
            let itemDir = BacklogItem.itemDir coordRoot item.Id
            let path = BacklogItem.itemFile coordRoot item.Id

            try
                Directory.CreateDirectory(itemDir) |> ignore

                let ac =
                    if item.AcceptanceCriteria.IsEmpty then null
                    else item.AcceptanceCriteria |> ResizeArray

                let deps =
                    if item.Dependencies.IsEmpty then null
                    else item.Dependencies |> List.map BacklogId.value |> ResizeArray

                let dto: BacklogItemDto =
                    { Id = id
                      Title = item.Title
                      Repos = item.Repos |> List.map RepoId.value |> ResizeArray
                      Type = BacklogItemType.toString item.Type
                      Priority = item.Priority |> Option.defaultValue null
                      Summary = item.Summary |> Option.defaultValue null
                      AcceptanceCriteria = ac
                      Dependencies = deps
                      CreatedAt = item.CreatedAt.ToString("yyyy-MM-dd") }

                let yaml = serializeYaml dto
                File.WriteAllText(path, yaml)
                Ok()
            with ex ->
                Error(ProductConfigParseError(path, ex.Message))

        member self.ListBacklogItems (coordRoot: string) =
            let backlogDir = Path.Combine(coordRoot, "BACKLOG")

            if not (Directory.Exists(backlogDir)) then
                Ok []
            else
                try
                    let dirs =
                        Directory.GetDirectories(backlogDir)
                        |> Array.filter (fun d -> not (Path.GetFileName(d).StartsWith("_")))

                    let results =
                        dirs
                        |> Array.toList
                        |> List.map (fun dir ->
                            let dirName = Path.GetFileName(dir)
                            match BacklogId.tryCreate dirName with
                            | Error _ -> Error(ProductConfigParseError(dir, $"Invalid backlog id: {dirName}"))
                            | Ok backlogId ->
                                (self :> IBacklogStore).LoadBacklogItem coordRoot backlogId)

                    let errors =
                        results
                        |> List.choose (function
                            | Error e -> Some e
                            | Ok _ -> None)

                    match errors with
                    | e :: _ -> Error e
                    | [] ->
                        Ok(
                            results
                            |> List.choose (function
                                | Ok tuple -> Some tuple
                                | Error _ -> None)
                        )
                with ex ->
                    Error(ProductConfigParseError(backlogDir, ex.Message))

        member _.ListArchivedBacklogItems (coordRoot: string) =
            let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive")

            if not (Directory.Exists(archiveDir)) then
                Ok []
            else
                try
                    let dirs = Directory.GetDirectories(archiveDir)

                    let results =
                        dirs
                        |> Array.toList
                        |> List.map (fun dir ->
                            let path = Path.Combine(dir, "item.yaml")
                            if not (File.Exists(path)) then
                                Ok None
                            else
                                try
                                    let content = File.ReadAllText(path)
                                    match parseYaml<BacklogItemDto> content with
                                    | Error msg -> Error(ProductConfigParseError(path, msg))
                                    | Ok dto ->
                                        match BacklogId.tryCreate dto.Id with
                                        | Error _ -> Error(ProductConfigParseError(path, $"Invalid backlog id in yaml: {dto.Id}"))
                                        | Ok backlogId ->
                                            let repos =
                                                if isNull dto.Repos then []
                                                else dto.Repos |> Seq.toList |> List.map RepoId

                                            let itemTypeResult =
                                                if isNull dto.Type || dto.Type = "" then Ok Feature
                                                else BacklogItemType.tryParse dto.Type

                                            match itemTypeResult with
                                            | Error e -> Error e
                                            | Ok itemType ->

                                            let deps =
                                                if isNull dto.Dependencies then []
                                                else
                                                    dto.Dependencies
                                                    |> Seq.toList
                                                    |> List.choose (fun d ->
                                                        match BacklogId.tryCreate d with
                                                        | Ok bid -> Some bid
                                                        | Error _ -> None)

                                            let ac =
                                                if isNull dto.AcceptanceCriteria then []
                                                else dto.AcceptanceCriteria |> Seq.toList

                                            let createdAt =
                                                match DateOnly.TryParse(dto.CreatedAt) with
                                                | true, d -> d
                                                | _ -> DateOnly.MinValue

                                            let item : BacklogItem =
                                                { Id = backlogId
                                                  Title = if isNull dto.Title then dto.Id else dto.Title
                                                  Repos = repos
                                                  Type = itemType
                                                  Priority = if isNull dto.Priority || dto.Priority = "" then None else Some dto.Priority
                                                  Summary = if isNull dto.Summary || dto.Summary = "" then None else Some dto.Summary
                                                  AcceptanceCriteria = ac
                                                  Dependencies = deps
                                                  CreatedAt = createdAt }
                                            Ok(Some(item, path))
                                with ex ->
                                    Error(ProductConfigParseError(path, ex.Message)))

                    let errors =
                        results
                        |> List.choose (function
                            | Error e -> Some e
                            | Ok _ -> None)

                    match errors with
                    | e :: _ -> Error e
                    | [] ->
                        Ok(results |> List.choose (function Ok(Some tuple) -> Some tuple | _ -> None))
                with ex ->
                    Error(ProductConfigParseError(archiveDir, ex.Message))

// ---------------------------------------------------------------------------
// ITaskStore implementation
// ---------------------------------------------------------------------------

type TaskStoreAdapter() =
    interface ITaskStore with
        member _.ListTasks (coordRoot: string) (backlogId: BacklogId) =
            let dir = Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId, "tasks")

            if not (Directory.Exists(dir)) then
                Ok []
            else
                try
                    let subdirs = Directory.GetDirectories(dir)

                    let results =
                        subdirs
                        |> Array.toList
                        |> List.choose (fun subdir ->
                            let taskFile = Path.Combine(subdir, "task.yaml")
                            if File.Exists(taskFile) then Some taskFile else None)
                        |> List.map (fun path ->
                            let content = File.ReadAllText(path)

                            match parseYaml<ItrTaskDto> content with
                            | Error msg -> Error(TaskStoreError(path, msg))
                            | Ok dto ->
                                match mapTaskDto dto with
                                | Error e -> Error e
                                | Ok task -> Ok(task, path))

                    let errors =
                        results
                        |> List.choose (function
                            | Error e -> Some e
                            | Ok _ -> None)

                    match errors with
                    | e :: _ -> Error e
                    | [] ->
                        Ok(
                            results
                            |> List.choose (function
                                | Ok tuple -> Some tuple
                                | Error _ -> None)
                        )
                with ex ->
                    Error(TaskStoreError(dir, ex.Message))

        member _.ListArchivedTasks (coordRoot: string) (backlogId: BacklogId) =
            let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive")

            if not (Directory.Exists(archiveDir)) then
                Ok []
            else
                try
                    let idStr = BacklogId.value backlogId
                    // Find the archive folder whose item.yaml has a matching id
                    let matchingDir =
                        Directory.GetDirectories(archiveDir)
                        |> Array.tryFind (fun dir ->
                            let itemPath = Path.Combine(dir, "item.yaml")
                            if not (File.Exists(itemPath)) then false
                            else
                                try
                                    let content = File.ReadAllText(itemPath)
                                    match parseYaml<BacklogItemDto> content with
                                    | Ok dto -> dto.Id = idStr
                                    | Error _ -> false
                                with _ -> false)

                    match matchingDir with
                    | None -> Ok []
                    | Some dir ->
                        let tasksDir = Path.Combine(dir, "tasks")
                        if not (Directory.Exists(tasksDir)) then
                            Ok []
                        else
                            let subdirs = Directory.GetDirectories(tasksDir)
                            let results =
                                subdirs
                                |> Array.toList
                                |> List.choose (fun subdir ->
                                    let taskFile = Path.Combine(subdir, "task.yaml")
                                    if File.Exists(taskFile) then Some taskFile else None)
                                |> List.map (fun path ->
                                    let content = File.ReadAllText(path)
                                    match parseYaml<ItrTaskDto> content with
                                    | Error msg -> Error(TaskStoreError(path, msg))
                                    | Ok dto -> mapTaskDto dto)

                            let errors =
                                results |> List.choose (function Error e -> Some e | Ok _ -> None)

                            match errors with
                            | e :: _ -> Error e
                            | [] ->
                                Ok(results |> List.choose (function Ok t -> Some t | Error _ -> None))
                with ex ->
                    Error(TaskStoreError(archiveDir, ex.Message))

        member _.WriteTask (coordRoot: string) (task: ItrTask) =
            let taskDir = ItrTask.taskDir coordRoot task.SourceBacklog task.Id
            let path = ItrTask.taskFile coordRoot task.SourceBacklog task.Id

            try
                Directory.CreateDirectory(taskDir) |> ignore

                let dto = taskToDto task
                let yaml = serializeYaml dto
                File.WriteAllText(path, yaml)
                Ok()
            with ex ->
                Error(TaskStoreError(path, ex.Message))

        member _.ArchiveTask (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) (date: string) =
            let tid = TaskId.value taskId
            let sourcePath = ItrTask.taskDir coordRoot backlogId taskId
            let destPath = Path.Combine(coordRoot, "BACKLOG", BacklogId.value backlogId, "tasks", $"{date}-{tid}")

            if not (Directory.Exists(sourcePath)) then
                Error(TaskStoreError(sourcePath, $"Task folder not found: {sourcePath}"))
            else
                try
                    Directory.Move(sourcePath, destPath)
                    Ok()
                with ex ->
                    Error(TaskStoreError(sourcePath, ex.Message))

        member _.ListAllTasks (coordRoot: string) =
            let backlogDir = Path.Combine(coordRoot, "BACKLOG")
            let archiveDir = Path.Combine(coordRoot, "BACKLOG", "_archive")

            /// Read all task.yaml files from tasks/ subdirs under a given backlog directory
            let readTasksFromBacklogDir (dir: string) : Result<(ItrTask * string) list, TaskError> =
                let tasksDir = Path.Combine(dir, "tasks")
                if not (Directory.Exists(tasksDir)) then
                    Ok []
                else
                    try
                        let subdirs = Directory.GetDirectories(tasksDir)
                        let results =
                            subdirs
                            |> Array.toList
                            |> List.choose (fun subdir ->
                                let taskFile = Path.Combine(subdir, "task.yaml")
                                if File.Exists(taskFile) then Some taskFile else None)
                            |> List.map (fun path ->
                                let content = File.ReadAllText(path)
                                match parseYaml<ItrTaskDto> content with
                                | Error msg -> Error(TaskStoreError(path, msg))
                                | Ok dto ->
                                    match mapTaskDto dto with
                                    | Error e -> Error e
                                    | Ok task -> Ok(task, path))
                        let errors = results |> List.choose (function Error e -> Some e | Ok _ -> None)
                        match errors with
                        | e :: _ -> Error e
                        | [] -> Ok(results |> List.choose (function Ok tuple -> Some tuple | Error _ -> None))
                    with ex ->
                        Error(TaskStoreError(tasksDir, ex.Message))

            // Collect tasks from active backlog items (skip dirs starting with '_')
            let activeResult =
                if not (Directory.Exists(backlogDir)) then
                    Ok []
                else
                    try
                        let dirs =
                            Directory.GetDirectories(backlogDir)
                            |> Array.filter (fun d -> not (Path.GetFileName(d).StartsWith("_")))
                        let results = dirs |> Array.toList |> List.map readTasksFromBacklogDir
                        let errors = results |> List.choose (function Error e -> Some e | Ok _ -> None)
                        match errors with
                        | e :: _ -> Error e
                        | [] -> Ok(results |> List.collect (function Ok ts -> ts | Error _ -> []))
                    with ex ->
                        Error(TaskStoreError(backlogDir, ex.Message))

            // Collect tasks from archived backlog items
            let archivedResult =
                if not (Directory.Exists(archiveDir)) then
                    Ok []
                else
                    try
                        let dirs = Directory.GetDirectories(archiveDir)
                        let results = dirs |> Array.toList |> List.map readTasksFromBacklogDir
                        let errors = results |> List.choose (function Error e -> Some e | Ok _ -> None)
                        match errors with
                        | e :: _ -> Error e
                        | [] -> Ok(results |> List.collect (function Ok ts -> ts | Error _ -> []))
                    with ex ->
                        Error(TaskStoreError(archiveDir, ex.Message))

            match activeResult, archivedResult with
            | Error e, _ -> Error e
            | _, Error e -> Error e
            | Ok activeTasks, Ok archivedTasks -> Ok(activeTasks @ archivedTasks)

// ---------------------------------------------------------------------------
// BacklogViewDto
// ---------------------------------------------------------------------------

[<CLIMutable>]
type BacklogViewDto =
    { [<YamlMember(Alias = "id")>]
      Id: string
      [<YamlMember(Alias = "description")>]
      Description: string
      [<YamlMember(Alias = "items")>]
      Items: string array }

// ---------------------------------------------------------------------------
// IViewStore implementation
// ---------------------------------------------------------------------------

type ViewStoreAdapter() =
    interface IViewStore with
        member _.ListViews (coordRoot: string) =
            let viewsDir = Path.Combine(coordRoot, "BACKLOG", "_views")

            if not (Directory.Exists(viewsDir)) then
                Ok []
            else
                try
                    let files =
                        Directory.GetFiles(viewsDir, "*.yaml")
                        |> Array.sort // alphabetical for first-match semantics

                    let results =
                        files
                        |> Array.toList
                        |> List.map (fun path ->
                            let content = File.ReadAllText(path)

                            match parseYaml<BacklogViewDto> content with
                            | Error msg -> Error(ProductConfigParseError(path, msg))
                            | Ok dto ->
                                Ok
                                    { BacklogView.Id = if isNull dto.Id then Path.GetFileNameWithoutExtension(path) else dto.Id
                                      Description = if isNull dto.Description || dto.Description = "" then None else Some dto.Description
                                      Items =
                                        if isNull dto.Items then []
                                        else dto.Items |> Array.toList })

                    let errors =
                        results
                        |> List.choose (function
                            | Error e -> Some e
                            | Ok _ -> None)

                    match errors with
                    | e :: _ -> Error e
                    | [] ->
                        Ok(
                            results
                            |> List.choose (function
                                | Ok v -> Some v
                                | Error _ -> None)
                        )
                with ex ->
                    Error(ProductConfigParseError(viewsDir, ex.Message))
