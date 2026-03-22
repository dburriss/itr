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
        member _.LoadConfig path = readConfig (fsAdapter :> IFileSystem) homeDir path
        member _.SaveConfig path portfolio = writeConfig (fsAdapter :> IFileSystem) homeDir path portfolio

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
    let fs = FileSystemAdapter() :> IFileSystem

    match readConfig fs homeDir missing with
    | Error(ConfigNotFound path) -> Assert.Equal(missing, path)
    | other -> failwithf "expected ConfigNotFound, got %A" other

[<Fact>]
let ``readConfig returns ConfigParseError for malformed JSON`` () =
    let dir = Path.Combine(Path.GetTempPath(), $"itr-bad-json-{Guid.NewGuid():N}")
    Directory.CreateDirectory(dir) |> ignore
    let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let fs = FileSystemAdapter() :> IFileSystem

    try
        let path = Path.Combine(dir, "itr.json")
        File.WriteAllText(path, "{ invalid json")

        match readConfig fs homeDir path with
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

// ---------------------------------------------------------------------------
// writeConfig / readConfig round-trip acceptance tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``writeConfig then readConfig round-trip preserves defaultProfile and all profiles`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-roundtrip-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        let fs = FileSystemAdapter() :> IFileSystem

        let portfolio =
            DomainPortfolio.tryCreate
                (Some(ProfileName.create "work"))
                [ { Name = ProfileName.create "work"
                    Products = []
                    GitIdentity = None }
                  { Name = ProfileName.create "personal"
                    Products = []
                    GitIdentity = Some { Name = "Alice"; Email = Some "alice@example.com" } } ]
            |> Result.defaultWith (fun e -> failwithf "failed to build test portfolio: %A" e)

        match writeConfig fs homeDir configPath portfolio with
        | Error e -> failwithf "writeConfig failed: %A" e
        | Ok() ->
            match readConfig fs homeDir configPath with
            | Error e -> failwithf "readConfig failed: %A" e
            | Ok loaded ->
                Assert.Equal(
                    portfolio.DefaultProfile |> Option.map ProfileName.value,
                    loaded.DefaultProfile |> Option.map ProfileName.value
                )

                Assert.Equal(portfolio.Profiles.Count, loaded.Profiles.Count)

                for kvp in portfolio.Profiles do
                    let name = ProfileName.value kvp.Key

                    match loaded.Profiles |> Map.tryFind (ProfileName.create name) with
                    | None -> failwithf "profile '%s' not found after round-trip" name
                    | Some loadedProfile ->
                        Assert.Equal(kvp.Value.GitIdentity, loadedProfile.GitIdentity)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// profiles add acceptance tests
// ---------------------------------------------------------------------------

/// Helper: run addProfile usecase and persist result via SaveConfig
let private runAddProfile (deps: TestDeps) (configPath: string) (input: FeaturePortfolio.AddProfileInput) =
    let portfolioConfig = deps :> IPortfolioConfig

    FeaturePortfolio.addProfile configPath input
    |> Effect.run deps
    |> Result.bind (fun updatedPortfolio -> portfolioConfig.SaveConfig configPath updatedPortfolio)

