module Itr.Tests.Communication.PortfolioDomainTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Features

module DomainPortfolio = Itr.Domain.Portfolio
module FeaturePortfolio = Itr.Features.Portfolio

let private getResult =
    function
    | Ok value -> value
    | Error error -> failwithf "expected Ok, got %A" error

/// Build a ProductRef with a bare root path.
let private mkProductRef root = { Root = ProductRoot root }

let private mkProfile name products =
    { Name = ProfileName.create name
      Products = products
      GitIdentity = None
      AgentConfig = { Protocol = "opencode-http"; Command = "opencode"; Args = [] } }

/// Test deps for environment operations
type TestEnvDeps(envVars: Map<string, string>) =
    interface IEnvironment with
        member _.GetEnvVar name = envVars |> Map.tryFind name

        member _.HomeDirectory() =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

/// Test deps for filesystem + product-config operations.
/// dirExists controls IFileSystem.DirectoryExists.
/// productDefs maps root path → ProductDefinition (for IProductConfig).
type TestFsDeps(dirExists: string -> bool, productDefs: Map<string, ProductDefinition>) =
    interface IFileSystem with
        member _.ReadFile _ = Error(FileNotFound "not implemented")

        member _.WriteFile _ _ =
            Error(IoException("", "not implemented"))

        member _.FileExists _ = false
        member _.DirectoryExists path = dirExists path

    interface IProductConfig with
        member _.LoadProductConfig root =
            match Map.tryFind root productDefs with
            | Some def -> Ok def
            | None -> Error(ProductConfigError(root, "product.yaml not found (test stub)"))

// ---------------------------------------------------------------------------
// ProfileName slug validation
// ---------------------------------------------------------------------------

[<Theory>]
[<InlineData("my-profile")>]
[<InlineData("work")>]
[<InlineData("dev2")>]
[<InlineData("a")>]
let ``ProfileName.tryCreate accepts valid slugs`` name =
    match ProfileName.tryCreate name with
    | Ok parsed -> Assert.Equal(name, ProfileName.value parsed)
    | Error e -> failwithf "expected valid, got error: %A" e

[<Theory>]
[<InlineData("")>]
[<InlineData("   ")>]
let ``ProfileName.tryCreate rejects blank names`` name =
    match ProfileName.tryCreate name with
    | Ok _ -> failwith "expected invalid slug"
    | Error(InvalidProfileName(value, _)) -> Assert.Equal(name, value)
    | Error other -> failwithf "expected InvalidProfileName, got %A" other

[<Theory>]
[<InlineData("MyProfile")>]
[<InlineData("WORK")>]
let ``ProfileName.tryCreate rejects uppercase names`` name =
    match ProfileName.tryCreate name with
    | Ok _ -> failwith "expected invalid slug"
    | Error(InvalidProfileName(value, _)) -> Assert.Equal(name, value)
    | Error other -> failwithf "expected InvalidProfileName, got %A" other

[<Theory>]
[<InlineData("my profile")>]
[<InlineData("my_profile")>]
[<InlineData("-bad")>]
let ``ProfileName.tryCreate rejects names with spaces or special characters`` name =
    match ProfileName.tryCreate name with
    | Ok _ -> failwith "expected invalid slug"
    | Error(InvalidProfileName(value, _)) -> Assert.Equal(name, value)
    | Error other -> failwithf "expected InvalidProfileName, got %A" other

// ---------------------------------------------------------------------------
// ProductId slug validation
// ---------------------------------------------------------------------------

[<Theory>]
[<InlineData("abc")>]
[<InlineData("a1")>]
[<InlineData("my-lib")>]
let ``ProductId accepts valid slugs`` id =
    match ProductId.tryCreate id with
    | Ok parsed -> Assert.Equal(id, ProductId.value parsed)
    | Error e -> failwithf "expected valid, got error: %A" e

[<Theory>]
[<InlineData("MyLib")>]
[<InlineData("my_lib")>]
[<InlineData("my lib")>]
[<InlineData("-foo")>]
let ``ProductId rejects invalid slugs`` id =
    match ProductId.tryCreate id with
    | Ok _ -> failwith "expected invalid slug"
    | Error(InvalidProductId(value, _)) -> Assert.Equal(id, value)
    | Error other -> failwithf "expected InvalidProductId, got %A" other

// ---------------------------------------------------------------------------
// resolveActiveProfile
// ---------------------------------------------------------------------------

