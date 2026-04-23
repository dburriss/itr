module Itr.Cli.Shared.Rendering

open Itr.Adapters

/// Render a Spectre table with the given columns and rows.
let renderTable (columns: string list) (addRows: Spectre.Console.Table -> unit) =
    let table = Spectre.Console.Table()
    columns |> List.iter (fun c -> table.AddColumn(Spectre.Console.TableColumn(c)) |> ignore)
    addRows table
    Spectre.Console.AnsiConsole.Write(table)

/// Print JSON array output for a list of already-formatted JSON item strings.
let printJsonArray (items: string list) =
    printfn "["
    printfn "%s" (items |> String.concat ",\n")
    printfn "]"

/// Try to parse an output format string into OutputFormat.
let tryParseOutputFormat (s: string option) : OutputFormat =
    OutputFormat.tryParse s
