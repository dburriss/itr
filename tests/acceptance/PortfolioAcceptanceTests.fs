module Itr.Tests.Acceptance.PortfolioAcceptanceTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Features
open Itr.Adapters
open Itr.Adapters.PortfolioAdapter

module DomainPortfolio = Itr.Domain.Portfolio
module FeaturePortfolio = Itr.Features.Portfolio

/// Test dependency implementation
type TestDeps(portfolioPath: string) =
    let envAdapter = EnvironmentAdapter()
    let fsAdapter = FileSystemAdapter()
    let productConfigAdapter = YamlAdapter.ProductConfigAdapter()
    let homeDir = (envAdapter :> IEnvironment).HomeDirectory()

    interface IEnvironment with
        member _.GetEnvVar _ = None
        member _.HomeDirectory() = homeDir

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
        member _.ConfigPath() = portfolioPath
        member _.LoadConfig path = readConfig homeDir path

    interface IProductConfig with
        member _.LoadProductConfig root =
            (productConfigAdapter :> IProductConfig).LoadProductConfig root

/// Creates a temp directory structure for portfolio acceptance tests.
/// itr.json uses path-string product entries; each product root has a product.yaml.
type TestFixture() =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-portfolio-tests-{Guid.NewGuid():N}")

    let portfolioPath = Path.Combine(root, "itr.json")
    let alphaRoot = Path.Combine(root, "alpha")
    let betaRoot = Path.Combine(root, "beta")
    let gammaRoot = Path.Combine(root, "gamma")

    do
        Directory.CreateDirectory(root) |> ignore
        Directory.CreateDirectory(alphaRoot) |> ignore
        Directory.CreateDirectory(betaRoot) |> ignore
        Directory.CreateDirectory(gammaRoot) |> ignore
        // coord roots
        Directory.CreateDirectory(Path.Combine(alphaRoot, ".itr")) |> ignore
        Directory.CreateDirectory(Path.Combine(betaRoot, ".itr")) |> ignore
        Directory.CreateDirectory(Path.Combine(gammaRoot, ".itr")) |> ignore

        // itr.json — products are bare path strings
        let config =
            $"""{{
  "defaultProfile": "work",
  "profiles": {{
    "work": {{
      "products": ["{alphaRoot}", "{betaRoot}", "{gammaRoot}"]
    }}
  }}
}}"""

        File.WriteAllText(portfolioPath, config)

        // product.yaml for each product root (standalone mode)
        let writeProductYaml (dir: string) (id: string) =
            let yaml =
                $"""id: {id}
coordination:
  mode: standalone
  path: .itr
"""

            File.WriteAllText(Path.Combine(dir, "product.yaml"), yaml)

        writeProductYaml alphaRoot "alpha"
        writeProductYaml betaRoot "beta"
        writeProductYaml gammaRoot "gamma"

    member _.Root = root
    member _.PortfolioPath = portfolioPath

    interface IDisposable with
        member _.Dispose() =
            if Directory.Exists(root) then
                Directory.Delete(root, true)

let private getResult =
    function
    | Ok value -> value
    | Error error -> failwithf "expected Ok, got %A" error

[<Fact>]
let ``full pipeline resolves to ResolvedProduct`` () =
    use fixture = new TestFixture()
    let deps = TestDeps(fixture.PortfolioPath)

    let resolved =
        FeaturePortfolio.loadPortfolio (Some fixture.PortfolioPath)
        |> Effect.run deps
        |> Result.bind (fun portfolio -> FeaturePortfolio.resolveActiveProfile portfolio None |> Effect.run deps)
        |> Result.bind (fun profile -> FeaturePortfolio.resolveProduct profile "alpha" |> Effect.run deps)
        |> getResult

    Assert.True(Directory.Exists(resolved.CoordRoot.AbsolutePath))
    Assert.Equal("alpha", ProductId.value resolved.Definition.Id)

[<Fact>]
let ``readConfig returns ConfigNotFound when file is absent`` () =
    let missing =
        Path.Combine(Path.GetTempPath(), $"itr-missing-{Guid.NewGuid():N}", "itr.json")

    let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    match readConfig homeDir missing with
    | Error(ConfigNotFound path) -> Assert.Equal(missing, path)
    | other -> failwithf "expected ConfigNotFound, got %A" other

[<Fact>]
let ``readConfig returns ConfigParseError for malformed JSON`` () =
    let dir = Path.Combine(Path.GetTempPath(), $"itr-bad-json-{Guid.NewGuid():N}")
    Directory.CreateDirectory(dir) |> ignore
    let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    try
        let path = Path.Combine(dir, "itr.json")
        File.WriteAllText(path, "{ invalid json")

        match readConfig homeDir path with
        | Error(ConfigParseError(returnedPath, _)) -> Assert.Equal(path, returnedPath)
        | other -> failwithf "expected ConfigParseError, got %A" other
    finally
        if Directory.Exists(dir) then
            Directory.Delete(dir, true)