[<Fact>]
let ``resolveActiveProfile precedence flag over env over default and error when unresolved`` () =
    let work = mkProfile "work" []
    let personal = mkProfile "personal" []

    let portfolio =
        DomainPortfolio.tryCreate (Some(ProfileName.create "work")) [ work; personal ]
        |> Result.defaultWith (fun e -> failwithf "%A" e)

    let depsWithPersonalEnv = TestEnvDeps(Map.ofList [ "ITR_PROFILE", "personal" ])
    let depsNoEnv = TestEnvDeps(Map.empty)

    let fromFlag =
        FeaturePortfolio.resolveActiveProfile portfolio (Some "work")
        |> Effect.run depsWithPersonalEnv

    let fromEnv =
        FeaturePortfolio.resolveActiveProfile portfolio None
        |> Effect.run depsWithPersonalEnv

    let fromDefault =
        FeaturePortfolio.resolveActiveProfile portfolio None |> Effect.run depsNoEnv

    Assert.Equal("work", fromFlag |> Result.map (fun p -> p.Name |> ProfileName.value) |> getResult)
    Assert.Equal("personal", fromEnv |> Result.map (fun p -> p.Name |> ProfileName.value) |> getResult)
    Assert.Equal("work", fromDefault |> Result.map (fun p -> p.Name |> ProfileName.value) |> getResult)

    let portfolioNoDefault =
        DomainPortfolio.tryCreate None [ work; personal ]
        |> Result.defaultWith (fun e -> failwithf "%A" e)

    match
        FeaturePortfolio.resolveActiveProfile portfolioNoDefault None
        |> Effect.run depsNoEnv
    with
    | Error(ProfileNotFound _) -> Assert.True(true)
    | other -> failwithf "expected ProfileNotFound, got %A" other

[<Fact>]
let ``resolveActiveProfile lookup is case-insensitive`` () =
    let work = mkProfile "work" []

    let portfolio =
        DomainPortfolio.tryCreate None [ work ]
        |> Result.defaultWith (fun e -> failwithf "%A" e)

    let deps = TestEnvDeps(Map.empty)

    match FeaturePortfolio.resolveActiveProfile portfolio (Some "WORK") |> Effect.run deps with
    | Ok profile -> Assert.Equal("work", profile.Name |> ProfileName.value)
    | Error e -> failwithf "expected success, got %A" e

// ---------------------------------------------------------------------------
// Helpers to build a ProductDefinition for test stubs
// ---------------------------------------------------------------------------

let private mkDefinition idStr root =
    let productId =
        ProductId.tryCreate idStr |> Result.defaultWith (fun e -> failwithf "%A" e)

    let coordRoot =
        { Mode = Standalone
          AbsolutePath = Path.Combine(root, ".itr") }

    { Id = productId
      Repos = Map.empty
      Docs = Map.empty
      Coordination =
        { Mode = "standalone"
          Repo = None
          Path = None }
      CoordRoot = coordRoot }

// ---------------------------------------------------------------------------
// resolveProduct
// ---------------------------------------------------------------------------

[<Fact>]
let ``resolveProduct succeeds when coordRoot dir exists`` () =
    let root = "/tmp/api"

    let definition = mkDefinition "api" root

    let profile = mkProfile "work" [ mkProductRef root ]

    let deps = TestFsDeps((fun _ -> true), Map.ofList [ root, definition ])

    match FeaturePortfolio.resolveProduct profile "api" |> Effect.run deps with
    | Ok resolved -> Assert.EndsWith(".itr", resolved.CoordRoot.AbsolutePath)
    | Error e -> failwithf "expected success, got %A" e

[<Fact>]
let ``resolveProduct returns CoordRootNotFound when dir missing`` () =
    let root = "/tmp/api"
    let definition = mkDefinition "api" root
    let profile = mkProfile "work" [ mkProductRef root ]

    let deps = TestFsDeps((fun _ -> false), Map.ofList [ root, definition ])

    match FeaturePortfolio.resolveProduct profile "api" |> Effect.run deps with
    | Error(CoordRootNotFound(productId, path)) ->
        Assert.Equal("api", productId)
        Assert.EndsWith(".itr", path)
    | other -> failwithf "expected CoordRootNotFound, got %A" other

