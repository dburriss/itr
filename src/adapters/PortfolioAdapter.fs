module Itr.Adapters.PortfolioAdapter

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open System.Collections.Generic
open Itr.Domain

[<CLIMutable>]
type AgentConfigDto =
    { protocol: string
      command: string
      args: string array }

[<CLIMutable>]
type ProfileDto =
    { products: string array
      gitIdentity: GitIdentity option
      agent: AgentConfigDto option }

[<CLIMutable>]
type PortfolioDto =
    { defaultProfile: string option
      profiles: Dictionary<string, ProfileDto> }

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
                System.IO.Path.Combine(homeDir, value.Substring(2))
            else
                value

        expandedHome |> expandUnixEnvVars |> Environment.ExpandEnvironmentVariables

let private defaultAgentConfig () : AgentConfig =
    { Protocol = "opencode-http"; Command = "opencode"; Args = [] }

let private mapAgentConfig (dto: AgentConfigDto option) : AgentConfig =
    match dto with
    | None -> defaultAgentConfig ()
    | Some d ->
        let protocol = if String.IsNullOrWhiteSpace(d.protocol) then "opencode-http" else d.protocol
        let command = if String.IsNullOrWhiteSpace(d.command) then "opencode" else d.command
        let args = if isNull d.args then [] else d.args |> Array.toList
        { Protocol = protocol; Command = command; Args = args }

let private mapProduct homeDir (rootPath: string) : ProductRef =
    { Root = ProductRoot(expandPath homeDir rootPath) }

let private mapProfile homeDir (name: string) (dto: ProfileDto) =
    let products = if isNull dto.products then [||] else dto.products

    let productRefs = products |> Array.toList |> List.map (mapProduct homeDir)

    { Name = ProfileName.create name
      Products = productRefs
      GitIdentity = dto.gitIdentity
      AgentConfig = mapAgentConfig dto.agent }

let private mapPortfolio homeDir (dto: PortfolioDto) =
    if isNull dto.profiles then
        Error(ConfigParseError("<memory>", "Missing required object 'profiles'."))
    else
        let profiles =
            dto.profiles
            |> Seq.toList
            |> List.map (fun kvp -> mapProfile homeDir kvp.Key kvp.Value)

        let defaultProfile = dto.defaultProfile |> Option.map ProfileName.create
        Portfolio.tryCreate defaultProfile profiles

let private jsonOptions () =
    let options = JsonSerializerOptions()
    options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    options.PropertyNameCaseInsensitive <- true
    options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
    options

let readConfig (fs: IFileSystem) (homeDir: string) (path: string) : Result<Portfolio, PortfolioError> =
    let resolvedPath = expandPath homeDir path

    if not (fs.FileExists(resolvedPath)) then
        Error(ConfigNotFound resolvedPath)
    else
        match fs.ReadFile(resolvedPath) with
        | Error ioErr ->
            let msg =
                match ioErr with
                | IoException(_, m) -> m
                | FileNotFound p -> $"File not found: {p}"
                | DirectoryNotFound p -> $"Directory not found: {p}"

            Error(ConfigParseError(resolvedPath, msg))
        | Ok json ->
            try
                let dto =
                    JsonSerializer.Deserialize<PortfolioDto>(json, jsonOptions ()) |> Option.ofObj

                match dto with
                | None -> Error(ConfigParseError(resolvedPath, "Config file was empty or invalid."))
                | Some model ->
                    mapPortfolio homeDir model
                    |> Result.mapError (function
                        | ConfigParseError(_, message) -> ConfigParseError(resolvedPath, message)
                        | other -> other)
            with ex ->
                Error(ConfigParseError(resolvedPath, ex.Message))

let writeConfig (fs: IFileSystem) (homeDir: string) (path: string) (portfolio: Portfolio) : Result<unit, PortfolioError> =
    let resolvedPath = expandPath homeDir path

    try
        let profiles = Dictionary<string, ProfileDto>()

        for kvp in portfolio.Profiles do
            let name = ProfileName.value kvp.Key
            let profile = kvp.Value

            let products =
                profile.Products
                |> List.map (fun p -> let (ProductRoot root) = p.Root in root)
                |> List.toArray

            let agentDto =
                { protocol = profile.AgentConfig.Protocol
                  command = profile.AgentConfig.Command
                  args = profile.AgentConfig.Args |> List.toArray }

            profiles.[name] <-
                { products = products
                  gitIdentity = profile.GitIdentity
                  agent = Some agentDto }

        let dto =
            { defaultProfile = portfolio.DefaultProfile |> Option.map ProfileName.value
              profiles = profiles }

        let writeOptions = jsonOptions ()
        writeOptions.WriteIndented <- true
        let json = JsonSerializer.Serialize(dto, writeOptions)

        fs.WriteFile resolvedPath json
        |> Result.mapError (fun ioErr ->
            let msg =
                match ioErr with
                | IoException(_, m) -> m
                | FileNotFound p -> $"File not found: {p}"
                | DirectoryNotFound p -> $"Directory not found: {p}"

            ConfigParseError(resolvedPath, msg))
    with ex ->
        Error(ConfigParseError(resolvedPath, ex.Message))

/// Create a PortfolioConfigAdapter that implements IPortfolioConfig
type PortfolioConfigAdapter(env: IEnvironment, fs: IFileSystem) =
    let homeDir = env.HomeDirectory()

    interface IPortfolioConfig with
        member _.ConfigPath() =
            match env.GetEnvVar "ITR_HOME" with
            | Some itrHome -> System.IO.Path.Combine(expandPath homeDir itrHome, "itr.json")
            | None -> System.IO.Path.Combine(homeDir, ".config", "itr", "itr.json")

        member _.LoadConfig path = readConfig fs homeDir path

        member _.SaveConfig path portfolio = writeConfig fs homeDir path portfolio

[<CLIMutable>]
type LocalConfigDto = { agent: AgentConfigDto option }

/// Read the optional `<productRoot>/itr.json` and extract the `agent` section.
/// Returns `Some AgentConfig` if the file exists and contains an `agent` section;
/// returns `None` if the file is absent or has no `agent` section.
let LoadLocalConfig (productRoot: string) : AgentConfig option =
    let path = System.IO.Path.Combine(productRoot, "itr.json")

    if not (System.IO.File.Exists(path)) then
        None
    else
        try
            let json = System.IO.File.ReadAllText(path)
            let dto = JsonSerializer.Deserialize<LocalConfigDto>(json, jsonOptions ()) |> Option.ofObj

            match dto with
            | None -> None
            | Some d -> d.agent |> Option.map (fun a -> mapAgentConfig (Some a))
        with _ ->
            None
