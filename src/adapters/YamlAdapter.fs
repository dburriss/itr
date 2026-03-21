module Itr.Adapters.YamlAdapter

open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
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
                            if isNull dto.Repos then
                                []
                            else
                                dto.Repos |> Array.toList |> List.map RepoId

                        Ok
                            { Id = backlogId
                              Title = if isNull dto.Title then id else dto.Title
                              Repos = repos }
                with ex ->
                    Error(ProductConfigParseError(path, ex.Message))

        member _.ArchiveBacklogItem (coordRoot: string) (backlogId: BacklogId) (date: string) =
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
                            | Error msg -> Error(ProductConfigParseError(path, msg))
                            | Ok dto -> mapTaskDto dto)

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
                                | Ok t -> Some t
                                | Error _ -> None)
                        )
                with ex ->
                    Error(ProductConfigParseError(dir, ex.Message))

        member _.WriteTask (coordRoot: string) (task: ItrTask) =
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

        member _.ArchiveTask (coordRoot: string) (backlogId: BacklogId) (taskId: TaskId) (date: string) =
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