[<Fact>]
let ``resolveProduct returns ProductNotFound for unknown id`` () =
    let root = "/tmp/api"
    let definition = mkDefinition "api" root
    let profile = mkProfile "work" [ mkProductRef root ]

    let deps = TestFsDeps((fun _ -> true), Map.ofList [ root, definition ])

    match FeaturePortfolio.resolveProduct profile "web" |> Effect.run deps with
    | Error(ProductNotFound productId) -> Assert.Equal("web", productId)
    | other -> failwithf "expected ProductNotFound, got %A" other

// ---------------------------------------------------------------------------
// Duplicate product id detection (use-case level)
// ---------------------------------------------------------------------------

[<Fact>]
let ``resolveProduct returns DuplicateProductId when two roots share same canonical id`` () =
    let rootA = "/tmp/product-a"
    let rootB = "/tmp/product-b"

    let defA = mkDefinition "same-id" rootA
    let defB = mkDefinition "same-id" rootB

    let profile = mkProfile "work" [ mkProductRef rootA; mkProductRef rootB ]

    let deps = TestFsDeps((fun _ -> true), Map.ofList [ rootA, defA; rootB, defB ])

    match FeaturePortfolio.resolveProduct profile "same-id" |> Effect.run deps with
    | Error(DuplicateProductId(profileName, productId)) ->
        Assert.Equal("work", profileName)
        Assert.Equal("same-id", productId)
    | other -> failwithf "expected DuplicateProductId, got %A" other

// ---------------------------------------------------------------------------
// addProfile use-case tests
// ---------------------------------------------------------------------------

/// Stub IPortfolioConfig that returns a fixed portfolio for LoadConfig,
/// with no-op SaveConfig.
type StubPortfolioConfig(portfolio: Portfolio) =
    interface IPortfolioConfig with
        member _.ConfigPath() = "/stub/itr.json"
        member _.LoadConfig _ = Ok portfolio
        member _.SaveConfig _ _ = Ok()

let private mkPortfolio (defaultProfileName: string option) (profiles: Profile list) =
    DomainPortfolio.tryCreate (defaultProfileName |> Option.map ProfileName.create) profiles
    |> Result.defaultWith (fun e -> failwithf "failed to build portfolio: %A" e)

[<Fact>]
let ``addProfile returns updated portfolio with new profile`` () =
    let existing = mkProfile "work" []
    let portfolio = mkPortfolio (Some "work") [ existing ]
    let deps = StubPortfolioConfig(portfolio)

    let input: FeaturePortfolio.AddProfileInput =
        { Name = "personal"; GitIdentity = None; SetAsDefault = false }

    match FeaturePortfolio.addProfile "/stub/itr.json" input |> Effect.run deps with
    | Ok updated ->
        Assert.True(updated.Profiles |> Map.containsKey (ProfileName.create "personal"))
        Assert.True(updated.Profiles |> Map.containsKey (ProfileName.create "work"))
    | Error e -> failwithf "expected Ok, got %A" e

[<Fact>]
let ``addProfile returns DuplicateProfileName for existing name`` () =
    let existing = mkProfile "work" []
    let portfolio = mkPortfolio (Some "work") [ existing ]
    let deps = StubPortfolioConfig(portfolio)

    let input: FeaturePortfolio.AddProfileInput =
        { Name = "work"; GitIdentity = None; SetAsDefault = false }

    match FeaturePortfolio.addProfile "/stub/itr.json" input |> Effect.run deps with
    | Error(DuplicateProfileName name) -> Assert.Equal("work", name)
    | other -> failwithf "expected DuplicateProfileName, got %A" other

[<Fact>]
let ``addProfile with setAsDefault updates DefaultProfile`` () =
    let existing = mkProfile "work" []
    let portfolio = mkPortfolio (Some "work") [ existing ]
    let deps = StubPortfolioConfig(portfolio)

    let input: FeaturePortfolio.AddProfileInput =
        { Name = "personal"; GitIdentity = None; SetAsDefault = true }

    match FeaturePortfolio.addProfile "/stub/itr.json" input |> Effect.run deps with
    | Ok updated ->
        Assert.Equal(Some "personal", updated.DefaultProfile |> Option.map ProfileName.value)
    | Error e -> failwithf "expected Ok, got %A" e

