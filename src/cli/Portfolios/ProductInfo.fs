module Itr.Cli.Portfolios.ProductInfo

open System
open Argu
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (configPath: string)
    (deps: #IPortfolioConfig & #IEnvironment & #IProductConfig & #IFileSystem)
    (infoArgs: ParseResults<ProductInfoArgs>)
    : Result<unit, string> =
    let format = infoArgs.TryGetResult ProductInfoArgs.Output |> OutputFormat.tryParse
    let productConfig = deps :> IProductConfig
    let fileSystem = deps :> IFileSystem

    let definitionResult: Result<string * ProductDefinition, string> =
        match infoArgs.TryGetResult ProductInfoArgs.Product_Id with
        | Some rawId ->
            let portfolioResult =
                Portfolios.Query.load (Some configPath)
                |> Effect.run deps
                |> Result.mapError formatPortfolioError

            match portfolioResult with
            | Error msg -> Error msg
            | Ok portfolio ->
                match Portfolios.Query.resolveActiveProfile portfolio None |> Effect.run deps with
                | Error e -> Error(formatPortfolioError e)
                | Ok activeProfile ->
                    match Portfolios.Query.loadAllDefinitions activeProfile deps with
                    | Error e -> Error(formatPortfolioError e)
                    | Ok pairs ->
                        match pairs |> List.tryFind (fun (_, def) -> ProductId.value def.Id = rawId) with
                        | None -> Error $"Product '{rawId}' not found in active profile."
                        | Some(productRef, definition) ->
                            let (ProductRoot root) = productRef.Root
                            Ok(root, definition)
        | None ->
            let cwd = IO.Directory.GetCurrentDirectory()

            match ProductLocator.locateProductRoot fileSystem cwd with
            | None ->
                Error "No product ID provided and no product.yaml found in current directory or any parent directory."
            | Some productRoot ->
                match productConfig.LoadProductConfig productRoot with
                | Error e -> Error(formatPortfolioError e)
                | Ok definition -> Ok(productRoot, definition)

    match definitionResult with
    | Error msg -> Error msg
    | Ok(productRoot, definition) ->
        let id = ProductId.value definition.Id
        let description = definition.Description |> Option.defaultValue ""

        let docs =
            definition.Docs
            |> Map.toList
            |> List.map (fun (key, relPath) ->
                let absPath = IO.Path.GetFullPath(IO.Path.Combine(productRoot, relPath))
                key, absPath)

        let repos =
            definition.Repos
            |> Map.toList
            |> List.map (fun (key, repoConfig) ->
                let absPath = IO.Path.GetFullPath(IO.Path.Combine(productRoot, repoConfig.Path))
                key, absPath, repoConfig.Url)

        let coordMode = definition.Coordination.Mode
        let coordRepo = definition.Coordination.Repo |> Option.defaultValue ""
        let coordPath = definition.Coordination.Path |> Option.defaultValue ""

        let data: ProductInfoData =
            { Id = id
              Description = description
              Docs = docs
              Repos = repos
              CoordMode = coordMode
              CoordRepo = coordRepo
              CoordPath = coordPath }

        PortfolioFormatter.formatProductInfo format data
        Ok()
