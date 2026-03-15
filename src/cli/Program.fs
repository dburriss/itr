module Itr.Cli.Program

open Argu
open Itr.Domain
open Itr.Features
open Itr.Adapters

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"

/// Composition root - combines all adapters into a single deps object
type AppDeps() =
    let envAdapter = EnvironmentAdapter()
    let fsAdapter = FileSystemAdapter()
    let portfolioConfigAdapter = PortfolioAdapter.PortfolioConfigAdapter(envAdapter)

    interface IEnvironment with
        member _.GetEnvVar name =
            (envAdapter :> IEnvironment).GetEnvVar name

        member _.HomeDirectory() =
            (envAdapter :> IEnvironment).HomeDirectory()

    interface IFileSystem with
        member _.ReadFile path =
            (fsAdapter :> IFileSystem).ReadFile path

        member _.WriteFile path content =
            (fsAdapter :> IFileSystem).WriteFile path content

        member _.FileExists path =
            (fsAdapter :> IFileSystem).FileExists path

        member _.DirectoryExists path =
            (fsAdapter :> IFileSystem).DirectoryExists path

    interface IPortfolioConfig with
        member _.ConfigPath() =
            (portfolioConfigAdapter :> IPortfolioConfig).ConfigPath()

        member _.LoadConfig path =
            (portfolioConfigAdapter :> IPortfolioConfig).LoadConfig path

let private dispatch (deps: AppDeps) (results: ParseResults<CliArgs>) =
    let profile = results.TryGetResult Profile

    // Run the effect pipeline
    let loadResult = Portfolio.loadPortfolio None |> Effect.run deps

    loadResult
    |> Result.bind (fun portfolio -> Portfolio.resolveActiveProfile portfolio profile |> Effect.run deps)

[<EntryPoint>]
let main argv =
    let deps = AppDeps()
    let parser = ArgumentParser.Create<CliArgs>(programName = "itr")

    try
        let results = parser.Parse argv

        match dispatch deps results with
        | Ok _ ->
            printfn "itr cli"
            0
        | Error err ->
            printfn "%A" err
            1
    with :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1