[<Fact>]
let ``addProfile returns InvalidProfileName for invalid name`` () =
    let portfolio = mkPortfolio None []
    let deps = StubPortfolioConfig(portfolio)

    let input: FeaturePortfolio.AddProfileInput =
        { Name = "My Work"; GitIdentity = None; SetAsDefault = false }

    match FeaturePortfolio.addProfile "/stub/itr.json" input |> Effect.run deps with
    | Error(InvalidProfileName(value, _)) -> Assert.Equal("My Work", value)
    | other -> failwithf "expected InvalidProfileName, got %A" other

// ---------------------------------------------------------------------------
// registerProduct use-case tests
// ---------------------------------------------------------------------------

/// Combined test deps for registerProduct: IPortfolioConfig + IProductConfig + IFileSystem.
/// dirExists: controls IFileSystem.DirectoryExists
/// productDefs: maps root path → ProductDefinition
/// portfolio: the portfolio returned by LoadConfig
type TestRegisterDeps
    (
        portfolio: Portfolio,
        dirExists: string -> bool,
        productDefs: Map<string, ProductDefinition>
    ) =
    interface IPortfolioConfig with
        member _.ConfigPath() = "/stub/itr.json"
        member _.LoadConfig _ = Ok portfolio
        member _.SaveConfig _ _ = Ok()

    interface IFileSystem with
        member _.ReadFile _ = Error(FileNotFound "not implemented")
        member _.WriteFile _ _ = Error(IoException("", "not implemented"))
        member _.FileExists _ = false
        member _.DirectoryExists path = dirExists path

    interface IProductConfig with
        member _.LoadProductConfig root =
            match Map.tryFind root productDefs with
            | Some def -> Ok def
            | None -> Error(ProductConfigError(root, "product.yaml not found (test stub)"))

let private mkRegPortfolio profileName products =
    let profile =
        { Name = ProfileName.create profileName
          Products = products
          GitIdentity = None
          AgentConfig = { Protocol = "opencode-http"; Command = "opencode"; Args = [] } }

    DomainPortfolio.tryCreate (Some(ProfileName.create profileName)) [ profile ]
    |> Result.defaultWith (fun e -> failwithf "failed to build portfolio: %A" e)

[<Fact>]
let ``registerProduct valid path adds ProductRef to active profile and returns updated Portfolio`` () =
    let root = "/tmp/my-product"
    let definition = mkDefinition "my-product" root

    let portfolio = mkRegPortfolio "work" []

    let deps =
        TestRegisterDeps(portfolio, (fun _ -> true), Map.ofList [ root, definition ])

    let input: FeaturePortfolio.RegisterProductInput =
        { Path = root; Profile = None }

    match FeaturePortfolio.registerProduct "/stub/itr.json" input |> Effect.run deps with
    | Ok updated ->
        let profile = updated.Profiles |> Map.find (ProfileName.create "work")
        Assert.Equal(1, profile.Products.Length)
        Assert.Equal(ProductRoot root, profile.Products.[0].Root)
    | Error e -> failwithf "expected Ok, got %A" e

[<Fact>]
let ``registerProduct duplicate canonical id returns DuplicateProductId portfolio unchanged`` () =
    let root = "/tmp/my-product"
    let root2 = "/tmp/my-product-2"
    let definition = mkDefinition "my-product" root
    let definition2 = mkDefinition "my-product" root2

    let portfolio = mkRegPortfolio "work" [ mkProductRef root ]

    let deps =
        TestRegisterDeps(
            portfolio,
            (fun _ -> true),
            Map.ofList [ root, definition; root2, definition2 ]
        )

    let input: FeaturePortfolio.RegisterProductInput =
        { Path = root2; Profile = None }

    match FeaturePortfolio.registerProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(DuplicateProductId(profileName, productId)) ->
        Assert.Equal("work", profileName)
        Assert.Equal("my-product", productId)
    | other -> failwithf "expected DuplicateProductId, got %A" other

[<Fact>]
let ``registerProduct non-existent directory returns ProductConfigError portfolio unchanged`` () =
    let root = "/tmp/nonexistent-product"

    let portfolio = mkRegPortfolio "work" []

    let deps =
        TestRegisterDeps(portfolio, (fun _ -> false), Map.empty)

    let input: FeaturePortfolio.RegisterProductInput =
        { Path = root; Profile = None }

    match FeaturePortfolio.registerProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(ProductConfigError _) -> Assert.True(true)
    | other -> failwithf "expected ProductConfigError, got %A" other

