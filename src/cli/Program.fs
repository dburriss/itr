module Itr.Cli.Program

open Argu
open Itr.Commands
open Itr.Adapters

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"

let private dispatch (results: ParseResults<CliArgs>) =
    let profile = results.TryGetResult Profile
    let _output = results.TryGetResult Output

    Portfolio.loadPortfolio None
    |> Result.bind (fun portfolio -> Portfolio.resolveActiveProfile portfolio profile (fun key -> System.Environment.GetEnvironmentVariable(key) |> Option.ofObj))

[<EntryPoint>]
let main argv =
    PortfolioAdapter.register ()
    let parser = ArgumentParser.Create<CliArgs>(programName = "itr")

    try
        let results = parser.Parse argv

        match dispatch results with
        | Ok _ ->
            printfn "itr cli"
            0
        | Error err ->
            printfn "%A" err
            1
    with :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1
