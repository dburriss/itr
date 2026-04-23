module Itr.Cli.Program

open System
open Argu
open Spectre.Console
open Itr.Domain
open Itr.Domain.Portfolios
open Itr.Domain.Tasks
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliParsers
open Itr.Cli.CliArgs
open Itr.Cli.AppDeps
open Itr.Cli.ErrorFormatting

// ---------------------------------------------------------------------------
// resolvePortfolio helper — shared load→resolveActiveProfile boilerplate
// ---------------------------------------------------------------------------

let private resolvePortfolio
    (deps: #IPortfolioConfig & #IEnvironment)
    (configPath: string)
    (profile: string option)
    : Result<Profile, string> =
    Portfolios.Query.load (Some configPath)
    |> Effect.run deps
    |> Result.mapError (fun e -> $"%A{e}")
    |> Result.bind (fun portfolio ->
        Portfolios.Query.resolveActiveProfile portfolio profile
        |> Effect.run deps
        |> Result.mapError (fun e -> $"%A{e}"))

// ---------------------------------------------------------------------------
// Active patterns for dispatch simplification
// ---------------------------------------------------------------------------

let private (|BacklogTake|_|) (r: ParseResults<BacklogArgs>) = r.TryGetResult BacklogArgs.Take

let private (|BacklogAdd|_|) (r: ParseResults<BacklogArgs>) = r.TryGetResult BacklogArgs.Add

let private (|BacklogList|_|) (r: ParseResults<BacklogArgs>) = r.TryGetResult BacklogArgs.List

let private (|BacklogInfo|_|) (r: ParseResults<BacklogArgs>) = r.TryGetResult BacklogArgs.Info

let private (|ProfileAdd|_|) (r: ParseResults<ProfileArgs>) = r.TryGetResult ProfileArgs.Add

let private (|ProfileList|_|) (r: ParseResults<ProfileArgs>) = r.TryGetResult ProfileArgs.List

let private (|ProfileSetDefault|_|) (r: ParseResults<ProfileArgs>) = r.TryGetResult ProfileArgs.Set_Default

let private (|ProductInit|_|) (r: ParseResults<ProductArgs>) = r.TryGetResult ProductArgs.Init

let private (|ProductRegister|_|) (r: ParseResults<ProductArgs>) = r.TryGetResult ProductArgs.Register

let private (|ProductList|_|) (r: ParseResults<ProductArgs>) = r.TryGetResult ProductArgs.List

let private (|ProductInfo|_|) (r: ParseResults<ProductArgs>) = r.TryGetResult ProductArgs.Info

let private (|TaskList|_|) (r: ParseResults<TaskArgs>) = r.TryGetResult TaskArgs.List

let private (|TaskInfo|_|) (r: ParseResults<TaskArgs>) = r.TryGetResult TaskArgs.Info

let private (|TaskPlan|_|) (r: ParseResults<TaskArgs>) = r.TryGetResult TaskArgs.Plan

let private (|TaskApprove|_|) (r: ParseResults<TaskArgs>) = r.TryGetResult TaskArgs.Approve

let private (|ViewList|_|) (r: ParseResults<ViewArgs>) = r.TryGetResult ViewArgs.List

// ---------------------------------------------------------------------------
// resolveProduct helper — resolve first product from profile
// ---------------------------------------------------------------------------

let private resolveProduct
    (deps: #IProductConfig & #IFileSystem)
    (activeProfile: Profile)
    : Result<ResolvedProduct, string> =
    match activeProfile.Products with
    | [] -> Error "No products in active profile"
    | productRef :: _ ->
        let (ProductRoot root) = productRef.Root
        let productConfig = deps :> IProductConfig

        match productConfig.LoadProductConfig root with
        | Error e -> Error(formatPortfolioError e)
        | Ok definition ->
            Portfolios.Query.resolveProduct activeProfile (ProductId.value definition.Id)
            |> Effect.run deps
            |> Result.mapError (fun e -> $"%A{e}")

// ---------------------------------------------------------------------------
// Dispatch
// ---------------------------------------------------------------------------

let private dispatch (deps: AppDeps) (results: ParseResults<CliArgs>) : Result<unit, string> =
    let profile = results.TryGetResult Profile
    let format = results.TryGetResult Output |> OutputFormat.tryParse

    let configPath = (deps :> IPortfolioConfig).ConfigPath()

    let bootstrapResult =
        Portfolios.BootstrapIfMissing.execute configPath
        |> Effect.run deps
        |> Result.mapError formatPortfolioError

    match bootstrapResult with
    | Error msg -> Error msg
    | Ok wasCreated ->
        if wasCreated then
            printfn "Created default config at %s. Run 'itr init' to configure profiles and products." configPath

        match
            results.TryGetResult Backlog,
            results.TryGetResult CliArgs.ProfileCmd,
            results.TryGetResult Product,
            results.TryGetResult Task,
            results.TryGetResult View
        with

        | Some backlogResults, _, _, _, _ ->
            match backlogResults with
            | BacklogTake takeArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Backlogs.Take.handle deps resolved takeArgs format)

            | BacklogAdd addArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Backlogs.Add.handle deps resolved addArgs format)

            | BacklogList listArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Backlogs.List.handle deps resolved listArgs)

            | BacklogInfo infoArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Backlogs.Info.handle deps resolved infoArgs)

            | _ ->
                Error
                    "Specify a backlog subcommand (e.g. backlog take <id> or backlog add <id> or backlog list or backlog info <id>)"

        | _, Some profilesResults, _, _, _ ->
            match profilesResults with
            | ProfileAdd addArgs ->
                let name = addArgs.GetResult ProfileAddArgs.Name
                let gitName = addArgs.TryGetResult ProfileAddArgs.Git_Name
                let gitEmail = addArgs.TryGetResult ProfileAddArgs.Git_Email
                let setDefault = addArgs.Contains ProfileAddArgs.Set_Default

                match gitEmail, gitName with
                | Some _, None -> Error "--git-email requires --git-name to also be specified"
                | _ ->
                    let gitIdentity = gitName |> Option.map (fun gn -> { Name = gn; Email = gitEmail })

                    let input: Portfolios.AddProfile.Input =
                        { Name = name
                          GitIdentity = gitIdentity
                          SetAsDefault = setDefault }

                    let result =
                        Portfolios.AddProfile.execute configPath input
                        |> Effect.run deps
                        |> Result.mapError formatPortfolioError

                    match result with
                    | Error msg -> Error msg
                    | Ok updatedPortfolio ->
                        let portfolioConfig = deps :> IPortfolioConfig

                        portfolioConfig.SaveConfig configPath updatedPortfolio
                        |> Result.mapError formatPortfolioError
                        |> Result.map (fun () -> printfn "Added profile '%s'." name)

            | ProfileList listArgs ->
                let portfolioResult =
                    Portfolios.Query.load (Some configPath)
                    |> Effect.run deps
                    |> Result.mapError formatPortfolioError

                match portfolioResult with
                | Error msg -> Error msg
                | Ok portfolio -> Portfolios.ProfileList.handle portfolio listArgs

            | ProfileSetDefault setDefaultArgs ->
                let name = setDefaultArgs.GetResult ProfileSetDefaultArgs.Name
                let useLocal = setDefaultArgs.Contains ProfileSetDefaultArgs.Local
                let useGlobal = setDefaultArgs.Contains ProfileSetDefaultArgs.Global
                let portfolioConfig = deps :> IPortfolioConfig
                let fs = deps :> IFileSystem

                let targetPathResult: Result<string * string, string> =
                    if useGlobal then
                        Ok(configPath, configPath)
                    elif useLocal then
                        let cwd = System.IO.Directory.GetCurrentDirectory()
                        let productYamlPath = System.IO.Path.Combine(cwd, "product.yaml")

                        if fs.FileExists productYamlPath then
                            let localConfigPath = System.IO.Path.Combine(cwd, "itr.json")
                            Ok(localConfigPath, localConfigPath)
                        else
                            Error
                                "--local flag requires a product context. Run this command from within a product directory or specify --global instead."
                    else
                        let cwd = System.IO.Directory.GetCurrentDirectory()
                        let productYamlPath = System.IO.Path.Combine(cwd, "product.yaml")
                        let localConfigPath = System.IO.Path.Combine(cwd, "itr.json")

                        if fs.FileExists productYamlPath && fs.FileExists localConfigPath then
                            Ok(localConfigPath, localConfigPath)
                        else
                            Ok(configPath, configPath)

                match targetPathResult with
                | Error msg -> Error msg
                | Ok(targetPath, displayPath) ->
                    let bootstrapResult2 =
                        if not (fs.FileExists targetPath) then
                            Portfolios.BootstrapIfMissing.execute targetPath
                            |> Effect.run deps
                            |> Result.mapError formatPortfolioError
                            |> Result.map (fun _ -> ())
                        else
                            Ok()

                    match bootstrapResult2 with
                    | Error msg -> Error msg
                    | Ok() ->
                        let input: Portfolios.SetDefaultProfile.Input = { Name = name }

                        let result =
                            Portfolios.SetDefaultProfile.execute targetPath input
                            |> Effect.run deps
                            |> Result.mapError formatPortfolioError

                        match result with
                        | Error msg -> Error msg
                        | Ok updatedPortfolio ->
                            portfolioConfig.SaveConfig targetPath updatedPortfolio
                            |> Result.mapError formatPortfolioError
                            |> Result.map (fun () -> printfn "Profile '%s' set as default. (%s)" name displayPath)

            | _ ->
                Error
                    "Specify a profile subcommand (e.g. profile add <name>, profile list, or profile set-default <name>)"

        | _, _, Some productResults, _, _ ->
            match productResults with
            | ProductInit initArgs ->
                let rawPath = initArgs.GetResult ProductInitArgs.Path

                let rawId =
                    match initArgs.TryGetResult ProductInitArgs.Id with
                    | Some id -> id
                    | None -> AnsiConsole.Ask<string>("Product id:")

                let rawRepoId =
                    match initArgs.TryGetResult ProductInitArgs.Repo_Id with
                    | Some repoId -> repoId
                    | None ->
                        let answer = AnsiConsole.Ask<string>($"Repo id (default: same as id):")
                        if String.IsNullOrWhiteSpace(answer) then rawId else answer

                let coordPath =
                    initArgs.TryGetResult ProductInitArgs.Coord_Path |> Option.defaultValue ".itr"

                let coordMode =
                    initArgs.TryGetResult ProductInitArgs.Coord_Mode
                    |> Option.defaultValue "primary-repo"

                let registerProfile =
                    if initArgs.Contains ProductInitArgs.No_Register then
                        None
                    else
                        match initArgs.TryGetResult ProductInitArgs.Register_Profile with
                        | Some p -> Some p
                        | None ->
                            let answer = AnsiConsole.Ask<string>("Register in profile (leave blank to skip):")

                            if String.IsNullOrWhiteSpace(answer) then
                                None
                            else
                                Some answer

                let input: Portfolios.InitProduct.Input =
                    { Id = rawId
                      Path = rawPath
                      RepoId = rawRepoId
                      CoordPath = coordPath
                      CoordinationMode = coordMode
                      RegisterProfile = registerProfile }

                let result =
                    Portfolios.InitProduct.execute configPath input
                    |> Effect.run deps
                    |> Result.mapError formatPortfolioError

                match result with
                | Error msg -> Error msg
                | Ok maybePortfolio ->
                    printfn "Initialized product '%s' at %s." rawId rawPath

                    match maybePortfolio, registerProfile with
                    | Some _, Some prof -> printfn "Registered in profile '%s'." prof
                    | _ -> ()

                    Ok()

            | ProductRegister registerArgs ->
                Portfolios.ProductRegister.handle deps configPath profile registerArgs format

            | ProductList listArgs -> Portfolios.ProductList.handle configPath deps listArgs

            | ProductInfo infoArgs -> Portfolios.ProductInfo.handle configPath deps infoArgs

            | _ ->
                Error
                    "Specify a product subcommand (e.g. product init <path>, product register <path>, product list, or product info)"

        | _, _, _, Some taskResults, _ ->
            match taskResults with
            | TaskList listArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Tasks.List.handle deps resolved listArgs)

            | TaskInfo infoArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Tasks.Info.handle deps resolved infoArgs)

            | TaskPlan planArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Tasks.Plan.handle deps resolved planArgs)

            | TaskApprove approveArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (resolveProduct deps)
                |> Result.bind (fun resolved -> Tasks.Approve.handle deps resolved approveArgs)

            | _ ->
                Error
                    "Specify a task subcommand (e.g. task list or task info <id> or task plan <id> or task approve <id>)"

        | _, _, _, _, Some viewResults ->
            match viewResults with
            | ViewList listArgs ->
                resolvePortfolio deps configPath profile
                |> Result.bind (fun activeProfile ->
                    let productIdOpt = listArgs.TryGetResult ViewListArgs.Product

                    match productIdOpt with
                    | Some rawId ->
                        Portfolios.Query.resolveProduct activeProfile rawId
                        |> Effect.run deps
                        |> Result.mapError (fun e -> $"%A{e}")
                    | None -> resolveProduct deps activeProfile)
                |> Result.bind (fun resolved -> Views.List.handle deps resolved listArgs)

            | _ -> Error "Specify a view subcommand (e.g. view list)"

        | _ ->
            let loadResult = Portfolios.Query.load (Some configPath) |> Effect.run deps

            loadResult
            |> Result.bind (fun portfolio -> Portfolios.Query.resolveActiveProfile portfolio profile |> Effect.run deps)
            |> Result.map (fun _ -> printfn "itr cli")
            |> Result.mapError (fun e -> $"%A{e}")

[<EntryPoint>]
let main argv =
    let deps = AppDeps()
    let parser = ArgumentParser.Create<CliArgs>(programName = "itr")

    try
        let results = parser.Parse argv

        match dispatch deps results with
        | Ok() -> 0
        | Error msg ->
            eprintfn "Error: %s" msg
            1
    with :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1
