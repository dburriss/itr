module Itr.Adapters.PortfolioAdapter

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open System.Collections.Generic
open Itr.Domain

[<CLIMutable>]
type ProfileDto =
    { products: string array
      gitIdentity: GitIdentity option }

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
                Path.Combine(homeDir, value.Substring(2))
            else
                value

        expandedHome |> expandUnixEnvVars |> Environment.ExpandEnvironmentVariables

let private mapProduct homeDir (rootPath: string) : ProductRef =
    { Root = ProductRoot(expandPath homeDir rootPath) }

let private mapProfile homeDir (name: string) (dto: ProfileDto) =
    let products = if isNull dto.products then [||] else dto.products

    let productRefs = products |> Array.toList |> List.map (mapProduct homeDir)

    { Name = ProfileName.create name
      Products = productRefs
      GitIdentity = dto.gitIdentity }

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

let readConfig (homeDir: string) (path: string) : Result<Portfolio, PortfolioError> =
    let resolvedPath = expandPath homeDir path

    if not (File.Exists(resolvedPath)) then
        Error(ConfigNotFound resolvedPath)
    else
        try
            let json = File.ReadAllText(resolvedPath)

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

let writeConfig (homeDir: string) (path: string) (portfolio: Portfolio) : Result<unit, PortfolioError> =
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

            profiles.[name] <-
                { products = products
                  gitIdentity = profile.GitIdentity }

        let dto =
            { defaultProfile = portfolio.DefaultProfile |> Option.map ProfileName.value
              profiles = profiles }

        let json = JsonSerializer.Serialize(dto, jsonOptions ())
        let dir = Path.GetDirectoryName(resolvedPath)

        if not (String.IsNullOrEmpty(dir)) then
            Directory.CreateDirectory(dir) |> ignore

        File.WriteAllText(resolvedPath, json)
        Ok()
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