[<Fact>]
let ``registerProduct missing product yaml propagates ProductConfigError`` () =
    let root = "/tmp/product-no-yaml"

    let portfolio = mkRegPortfolio "work" []

    // dirExists returns true but productDefs is empty → LoadProductConfig returns Error
    let deps =
        TestRegisterDeps(portfolio, (fun _ -> true), Map.empty)

    let input: FeaturePortfolio.RegisterProductInput =
        { Path = root; Profile = None }

    match FeaturePortfolio.registerProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(ProductConfigError _) -> Assert.True(true)
    | other -> failwithf "expected ProductConfigError, got %A" other

[<Fact>]
let ``registerProduct named profile not found returns ProfileNotFound`` () =
    let root = "/tmp/product"
    let definition = mkDefinition "my-product" root

    let portfolio = mkRegPortfolio "work" []

    let deps =
        TestRegisterDeps(portfolio, (fun _ -> true), Map.ofList [ root, definition ])

    let input: FeaturePortfolio.RegisterProductInput =
        { Path = root; Profile = Some "nonexistent" }

    match FeaturePortfolio.registerProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(ProfileNotFound name) -> Assert.Equal("nonexistent", name)
    | other -> failwithf "expected ProfileNotFound, got %A" other

/// Test deps for initProduct.
/// dirExists: controls IFileSystem.DirectoryExists
/// fileExists: controls IFileSystem.FileExists
/// writtenFiles: mutable set tracking written paths
type TestInitDeps
    (
        dirExists: string -> bool,
        fileExists: string -> bool,
        writtenFiles: System.Collections.Generic.List<string * string>,
        portfolio: Portfolio
    ) =
    interface IFileSystem with
        member _.ReadFile _ = Error(FileNotFound "not implemented")

        member _.WriteFile path content =
            writtenFiles.Add((path, content))
            Ok()

        member _.FileExists path = fileExists path
        member _.DirectoryExists path = dirExists path

    interface IPortfolioConfig with
        member _.ConfigPath() = "/stub/itr.json"
        member _.LoadConfig _ = Ok portfolio

        member _.SaveConfig _ _ = Ok()

    interface IProductConfig with
        member _.LoadProductConfig root =
            // Return a stub definition using the directory name as the product id
            let dirName =
                System.IO.Path.GetFileName(root.TrimEnd(System.IO.Path.DirectorySeparatorChar))

            // Try to parse as a valid slug; fall back to a fixed id if not
            match ProductId.tryCreate dirName with
            | Ok id ->
                let coordRoot =
                    { Mode = Standalone
                      AbsolutePath = System.IO.Path.Combine(root, ".itr") }

                Ok
                    { Id = id
                      Repos = Map.empty
                      Docs = Map.empty
                      Coordination =
                        { Mode = "standalone"
                          Repo = None
                          Path = None }
                      CoordRoot = coordRoot }
            | Error _ ->
                Error(ProductConfigError(root, "product.yaml not found (test stub)"))

let private mkInitPortfolio () =
    let profile =
        { Name = ProfileName.create "default"
          Products = []
          GitIdentity = None
          AgentConfig = { Protocol = "opencode-http"; Command = "opencode"; Args = [] } }

    DomainPortfolio.tryCreate (Some(ProfileName.create "default")) [ profile ]
    |> Result.defaultWith (fun e -> failwithf "failed to build test portfolio: %A" e)

let private defaultInitInput idStr path =
    { Portfolio.InitProductInput.Id = idStr
      Portfolio.InitProductInput.Path = path
      Portfolio.InitProductInput.RepoId = "my-repo"
      Portfolio.InitProductInput.CoordPath = ".itr"
      Portfolio.InitProductInput.CoordinationMode = "primary-repo"
      Portfolio.InitProductInput.RegisterProfile = None }

[<Fact>]
let ``initProduct with valid inputs and RegisterProfile=None writes all files and returns None`` () =
    let path = "/tmp/test-product"
    let written = System.Collections.Generic.List<string * string>()
    let portfolio = mkInitPortfolio ()

    let deps =
        TestInitDeps((fun _ -> true), (fun _ -> false), written, portfolio)

    let input = defaultInitInput "my-product" path

    match FeaturePortfolio.initProduct "/stub/itr.json" input |> Effect.run deps with
    | Ok None ->
        let paths = written |> Seq.map fst |> Seq.toList
        Assert.Contains(IO.Path.Combine(path, "product.yaml"), paths)
        Assert.Contains(IO.Path.Combine(path, ".itr", ".gitkeep"), paths)
        Assert.Contains(IO.Path.Combine(path, "PRODUCT.md"), paths)
        Assert.Contains(IO.Path.Combine(path, "ARCHITECTURE.md"), paths)
    | other -> failwithf "expected Ok None, got %A" other

