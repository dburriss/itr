module Itr.Adapters.YamlAdapter

open System
open System.IO
open System.Collections.Generic
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
type ProductConfigDto =
    { [<YamlMember(Alias = "id")>]
      Id: string
      [<YamlMember(Alias = "repos")>]
      Repos: Dictionary<string, RepoConfigDto> }

[<CLIMutable>]
type BacklogItemDto =
    { [<YamlMember(Alias = "id")>]
      Id: string
      [<YamlMember(Alias = "title")>]
      Title: string
      [<YamlMember(Alias = "repos")>]
      Repos: string array }

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
    DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build()

let private serializer =
    SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build()

let private parseYaml<'a> (content: string) : Result<'a, string> =
    try
        Ok(deserializer.Deserialize<'a>(content))
    with ex ->
        Error ex.Message

let private serializeYaml<'a> (value: 'a) : string = serializer.Serialize(value)

// ---------------------------------------------------------------------------
// Domain mapping helpers
// ---------------------------------------------------------------------------

let private mapTaskState (s: string) : TaskState =
    match s with
    | "in_progress" -> InProgress
    | "implemented" -> Implemented
    | "validated" -> Validated
    | _ -> Planning

let private taskStateToString (state: TaskState) : string =
    match state with
    | Planning -> "planning"
    | InProgress -> "in_progress"
    | Implemented -> "implemented"
    | Validated -> "validated"

let private mapTaskDto (dto: ItrTaskDto) : Result<ItrTask, TakeError> =
    match BacklogId.tryCreate dto.Source.Backlog with
    | Error _ -> Error(ProductConfigParseError("<task>", $"Invalid backlog id: {dto.Source.Backlog}"))
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
        member _.LoadProductConfig(coordRoot: string) =
            let path = Path.Combine(coordRoot, "product.yaml")

            if not (File.Exists(path)) then
                Error(ProductConfigNotFound coordRoot)
            else
                try
                    let content = File.ReadAllText(path)

                    match parseYaml<ProductConfigDto> content with
                    | Error msg -> Error(ProductConfigParseError(path, msg))
                    | Ok dto when isNull dto.Id ->
                        Error(ProductConfigParseError(path, "Missing required field 'id'"))
                    | Ok dto ->
                        match ProductId.tryCreate dto.Id with
                        | Error _ -> Error(ProductConfigParseError(path, $"Invalid product id: {dto.Id}"))
                        | Ok productId ->
                            let repos =
                                if isNull dto.Repos then
                                    Map.empty
                                else
                                     dto.Repos
                                     |> Seq.map (fun kvp ->
                                         let url =
                                             if isNull kvp.Value.Url || kvp.Value.Url = "" then
                                                 None
                                             else
                                                 Some kvp.Value.Url

                                         RepoId kvp.Key, ({ Path = kvp.Value.Path; Url = url } : RepoConfig))
                                    |> Map.ofSeq

                            Ok { Id = productId; Repos = repos }
                with ex ->
                    Error(ProductConfigParseError(path, ex.Message))

// ---------------------------------------------------------------------------
// IBacklogStore implementation
// ---------------------------------------------------------------------------

type BacklogStoreAdapter() =
    interface IBacklogStore with
        member _.LoadBacklogItem(coordRoot: string) (backlogId: BacklogId) =
            let id = BacklogId.value backlogId
            let path = Path.Combine(coordRoot, "BACKLOG", id, "item.yaml")

            if not (File.Exists(path)) then
                Error(BacklogItemNotFound backlogId)
            else
                try
                    let content = File.ReadAllText(path)

                    match parseYaml<BacklogItemDto> content with
                    | Error msg -> Error(ProductConfigParseError(path, msg))
                    | Ok dto ->
                        let repos =
                            if isNull dto.Repos then [] else dto.Repos |> Array.toList |> List.map RepoId

                        Ok
                            { Id = backlogId
                              Title = if isNull dto.Title then id else dto.Title
                              Repos = repos }
                with ex ->
                    Error(ProductConfigParseError(path, ex.Message))

        member _.ArchiveBacklogItem(coordRoot: string) (backlogId: BacklogId) (date: string) =
            let id = BacklogId.value backlogId
            let sourcePath = Path.Combine(coordRoot, "BACKLOG", id)
            let archiveDir = Path.Combine(coordRoot, "BACKLOG", "archive")
            let destPath = Path.Combine(archiveDir, $"{date}-{id}")

            if not (Directory.Exists(sourcePath)) then
                Error(BacklogItemNotFound backlogId)
            else
                try
                    Directory.CreateDirectory(archiveDir) |> ignore
                    Directory.Move(sourcePath, destPath)
                    Ok()
                with ex ->
                    Error(ProductConfigParseError(sourcePath, ex.Message))

// ---------------------------------------------------------------------------
// ITaskStore implementation
// ---------------------------------------------------------------------------

type TaskStoreAdapter() =
    interface ITaskStore with
        member _.ListTasks(coordRoot: string) (backlogId: BacklogId) =
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
                            | Error msg -> Error(ProductConfigParseError(path, msg))
                            | Ok dto -> mapTaskDto dto)

                    let errors = results |> List.choose (function | Error e -> Some e | Ok _ -> None)

                    match errors with
                    | e :: _ -> Error e
                    | [] -> Ok(results |> List.choose (function | Ok t -> Some t | Error _ -> None))
                with ex ->
                    Error(ProductConfigParseError(dir, ex.Message))

        member _.WriteTask(coordRoot: string) (task: ItrTask) =
            let taskId = TaskId.value task.Id
            let backlogId = BacklogId.value task.SourceBacklog
            let taskDir = Path.Combine(coordRoot, "BACKLOG", backlogId, "tasks", taskId)
            let path = Path.Combine(taskDir, "task.yaml")

            try
                Directory.CreateDirectory(taskDir) |> ignore

                let dto = taskToDto task
                let yaml = serializeYaml dto
                File.WriteAllText(path, yaml)
                Ok()
            with ex ->
                Error(ProductConfigParseError(path, ex.Message))

        member _.ArchiveTask(coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) (date: string) =
            let bid = BacklogId.value backlogId
            let tid = TaskId.value taskId
            let sourcePath = Path.Combine(coordRoot, "BACKLOG", bid, "tasks", tid)
            let destPath = Path.Combine(coordRoot, "BACKLOG", bid, "tasks", $"{date}-{tid}")

            if not (Directory.Exists(sourcePath)) then
                Error(ProductConfigParseError(sourcePath, $"Task folder not found: {sourcePath}"))
            else
                try
                    Directory.Move(sourcePath, destPath)
                    Ok()
                with ex ->
                    Error(ProductConfigParseError(sourcePath, ex.Message))
