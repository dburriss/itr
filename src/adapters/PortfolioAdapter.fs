module Itr.Adapters.PortfolioAdapter

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open System.Collections.Generic
open Itr.Domain

[<CLIMutable>]
type ProductRefDto =
    { id: string
      root: CoordinationRootConfig }

[<CLIMutable>]
type ProfileDto =
    { products: ProductRefDto array
      gitIdentity: GitIdentity option }

[<CLIMutable>]
type PortfolioDto =
    { defaultProfile: string option
      profiles: Dictionary<string, ProfileDto> }

type CoordinationRootConfigConverter() =
    inherit JsonConverter<CoordinationRootConfig>()

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, _options: JsonSerializerOptions) =
        use document = JsonDocument.ParseValue(&reader)
        let root = document.RootElement

        let mode =
            match root.TryGetProperty("mode") with
            | true, element -> element.GetString()
            | false, _ -> raise (JsonException("Missing required field 'mode' in product root."))

        match mode with
        | "standalone" ->
            match root.TryGetProperty("dir") with
            | true, element -> StandaloneConfig(element.GetString())
            | false, _ -> raise (JsonException("mode 'standalone' requires field 'dir'."))
        | "primary-repo" ->
            match root.TryGetProperty("repoDir") with
            | true, element -> PrimaryRepoConfig(element.GetString())
            | false, _ -> raise (JsonException("mode 'primary-repo' requires field 'repoDir'."))
        | "control-repo" ->
            match root.TryGetProperty("repoDir") with
            | true, element -> ControlRepoConfig(element.GetString())
            | false, _ -> raise (JsonException("mode 'control-repo' requires field 'repoDir'."))
        | null -> raise (JsonException("Field 'mode' cannot be null."))
        | value -> raise (JsonException($"Unknown coordination mode '{value}'."))

    override _.Write(writer: Utf8JsonWriter, value: CoordinationRootConfig, options: JsonSerializerOptions) =
        writer.WriteStartObject()

        match value with
        | StandaloneConfig dir ->
            writer.WriteString("mode", "standalone")
            writer.WriteString("dir", dir)
        | PrimaryRepoConfig repoDir ->
            writer.WriteString("mode", "primary-repo")
            writer.WriteString("repoDir", repoDir)
        | ControlRepoConfig repoDir ->
            writer.WriteString("mode", "control-repo")
            writer.WriteString("repoDir", repoDir)

        writer.WriteEndObject()

let private expandUnixEnvVars (value: string) =
    let replaceToken (matchValue: Match) =
        let name =
            if matchValue.Groups.[1].Success then
                matchValue.Groups.[1].Value
            else
                matchValue.Groups.[2].Value

        match Environment.GetEnvironmentVariable(name) with
        | null -> matchValue.Value
        | resolved -> resolved

    Regex.Replace(value, "\$\{([A-Za-z_][A-Za-z0-9_]*)\}|\$([A-Za-z_][A-Za-z0-9_]*)", MatchEvaluator replaceToken)

let expandPath (homeDir: string) (value: string) : string =
    if String.IsNullOrWhiteSpace(value) then
        value
    else
        let expandedHome =
            if value = "~" then
                homeDir
            elif value.StartsWith("~/", StringComparison.Ordinal) then
                Path.Combine(homeDir, value.Substring(2))
            else
                value

        expandedHome |> expandUnixEnvVars |> Environment.ExpandEnvironmentVariables

let private normalizeCoordRootPath homeDir rootConfig =
    match rootConfig with
    | StandaloneConfig dir -> StandaloneConfig(expandPath homeDir dir)
    | PrimaryRepoConfig repoDir -> PrimaryRepoConfig(expandPath homeDir repoDir)
    | ControlRepoConfig repoDir -> ControlRepoConfig(expandPath homeDir repoDir)

let private mapProduct homeDir (dto: ProductRefDto) =
    ProductId.tryCreate dto.id
    |> Result.map (fun productId ->
        { Id = productId
          Root = normalizeCoordRootPath homeDir dto.root })

let private mapProfile homeDir (name: string) (dto: ProfileDto) =
    let products = if isNull dto.products then [||] else dto.products

    products
    |> Array.toList
    |> List.fold
        (fun state productDto ->
            state
            |> Result.bind (fun acc -> mapProduct homeDir productDto |> Result.map (fun product -> product :: acc)))
        (Ok [])
    |> Result.map (fun products ->
        { Name = ProfileName.create name
          Products = List.rev products
          GitIdentity = dto.gitIdentity })

let private mapPortfolio homeDir (dto: PortfolioDto) =
    if isNull dto.profiles then
        Error(ConfigParseError("<memory>", "Missing required object 'profiles'."))
    else
        dto.profiles
        |> Seq.toList
        |> List.fold
            (fun state kvp ->
                state
                |> Result.bind (fun acc ->
                    mapProfile homeDir kvp.Key kvp.Value
                    |> Result.map (fun profile -> profile :: acc)))
            (Ok [])
        |> Result.bind (fun profiles ->
            let defaultProfile = dto.defaultProfile |> Option.map ProfileName.create
            Portfolio.tryCreate defaultProfile (List.rev profiles))

let readConfig (homeDir: string) (path: string) : Result<Portfolio, PortfolioError> =
    let resolvedPath = expandPath homeDir path

    if not (File.Exists(resolvedPath)) then
        Error(ConfigNotFound resolvedPath)
    else
        try
            let json = File.ReadAllText(resolvedPath)

            let options = JsonSerializerOptions()
            options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
            options.PropertyNameCaseInsensitive <- true
            options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
            options.Converters.Add(CoordinationRootConfigConverter())

            let dto = JsonSerializer.Deserialize<PortfolioDto>(json, options) |> Option.ofObj

            match dto with
            | None -> Error(ConfigParseError(resolvedPath, "Config file was empty or invalid."))
            | Some model ->
                mapPortfolio homeDir model
                |> Result.mapError (function
                    | ConfigParseError(_, message) -> ConfigParseError(resolvedPath, message)
                    | other -> other)
        with ex ->
            Error(ConfigParseError(resolvedPath, ex.Message))

/// Create a PortfolioConfigAdapter that implements IPortfolioConfig
type PortfolioConfigAdapter(env: IEnvironment) =
    let homeDir = env.HomeDirectory()

    interface IPortfolioConfig with
        member _.ConfigPath() =
            match env.GetEnvVar "ITR_HOME" with
            | Some itrHome -> Path.Combine(expandPath homeDir itrHome, "itr.json")
            | None -> Path.Combine(homeDir, ".config", "itr", "itr.json")

        member _.LoadConfig path = readConfig homeDir path
