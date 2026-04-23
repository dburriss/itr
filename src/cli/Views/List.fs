module Itr.Cli.Views.List

open Argu
open Spectre.Console
open Itr.Domain
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (deps: #IViewStore & #IBacklogStore)
    (resolved: ResolvedProduct)
    (listArgs: ParseResults<ViewListArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let viewStore = deps :> IViewStore
    let backlogStore = deps :> IBacklogStore
    let format = listArgs.TryGetResult ViewListArgs.Output |> OutputFormat.tryParse

    match viewStore.ListViews coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok views ->
        match backlogStore.ListArchivedBacklogItems coordRoot with
        | Error e -> Error(formatBacklogError e)
        | Ok archivedItems ->
            let archivedIds =
                archivedItems |> List.map (fun (item, _) -> BacklogId.value item.Id) |> Set.ofList

            if views.IsEmpty then
                match format with
                | Json -> printfn "[]"
                | Text -> () // no output for empty in text mode
                | Table -> printfn "No views defined."
            else
                let rows =
                    views
                    |> List.map (fun view ->
                        let description = view.Description |> Option.defaultValue ""
                        let total = view.Items.Length
                        let archived =
                            view.Items
                            |> List.filter (fun id -> archivedIds.Contains(id))
                            |> List.length
                        (view.Id, description, total, archived))

                match format with
                | Json ->
                    let items =
                        rows
                        |> List.map (fun (id, description, total, archived) ->
                            let descJson = description.Replace("\"", "\\\"")
                            sprintf "  { \"id\": \"%s\", \"description\": \"%s\", \"items\": %d, \"archived\": %d }"
                                id descJson total archived)
                        |> String.concat ",\n"
                    printfn "["
                    printfn "%s" items
                    printfn "]"
                | Text ->
                    rows
                    |> List.iter (fun (id, description, total, archived) ->
                        printfn "%s\t%s\t%d\t%d" id description total archived)
                | Table ->
                    let table = Spectre.Console.Table()
                    table.AddColumn(Spectre.Console.TableColumn("Id")) |> ignore
                    table.AddColumn(Spectre.Console.TableColumn("Description")) |> ignore
                    table.AddColumn(Spectre.Console.TableColumn("Items")) |> ignore
                    table.AddColumn(Spectre.Console.TableColumn("Archived")) |> ignore

                    rows
                    |> List.iter (fun (id, description, total, archived) ->
                        table.AddRow(id, description, string total, string archived) |> ignore)

                    Spectre.Console.AnsiConsole.Write(table)

            Ok()