[<Fact>]
let ``initProduct with RegisterProfile=Some writes files and returns Some updatedPortfolio`` () =
    let path = IO.Path.GetFullPath("/tmp/test-product-reg")
    let written = System.Collections.Generic.List<string * string>()
    let portfolio = mkInitPortfolio ()

    let deps =
        TestInitDeps((fun _ -> true), (fun _ -> false), written, portfolio)

    let input =
        { defaultInitInput "my-product" path with
            RegisterProfile = Some "default" }

    match FeaturePortfolio.initProduct "/stub/itr.json" input |> Effect.run deps with
    | Ok(Some updatedPortfolio) ->
        let paths = written |> Seq.map fst |> Seq.toList
        Assert.Contains(IO.Path.Combine(path, "product.yaml"), paths)

        let profile =
            updatedPortfolio.Profiles |> Map.find (ProfileName.create "default")

        Assert.Equal(1, profile.Products.Length)
    | other -> failwithf "expected Ok Some, got %A" other

[<Fact>]
let ``initProduct when product.yaml already exists returns ProductConfigError and no files written`` () =
    let path = "/tmp/test-product-exists"
    let written = System.Collections.Generic.List<string * string>()
    let portfolio = mkInitPortfolio ()

    let fileExistsStub p =
        p = IO.Path.Combine(path, "product.yaml")

    let deps =
        TestInitDeps((fun _ -> true), fileExistsStub, written, portfolio)

    let input = defaultInitInput "my-product" path

    match FeaturePortfolio.initProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(ProductConfigError _) -> Assert.Empty(written)
    | other -> failwithf "expected ProductConfigError, got %A" other

[<Fact>]
let ``initProduct when path directory does not exist returns ProductConfigError`` () =
    let path = "/tmp/nonexistent"
    let written = System.Collections.Generic.List<string * string>()
    let portfolio = mkInitPortfolio ()

    let deps =
        TestInitDeps((fun _ -> false), (fun _ -> false), written, portfolio)

    let input = defaultInitInput "my-product" path

    match FeaturePortfolio.initProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(ProductConfigError _) -> Assert.Empty(written)
    | other -> failwithf "expected ProductConfigError, got %A" other

[<Fact>]
let ``initProduct with invalid id returns InvalidProductId`` () =
    let path = "/tmp/test-product"
    let written = System.Collections.Generic.List<string * string>()
    let portfolio = mkInitPortfolio ()

    let deps =
        TestInitDeps((fun _ -> true), (fun _ -> false), written, portfolio)

    let input = defaultInitInput "INVALID_ID" path

    match FeaturePortfolio.initProduct "/stub/itr.json" input |> Effect.run deps with
    | Error(InvalidProductId(value, _)) ->
        Assert.Equal("INVALID_ID", value)
        Assert.Empty(written)
    | other -> failwithf "expected InvalidProductId, got %A" other

[<Fact>]
let ``initProduct with CoordinationMode=standalone writes yaml without coordination.repo`` () =
    let path = "/tmp/test-standalone"
    let written = System.Collections.Generic.List<string * string>()
    let portfolio = mkInitPortfolio ()

    let deps =
        TestInitDeps((fun _ -> true), (fun _ -> false), written, portfolio)

    let input =
        { defaultInitInput "my-product" path with
            CoordinationMode = "standalone" }

    match FeaturePortfolio.initProduct "/stub/itr.json" input |> Effect.run deps with
    | Ok None ->
        let yamlPath = IO.Path.Combine(path, "product.yaml")

        let yamlContent =
            written
            |> Seq.tryFind (fun (p, _) -> p = yamlPath)
            |> Option.map snd
            |> Option.defaultValue ""

        Assert.Contains("mode: standalone", yamlContent)
        Assert.DoesNotContain("coordination.repo", yamlContent)
        Assert.DoesNotContain("repo: my-repo", yamlContent)
    | other -> failwithf "expected Ok None, got %A" other
