module Itr.Tests.Acceptance.PortfolioAcceptanceTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Commands
open Itr.Adapters.PortfolioAdapter

module DomainPortfolio = Itr.Domain.Portfolio
module CommandPortfolio = Itr.Commands.Portfolio

type TestFixture() =
    let root = Path.Combine(Path.GetTempPath(), $"itr-portfolio-tests-{Guid.NewGuid():N}")
    let portfolioPath = Path.Combine(root, "portfolio.json")
    let standalone = Path.Combine(root, "standalone")
    let primary = Path.Combine(root, "primary")
    let control = Path.Combine(root, "control")

    do
        Directory.CreateDirectory(root) |> ignore
        Directory.CreateDirectory(Path.Combine(standalone, ".itr")) |> ignore
        Directory.CreateDirectory(Path.Combine(primary, ".itr")) |> ignore
        Directory.CreateDirectory(Path.Combine(control, ".itr")) |> ignore

        let config =
            $"""
{{
  "defaultProfile": "work",
  "profiles": {{
    "work": {{
      "products": [
        {{ "id": "alpha", "root": {{ "mode": "standalone", "dir": "{standalone}" }} }},
        {{ "id": "beta", "root": {{ "mode": "primary-repo", "repoDir": "{primary}" }} }},
        {{ "id": "gamma", "root": {{ "mode": "control-repo", "repoDir": "{control}" }} }}
      ]
    }}
  }}
}}
"""

        File.WriteAllText(portfolioPath, config)

    member _.Root = root
    member _.PortfolioPath = portfolioPath

    interface IDisposable with
        member _.Dispose() =
            if Directory.Exists(root) then
                Directory.Delete(root, true)

let private getResult = function
    | Ok value -> value
    | Error error -> failwithf "expected Ok, got %A" error

[<Fact>]
let ``full pipeline resolves to ResolvedProduct`` () =
    use fixture = new TestFixture()
    register ()

    let resolved =
        CommandPortfolio.loadPortfolio (Some fixture.PortfolioPath)
        |> Result.bind (fun portfolio -> CommandPortfolio.resolveActiveProfile portfolio None (fun _ -> None))
        |> Result.bind (fun profile -> CommandPortfolio.resolveProduct profile "alpha" Directory.Exists)
        |> getResult

    Assert.Equal("alpha", ProductId.value resolved.Product.Id)
    Assert.True(Directory.Exists(resolved.CoordRoot.AbsolutePath))

[<Fact>]
let ``readConfig returns ConfigNotFound when file is absent`` () =
    let missing = Path.Combine(Path.GetTempPath(), $"itr-missing-{Guid.NewGuid():N}", "portfolio.json")

    match readConfig missing with
    | Error (ConfigNotFound path) -> Assert.Equal(missing, path)
    | other -> failwithf "expected ConfigNotFound, got %A" other

[<Fact>]
let ``readConfig returns ConfigParseError for malformed JSON`` () =
    let dir = Path.Combine(Path.GetTempPath(), $"itr-bad-json-{Guid.NewGuid():N}")
    Directory.CreateDirectory(dir) |> ignore

    try
        let path = Path.Combine(dir, "portfolio.json")
        File.WriteAllText(path, "{ invalid json")

        match readConfig path with
        | Error (ConfigParseError(returnedPath, _)) -> Assert.Equal(path, returnedPath)
        | other -> failwithf "expected ConfigParseError, got %A" other
    finally
        if Directory.Exists(dir) then
            Directory.Delete(dir, true)

[<Fact>]
let ``all coordination modes resolve to dir/.itr`` () =
    use fixture = new TestFixture()
    register ()

    let profile =
        CommandPortfolio.loadPortfolio (Some fixture.PortfolioPath)
        |> Result.bind (fun portfolio -> CommandPortfolio.resolveActiveProfile portfolio None (fun _ -> None))
        |> getResult

    [ "alpha"; "beta"; "gamma" ]
    |> List.iter (fun id ->
        let resolved = CommandPortfolio.resolveProduct profile id Directory.Exists |> getResult
        Assert.EndsWith(".itr", resolved.CoordRoot.AbsolutePath))