[<Fact>]
let ``all products resolve with standalone coord root`` () =
    use fixture = new TestFixture()
    let deps = TestDeps(fixture.PortfolioPath)

    let profile =
        FeaturePortfolio.loadPortfolio (Some fixture.PortfolioPath)
        |> Effect.run deps
        |> Result.bind (fun portfolio -> FeaturePortfolio.resolveActiveProfile portfolio None |> Effect.run deps)
        |> getResult

    [ "alpha"; "beta"; "gamma" ]
    |> List.iter (fun id ->
        let resolved =
            FeaturePortfolio.resolveProduct profile id |> Effect.run deps |> getResult

        Assert.EndsWith(".itr", resolved.CoordRoot.AbsolutePath))

[<Fact>]
let ``duplicate registration returns DuplicateProductId error`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-dup-{Guid.NewGuid():N}")

    try
        let productRoot = Path.Combine(root, "product")
        Directory.CreateDirectory(productRoot) |> ignore
        Directory.CreateDirectory(Path.Combine(productRoot, ".itr")) |> ignore

        // Both entries point to different paths but same canonical id
        let productRoot2 = Path.Combine(root, "product2")
        Directory.CreateDirectory(productRoot2) |> ignore
        Directory.CreateDirectory(Path.Combine(productRoot2, ".itr")) |> ignore

        let portfolioPath = Path.Combine(root, "itr.json")

        let config =
            $"""{{
  "defaultProfile": "work",
  "profiles": {{
    "work": {{
      "products": ["{productRoot}", "{productRoot2}"]
    }}
  }}
}}"""

        File.WriteAllText(portfolioPath, config)

        let writeProductYaml dir id =
            File.WriteAllText(
                Path.Combine(dir, "product.yaml"),
                $"id: {id}\ncoordination:\n  mode: standalone\n  path: .itr\n"
            )

        writeProductYaml productRoot "same-id"
        writeProductYaml productRoot2 "same-id"

        let deps = TestDeps(portfolioPath)

        let profile =
            FeaturePortfolio.loadPortfolio (Some portfolioPath)
            |> Effect.run deps
            |> Result.bind (fun portfolio -> FeaturePortfolio.resolveActiveProfile portfolio None |> Effect.run deps)
            |> getResult

        match FeaturePortfolio.resolveProduct profile "same-id" |> Effect.run deps with
        | Error(DuplicateProductId(profileName, productId)) ->
            Assert.Equal("work", profileName)
            Assert.Equal("same-id", productId)
        | other -> failwithf "expected DuplicateProductId, got %A" other
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// Bootstrap acceptance tests
// ---------------------------------------------------------------------------

/// Minimal IFileSystem that always fails writes with IoException
type FailingWriteFsDeps(failMsg: string) =
    interface IFileSystem with
        member _.ReadFile path =
            if File.Exists(path) then
                Ok(File.ReadAllText(path))
            else
                Error(FileNotFound path)

        member _.WriteFile path _ = Error(IoException(path, failMsg))
        member _.FileExists path = File.Exists(path)
        member _.DirectoryExists path = Directory.Exists(path)

[<Fact>]
let ``bootstrapIfMissing creates file and parent directory when both absent`` () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-bootstrap-{Guid.NewGuid():N}")

    try
        let configPath = Path.Combine(root, "subdir", "itr.json")
        let deps = TestDeps(configPath)

        let result = FeaturePortfolio.bootstrapIfMissing configPath |> Effect.run deps

        match result with
        | Ok wasCreated ->
            Assert.True(wasCreated, "Expected wasCreated = true for a new file")
            Assert.True(File.Exists(configPath), $"Expected config file at: {configPath}")
            let content = File.ReadAllText(configPath)
            Assert.Contains("profiles", content)
        | Error e -> failwithf "expected Ok, got %A" e
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``bootstrapIfMissing is idempotent when file already exists`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-bootstrap-idempotent-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        let originalContent = """{"defaultProfile": "work", "profiles": {}}"""
        File.WriteAllText(configPath, originalContent)

        let deps = TestDeps(configPath)
        let result = FeaturePortfolio.bootstrapIfMissing configPath |> Effect.run deps

        match result with
        | Ok wasCreated ->
            Assert.False(wasCreated, "Expected wasCreated = false when file already exists")
            let content = File.ReadAllText(configPath)
            Assert.Equal(originalContent, content)
        | Error e -> failwithf "expected Ok, got %A" e
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``bootstrapIfMissing returns BootstrapWriteError when write fails`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-bootstrap-fail-{Guid.NewGuid():N}")
    // Use a path that does not exist so FileExists returns false, then the write fails
    let configPath = Path.Combine(root, "itr.json")
    let deps = FailingWriteFsDeps("Permission denied")

    let result = FeaturePortfolio.bootstrapIfMissing configPath |> Effect.run deps

    match result with
    | Error(BootstrapWriteError(path, msg)) ->
        Assert.Equal(configPath, path)
        Assert.Contains("Permission denied", msg)
    | other -> failwithf "expected BootstrapWriteError, got %A" other