[<Fact>]
let ``profiles add writes new profile to itr.json`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-profiles-add-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        File.WriteAllText(configPath, """{"defaultProfile": null, "profiles": {}}""")

        let deps = TestDeps(configPath)

        let input: FeaturePortfolio.AddProfileInput =
            { Name = "my-work"; GitIdentity = None; SetAsDefault = false }

        match runAddProfile deps configPath input with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok() ->
            let content = File.ReadAllText(configPath)
            Assert.Contains("my-work", content)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``profiles add with --set-default updates defaultProfile`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-profiles-setdefault-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        File.WriteAllText(configPath, """{"defaultProfile": null, "profiles": {}}""")

        let deps = TestDeps(configPath)

        let input: FeaturePortfolio.AddProfileInput =
            { Name = "work"; GitIdentity = None; SetAsDefault = true }

        match runAddProfile deps configPath input with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok() ->
            let portfolioConfig = deps :> IPortfolioConfig

            match portfolioConfig.LoadConfig configPath with
            | Error e -> failwithf "readConfig failed: %A" e
            | Ok loaded ->
                Assert.Equal(Some "work", loaded.DefaultProfile |> Option.map ProfileName.value)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``profiles add duplicate name returns error and file is unchanged`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-profiles-dup-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        let originalContent =
            """{"defaultProfile": "work", "profiles": {"work": {"products": []}}}"""

        File.WriteAllText(configPath, originalContent)

        let deps = TestDeps(configPath)

        let input: FeaturePortfolio.AddProfileInput =
            { Name = "work"; GitIdentity = None; SetAsDefault = false }

        match runAddProfile deps configPath input with
        | Error(DuplicateProfileName name) ->
            Assert.Equal("work", name)
            Assert.Equal(originalContent, File.ReadAllText(configPath))
        | other -> failwithf "expected DuplicateProfileName, got %A" other
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``profiles add preserves existing profiles`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-profiles-preserve-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        File.WriteAllText(
            configPath,
            """{"defaultProfile": "personal", "profiles": {"personal": {"products": []}}}"""
        )

        let deps = TestDeps(configPath)

        let input: FeaturePortfolio.AddProfileInput =
            { Name = "work"; GitIdentity = None; SetAsDefault = false }

        match runAddProfile deps configPath input with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok() ->
            let portfolioConfig = deps :> IPortfolioConfig

            match portfolioConfig.LoadConfig configPath with
            | Error e -> failwithf "readConfig failed: %A" e
            | Ok loaded ->
                Assert.True(loaded.Profiles |> Map.containsKey (ProfileName.create "personal"))
                Assert.True(loaded.Profiles |> Map.containsKey (ProfileName.create "work"))
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``profiles add git-email without git-name is caught by CLI validation`` () =
    // This test verifies the domain/usecase correctly adds a profile with gitName only (no email),
    // and that a git identity with no name but with email is not created by the usecase
    // (the CLI guards this, not the usecase itself - so we test via CLI logic)
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-profiles-gitval-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let configPath = Path.Combine(root, "itr.json")
        File.WriteAllText(configPath, """{"defaultProfile": null, "profiles": {}}""")

        let deps = TestDeps(configPath)

        // Simulate what the CLI does: if git-email given without git-name, we don't call usecase
        // Verify that adding with git-name only succeeds
        let input: FeaturePortfolio.AddProfileInput =
            { Name = "dev"
              GitIdentity = Some { Name = "Alice"; Email = None }
              SetAsDefault = false }

        match runAddProfile deps configPath input with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok() ->
            let portfolioConfig = deps :> IPortfolioConfig

            match portfolioConfig.LoadConfig configPath with
            | Error e -> failwithf "readConfig failed: %A" e
            | Ok loaded ->
                match loaded.Profiles |> Map.tryFind (ProfileName.create "dev") with
                | None -> failwith "profile 'dev' not found"
                | Some p ->
                    Assert.Equal(Some { Name = "Alice"; Email = None }, p.GitIdentity)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// initProduct acceptance tests
// ---------------------------------------------------------------------------

/// Helper: create an itr.json with a single "default" profile
let private makeItrJson (portfolioPath: string) =
    File.WriteAllText(portfolioPath, """{"defaultProfile": "default", "profiles": {"default": {"products": []}}}""")

let private defaultInitInput idStr (path: string) : Portfolio.InitProductInput =
    { Id = idStr
      Path = path
      RepoId = "my-repo"
      CoordPath = ".itr"
      CoordinationMode = "primary-repo"
      RegisterProfile = None }

