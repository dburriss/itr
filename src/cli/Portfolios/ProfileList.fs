module Itr.Cli.Portfolios.ProfileList

open Argu
open Itr.Domain
open Itr.Adapters
open Itr.Cli.CliArgs

let handle
    (portfolio: Portfolio)
    (listArgs: ParseResults<ProfileListArgs>)
    : Result<unit, string> =
    let format = listArgs.TryGetResult ProfileListArgs.Output |> OutputFormat.tryParse

    let profiles =
        portfolio.Profiles
        |> Map.toList
        |> List.map (fun (name, profile) ->
            let nameStr = ProfileName.value name
            let isDefault =
                match portfolio.DefaultProfile with
                | Some d -> d = name
                | None -> false
            let gitName =
                profile.GitIdentity |> Option.map (fun g -> g.Name) |> Option.defaultValue ""
            let gitEmail =
                profile.GitIdentity
                |> Option.bind (fun g -> g.Email)
                |> Option.defaultValue ""
            let productCount = profile.Products.Length
            ({ Name = nameStr
               IsDefault = isDefault
               GitName = gitName
               GitEmail = gitEmail
               ProductCount = productCount } : ProfileRow))

    PortfolioFormatter.formatProfileList format profiles
    Ok()
