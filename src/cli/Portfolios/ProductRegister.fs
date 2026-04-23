module Itr.Cli.Portfolios.ProductRegister

open Argu
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (deps: #IPortfolioConfig & #IProductConfig & #IFileSystem)
    (configPath: string)
    (profile: string option)
    (registerArgs: ParseResults<ProductRegisterArgs>)
    (format: OutputFormat)
    : Result<unit, string> =
    let path = registerArgs.GetResult ProductRegisterArgs.Path

    let input: Portfolios.RegisterProduct.Input = { Path = path; Profile = profile }

    let result =
        Portfolios.RegisterProduct.execute configPath input
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match result with
    | Error msg -> Error msg
    | Ok updatedPortfolio ->
        let portfolioConfig = deps :> IPortfolioConfig

        portfolioConfig.SaveConfig configPath updatedPortfolio
        |> Result.mapError formatPortfolioError
        |> Result.map (fun () ->
            let productConfig = deps :> IProductConfig

            match productConfig.LoadProductConfig path with
            | Ok definition ->
                let id = ProductId.value definition.Id

                match format with
                | Json -> printfn """{ "ok": true, "id": "%s", "path": "%s" }""" id path
                | _ -> printfn "Registered product '%s' from '%s'." id path
            | Error _ ->
                match format with
                | Json -> printfn """{ "ok": true, "path": "%s" }""" path
                | _ -> printfn "Registered product from '%s'." path)
