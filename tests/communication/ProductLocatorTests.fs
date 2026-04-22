module Itr.Tests.Communication.ProductLocatorTests

open Xunit
open Itr.Domain
open Itr.Adapters

// ---------------------------------------------------------------------------
// In-memory IFileSystem stub
// ---------------------------------------------------------------------------

type InMemoryFs(existingFiles: string list) =
    let files = System.Collections.Generic.HashSet<string>(existingFiles)
    interface IFileSystem with
        member _.ReadFile path =
            if files.Contains(path) then Ok "" else Error(FileNotFound path)
        member _.WriteFile path _ = Ok()
        member _.FileExists path = files.Contains(path)
        member _.DirectoryExists _ = true

// ---------------------------------------------------------------------------
// ProductLocator.locateProductRoot tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``found in start directory`` () =
    let fs = InMemoryFs [ "/myproject/product.yaml" ]
    let result = ProductLocator.locateProductRoot fs "/myproject"
    Assert.Equal(Some "/myproject", result)

[<Fact>]
let ``found in ancestor directory`` () =
    let fs = InMemoryFs [ "/myproject/product.yaml" ]
    let result = ProductLocator.locateProductRoot fs "/myproject/src/cli"
    Assert.Equal(Some "/myproject", result)

[<Fact>]
let ``not found returns None`` () =
    let fs = InMemoryFs []
    let result = ProductLocator.locateProductRoot fs "/some/deep/dir"
    Assert.Equal(None, result)

[<Fact>]
let ``found in immediate parent`` () =
    let fs = InMemoryFs [ "/projects/myapp/product.yaml" ]
    let result = ProductLocator.locateProductRoot fs "/projects/myapp/src"
    Assert.Equal(Some "/projects/myapp", result)
