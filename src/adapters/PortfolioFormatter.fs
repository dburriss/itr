namespace Itr.Adapters

open System
open Spectre.Console
open Itr.Domain
open Itr.Domain.Portfolios

// ---------------------------------------------------------------------------
// Input types for portfolio formatting
// ---------------------------------------------------------------------------

type ProfileRow =
    { Name: string
      IsDefault: bool
      GitName: string
      GitEmail: string
      ProductCount: int }

type ProductRow =
    { Id: string
      RepoCount: int
      CoordRoot: string }

type ProductInfoData =
    { Id: string
      Description: string
      Docs: (string * string) list // key * absPath
      Repos: (string * string * string option) list // key * absPath * url
      CoordMode: string
      CoordRepo: string
      CoordPath: string }

// ---------------------------------------------------------------------------
// PortfolioFormatter
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module PortfolioFormatter =

    /// Format and print a list of profiles.
    let formatProfileList (format: OutputFormat) (profiles: ProfileRow list) : unit =
        match format with
        | Json ->
            let items =
                profiles
                |> List.map (fun p ->
                    let isDefaultStr = if p.IsDefault then "true" else "false"

                    sprintf
                        "  { \"name\": \"%s\", \"isDefault\": %s, \"gitName\": \"%s\", \"gitEmail\": \"%s\", \"productCount\": %d }"
                        p.Name
                        isDefaultStr
                        p.GitName
                        p.GitEmail
                        p.ProductCount)
                |> String.concat ",\n"

            printfn "["

            if not profiles.IsEmpty then
                printfn "%s" items

            printfn "]"
        | Text ->
            profiles
            |> List.iter (fun p ->
                let marker = if p.IsDefault then "*" else " "
                printfn "%s\t%s\t%s\t%s\t%d" marker p.Name p.GitName p.GitEmail p.ProductCount)
        | Table ->
            let table = Table()
            table.AddColumn("Default") |> ignore
            table.AddColumn("Name") |> ignore
            table.AddColumn("Git Name") |> ignore
            table.AddColumn("Git Email") |> ignore
            table.AddColumn("Products") |> ignore

            profiles
            |> List.iter (fun p ->
                let marker = if p.IsDefault then "*" else ""

                table.AddRow(marker, p.Name, p.GitName, p.GitEmail, string p.ProductCount)
                |> ignore)

            AnsiConsole.Write(table)

    /// Format and print a list of products.
    let formatProductList (format: OutputFormat) (rows: ProductRow list) : unit =
        match format with
        | Json ->
            let items =
                rows
                |> List.map (fun r ->
                    sprintf
                        "  { \"id\": \"%s\", \"repoCount\": %d, \"coordRoot\": \"%s\" }"
                        r.Id
                        r.RepoCount
                        r.CoordRoot)
                |> String.concat ",\n"

            printfn "["

            if not rows.IsEmpty then
                printfn "%s" items

            printfn "]"
        | Text -> rows |> List.iter (fun r -> printfn "%s\t%d\t%s" r.Id r.RepoCount r.CoordRoot)
        | Table ->
            let table = Table()
            table.AddColumn("Id") |> ignore
            table.AddColumn("Repo Count") |> ignore
            table.AddColumn("Coord Root") |> ignore

            rows
            |> List.iter (fun r -> table.AddRow(r.Id, string r.RepoCount, r.CoordRoot) |> ignore)

            AnsiConsole.Write(table)

    /// Format and print product info.
    let formatProductInfo (format: OutputFormat) (data: ProductInfoData) : unit =
        match format with
        | Json ->
            let docsJson =
                data.Docs
                |> List.map (fun (key, absPath) -> sprintf "    \"%s\": \"%s\"" key (absPath.Replace("\\", "\\\\")))
                |> String.concat ",\n"

            let reposJson =
                data.Repos
                |> List.map (fun (key, absPath, url) ->
                    match url with
                    | Some u ->
                        sprintf
                            "    \"%s\": { \"path\": \"%s\", \"url\": \"%s\" }"
                            key
                            (absPath.Replace("\\", "\\\\"))
                            u
                    | None -> sprintf "    \"%s\": { \"path\": \"%s\" }" key (absPath.Replace("\\", "\\\\")))
                |> String.concat ",\n"

            let descriptionJson = data.Description.Replace("\"", "\\\"")
            printfn "{"
            printfn "  \"id\": \"%s\"," data.Id
            printfn "  \"description\": \"%s\"," descriptionJson
            printfn "  \"docs\": {"

            if not data.Docs.IsEmpty then
                printfn "%s" docsJson

            printfn "  },"
            printfn "  \"repos\": {"

            if not data.Repos.IsEmpty then
                printfn "%s" reposJson

            printfn "  },"
            printfn "  \"coordMode\": \"%s\"," data.CoordMode
            printfn "  \"coordRepo\": \"%s\"," data.CoordRepo
            printfn "  \"coordPath\": \"%s\"" data.CoordPath
            printfn "}"
        | Text ->
            printfn "id\t%s" data.Id
            printfn "description\t%s" data.Description

            data.Docs
            |> List.iter (fun (key, absPath) -> printfn "docs\t%s\t%s" key absPath)

            data.Repos
            |> List.iter (fun (key, absPath, _) -> printfn "repos\t%s\t%s" key absPath)

            printfn "coordMode\t%s" data.CoordMode
            printfn "coordRepo\t%s" data.CoordRepo
            printfn "coordPath\t%s" data.CoordPath
        | Table ->
            let table = Table()
            table.AddColumn("Field") |> ignore
            table.AddColumn("Key") |> ignore
            table.AddColumn("Value") |> ignore
            table.AddRow("id", "", data.Id) |> ignore
            table.AddRow("description", "", data.Description) |> ignore

            data.Docs
            |> List.iter (fun (key, absPath) -> table.AddRow("docs", key, absPath) |> ignore)

            data.Repos
            |> List.iter (fun (key, absPath, _) -> table.AddRow("repos", key, absPath) |> ignore)

            table.AddRow("coordMode", "", data.CoordMode) |> ignore
            table.AddRow("coordRepo", "", data.CoordRepo) |> ignore
            table.AddRow("coordPath", "", data.CoordPath) |> ignore
            AnsiConsole.Write(table)
