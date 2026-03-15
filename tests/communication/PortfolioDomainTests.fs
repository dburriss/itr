module Itr.Tests.Communication.PortfolioDomainTests

open System
open Xunit
open Itr.Domain
open Itr.Features
open Itr.Adapters

module DomainPortfolio = Itr.Domain.Portfolio
module FeaturePortfolio = Itr.Features.Portfolio

let private getResult =
    function
    | Ok value -> value
    | Error error -> failwithf "expected Ok, got %A" error

let private mkProduct id root =
    match ProductId.tryCreate id with
    | Ok productId -> { Id = productId; Root = root }
    | Error e -> failwithf "invalid test product id: %A" e

let private mkProfile name products =
    { Name = ProfileName.create name
      Products = products
      GitIdentity = None }

/// Test deps for environment operations
type TestEnvDeps(envVars: Map<string, string>) =
    interface IEnvironment with
        member _.GetEnvVar name = envVars |> Map.tryFind name

        member _.HomeDirectory() =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

/// Test deps for filesystem operations
type TestFsDeps(dirExists: string -> bool) =
    interface IFileSystem with
        member _.ReadFile _ = Error(FileNotFound "not implemented")

        member _.WriteFile _ _ =
            Error(IoException("", "not implemented"))

        member _.FileExists _ = false
        member _.DirectoryExists path = dirExists path

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

[<Fact>]
let ``Portfolio rejects duplicate ProductId within profile`` () =
    let productA = mkProduct "same-id" (StandaloneConfig "~/a")
    let productB = mkProduct "same-id" (StandaloneConfig "~/b")
    let profile = mkProfile "work" [ productA; productB ]

    match DomainPortfolio.tryCreate None [ profile ] with
    | Ok _ -> failwith "expected duplicate product error"
    | Error(DuplicateProductId(profileName, productId)) ->
        Assert.Equal("work", profileName)
        Assert.Equal("same-id", productId)
    | Error other -> failwithf "unexpected error: %A" other

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

[<Fact>]
let ``resolveProduct succeeds when dirExists returns true`` () =
    let profile = mkProfile "work" [ mkProduct "api" (StandaloneConfig "/tmp/api") ]
    let deps = TestFsDeps(fun _ -> true)

    match FeaturePortfolio.resolveProduct profile "api" |> Effect.run deps with
    | Ok resolved ->
        Assert.Equal("api", ProductId.value resolved.Product.Id)
        Assert.EndsWith(".itr", resolved.CoordRoot.AbsolutePath)
    | Error e -> failwithf "expected success, got %A" e

[<Fact>]
let ``resolveProduct returns CoordRootNotFound when dir missing`` () =
    let profile = mkProfile "work" [ mkProduct "api" (StandaloneConfig "/tmp/api") ]
    let deps = TestFsDeps(fun _ -> false)

    match FeaturePortfolio.resolveProduct profile "api" |> Effect.run deps with
    | Error(CoordRootNotFound(productId, path)) ->
        Assert.Equal("api", productId)
        Assert.EndsWith(".itr", path)
    | other -> failwithf "expected CoordRootNotFound, got %A" other

[<Fact>]
let ``resolveProduct returns ProductNotFound for unknown id`` () =
    let profile = mkProfile "work" [ mkProduct "api" (StandaloneConfig "/tmp/api") ]
    let deps = TestFsDeps(fun _ -> true)

    match FeaturePortfolio.resolveProduct profile "web" |> Effect.run deps with
    | Error(ProductNotFound productId) -> Assert.Equal("web", productId)
    | other -> failwithf "expected ProductNotFound, got %A" other
