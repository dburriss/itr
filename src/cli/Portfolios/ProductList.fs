module Itr.Cli.Portfolios.ProductList

open Argu
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (configPath: string)
    (deps: #IPortfolioConfig & #IEnvironment & #IProductConfig & #IFileSystem)
    (listArgs: ParseResults<ProductListArgs>)
    : Result<unit, string> =
    let portfolioResult =
        Portfolios.Query.load (Some configPath)
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match portfolioResult with
    | Error msg -> Error msg
    | Ok portfolio ->
        if portfolio.Profiles.IsEmpty then
            Error "No profiles found. Run 'itr profile add <name>' to create one."
        else
            let flagProfile = listArgs.TryGetResult ProductListArgs.Profile
            let format = listArgs.TryGetResult ProductListArgs.Output |> OutputFormat.tryParse

            match Portfolios.Query.resolveActiveProfile portfolio flagProfile |> Effect.run deps with
            | Error e -> Error(formatPortfolioError e)
            | Ok profile ->
                match Portfolios.Query.loadAllDefinitions profile deps with
                | Error e -> Error(formatPortfolioError e)
                | Ok pairs ->
                    let rows: ProductRow list =
                        pairs
                        |> List.map (fun (_, definition) ->
                            { Id = ProductId.value definition.Id
                              RepoCount = definition.Repos.Count
                              CoordRoot = definition.CoordRoot.AbsolutePath })

                    PortfolioFormatter.formatProductList format rows
                    Ok()