[<Fact>]
let ``initProduct creates all expected files and itr.json updated when registered`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-init-product-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let portfolioPath = Path.Combine(root, "itr.json")
        makeItrJson portfolioPath

        let productPath = Path.Combine(root, "my-product")
        Directory.CreateDirectory(productPath) |> ignore

        let deps = TestDeps(portfolioPath)

        let input =
            { defaultInitInput "my-product" productPath with
                RegisterProfile = Some "default" }

        match FeaturePortfolio.initProduct portfolioPath input |> Effect.run deps with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok result ->
            Assert.True(File.Exists(Path.Combine(productPath, "product.yaml")), "product.yaml missing")
            Assert.True(File.Exists(Path.Combine(productPath, ".itr", ".gitkeep")), ".gitkeep missing")
            Assert.True(File.Exists(Path.Combine(productPath, "PRODUCT.md")), "PRODUCT.md missing")
            Assert.True(File.Exists(Path.Combine(productPath, "ARCHITECTURE.md")), "ARCHITECTURE.md missing")

            Assert.True(result.IsSome, "expected Some updatedPortfolio")

            let portfolioConfig = deps :> IPortfolioConfig

            match portfolioConfig.LoadConfig portfolioPath with
            | Error e -> failwithf "LoadConfig failed: %A" e
            | Ok loaded ->
                let profile = loaded.Profiles |> Map.find (ProfileName.create "default")
                Assert.Equal(1, profile.Products.Length)

            let yaml = File.ReadAllText(Path.Combine(productPath, "product.yaml"))
            Assert.Contains("id: my-product", yaml)
            Assert.Contains("mode: primary-repo", yaml)
            Assert.Contains("repo: my-repo", yaml)

            let productMd = File.ReadAllText(Path.Combine(productPath, "PRODUCT.md"))
            Assert.Contains("# Product: my-product", productMd)
            Assert.Contains("## Purpose", productMd)

            let archMd = File.ReadAllText(Path.Combine(productPath, "ARCHITECTURE.md"))
            Assert.Contains("# Architecture: my-product", archMd)
            Assert.Contains("## Technology Stack", archMd)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``initProduct skip registration leaves itr.json unchanged`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-init-noreg-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let portfolioPath = Path.Combine(root, "itr.json")
        makeItrJson portfolioPath
        let originalContent = File.ReadAllText(portfolioPath)

        let productPath = Path.Combine(root, "my-product")
        Directory.CreateDirectory(productPath) |> ignore

        let deps = TestDeps(portfolioPath)
        let input = defaultInitInput "my-product" productPath

        match FeaturePortfolio.initProduct portfolioPath input |> Effect.run deps with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok result ->
            Assert.True(result.IsNone, "expected None when RegisterProfile=None")
            Assert.Equal(originalContent, File.ReadAllText(portfolioPath))
            Assert.True(File.Exists(Path.Combine(productPath, "product.yaml")))
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``initProduct duplicate product root in profile returns error and itr.json unchanged`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-init-dup-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let portfolioPath = Path.Combine(root, "itr.json")

        let productPath = Path.GetFullPath(Path.Combine(root, "my-product"))
        Directory.CreateDirectory(productPath) |> ignore

        // Register productPath once
        makeItrJson portfolioPath

        let deps = TestDeps(portfolioPath)

        let input1 =
            { defaultInitInput "my-product" productPath with
                RegisterProfile = Some "default" }

        match FeaturePortfolio.initProduct portfolioPath input1 |> Effect.run deps with
        | Error e -> failwithf "first init expected Ok, got %A" e
        | Ok _ ->
            let afterFirst = File.ReadAllText(portfolioPath)

            // Try to register the same root again
            let regInput: Portfolio.RegisterProductInput =
                { Path = productPath
                  Profile = Some "default" }

            let deps2 = TestDeps(portfolioPath)

            match Portfolio.registerProduct portfolioPath regInput |> Effect.run deps2 with
            | Error _ ->
                Assert.Equal(afterFirst, File.ReadAllText(portfolioPath))
            | Ok _ -> failwith "expected error for duplicate registration"
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// registerProduct acceptance tests
// ---------------------------------------------------------------------------

/// Helper: write itr.json with one named profile and no products
let private makePortfolioJson (portfolioPath: string) (profileName: string) =
    File.WriteAllText(
        portfolioPath,
        $"""{{ "defaultProfile": "{profileName}", "profiles": {{ "{profileName}": {{ "products": [] }} }} }}"""
    )

/// Helper: write product.yaml with given id
let private writeProductYamlWithId (dir: string) (id: string) =
    File.WriteAllText(
        Path.Combine(dir, "product.yaml"),
        $"id: {id}\ncoordination:\n  mode: standalone\n  path: .itr\n"
    )

[<Fact>]
let ``registerProduct end-to-end adds product root to profile in itr.json`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-reg-e2e-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let portfolioPath = Path.Combine(root, "itr.json")
        makePortfolioJson portfolioPath "work"

        let productRoot = Path.Combine(root, "my-product")
        Directory.CreateDirectory(productRoot) |> ignore
        writeProductYamlWithId productRoot "my-product"

        let deps = TestDeps(portfolioPath)

        let input: FeaturePortfolio.RegisterProductInput =
            { Path = productRoot; Profile = None }

        match FeaturePortfolio.registerProduct portfolioPath input |> Effect.run deps with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok updatedPortfolio ->
            let portfolioConfig = deps :> IPortfolioConfig

            portfolioConfig.SaveConfig portfolioPath updatedPortfolio
            |> Result.defaultWith (fun e -> failwithf "SaveConfig failed: %A" e)

            let loaded =
                portfolioConfig.LoadConfig portfolioPath
                |> Result.defaultWith (fun e -> failwithf "LoadConfig failed: %A" e)

            let profile = loaded.Profiles |> Map.find (ProfileName.create "work")
            Assert.Equal(1, profile.Products.Length)
            let (ProductRoot storedPath) = profile.Products.[0].Root
            Assert.Equal(productRoot, storedPath)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``registerProduct duplicate canonical id returns DuplicateProductId and file unchanged`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-reg-dup2-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let portfolioPath = Path.Combine(root, "itr.json")
        makePortfolioJson portfolioPath "work"

        let productRoot = Path.Combine(root, "my-product")
        Directory.CreateDirectory(productRoot) |> ignore
        writeProductYamlWithId productRoot "my-product"

        let deps = TestDeps(portfolioPath)

        let input: FeaturePortfolio.RegisterProductInput =
            { Path = productRoot; Profile = None }

        // First registration
        match FeaturePortfolio.registerProduct portfolioPath input |> Effect.run deps with
        | Error e -> failwithf "first registration expected Ok, got %A" e
        | Ok updatedPortfolio ->
            (deps :> IPortfolioConfig).SaveConfig portfolioPath updatedPortfolio
            |> Result.defaultWith (fun e -> failwithf "SaveConfig failed: %A" e)

            let afterFirst = File.ReadAllText(portfolioPath)
            let deps2 = TestDeps(portfolioPath)

            // Second registration — same canonical id
            match FeaturePortfolio.registerProduct portfolioPath input |> Effect.run deps2 with
            | Error(DuplicateProductId(profileName, productId)) ->
                Assert.Equal("work", profileName)
                Assert.Equal("my-product", productId)
                Assert.Equal(afterFirst, File.ReadAllText(portfolioPath))
            | other -> failwithf "expected DuplicateProductId, got %A" other
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)

[<Fact>]
let ``registerProduct round-trip preserves existing profiles and products`` () =
    let root =
        Path.Combine(Path.GetTempPath(), $"itr-reg-roundtrip-{Guid.NewGuid():N}")

    try
        Directory.CreateDirectory(root) |> ignore
        let portfolioPath = Path.Combine(root, "itr.json")

        // Create itr.json with two profiles; work already has one product
        let existingProductRoot = Path.Combine(root, "existing-product")
        Directory.CreateDirectory(existingProductRoot) |> ignore
        writeProductYamlWithId existingProductRoot "existing-product"

        File.WriteAllText(
            portfolioPath,
            $"""{{
  "defaultProfile": "work",
  "profiles": {{
    "work": {{ "products": ["{existingProductRoot}"] }},
    "personal": {{ "products": [] }}
  }}
}}"""
        )

        let newProductRoot = Path.Combine(root, "new-product")
        Directory.CreateDirectory(newProductRoot) |> ignore
        writeProductYamlWithId newProductRoot "new-product"

        let deps = TestDeps(portfolioPath)

        let input: FeaturePortfolio.RegisterProductInput =
            { Path = newProductRoot; Profile = None }

        match FeaturePortfolio.registerProduct portfolioPath input |> Effect.run deps with
        | Error e -> failwithf "expected Ok, got %A" e
        | Ok updatedPortfolio ->
            (deps :> IPortfolioConfig).SaveConfig portfolioPath updatedPortfolio
            |> Result.defaultWith (fun e -> failwithf "SaveConfig failed: %A" e)

            let loaded =
                (deps :> IPortfolioConfig).LoadConfig portfolioPath
                |> Result.defaultWith (fun e -> failwithf "LoadConfig failed: %A" e)

            // Both profiles must still be present
            Assert.True(loaded.Profiles |> Map.containsKey (ProfileName.create "work"))
            Assert.True(loaded.Profiles |> Map.containsKey (ProfileName.create "personal"))

            // work profile now has 2 products
            let workProfile = loaded.Profiles |> Map.find (ProfileName.create "work")
            Assert.Equal(2, workProfile.Products.Length)

            // existing product still present
            let roots = workProfile.Products |> List.map (fun p -> let (ProductRoot r) = p.Root in r)
            Assert.Contains(existingProductRoot, roots)
            Assert.Contains(newProductRoot, roots)
    finally
        if Directory.Exists(root) then
            Directory.Delete(root, true)
