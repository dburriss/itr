module Itr.Tests.Communication.AgentHarnessSelectorTests

open Xunit
open Itr.Adapters

[<Fact>]
let ``acp protocol returns AcpHarnessAdapter`` () =
    let harness = AgentHarnessSelector.selectHarness "acp" "my-agent" [] "/coord"
    Assert.IsType<AcpHarnessAdapter>(harness) |> ignore

[<Fact>]
let ``opencode-http protocol returns OpenCodeHarnessAdapter`` () =
    let harness = AgentHarnessSelector.selectHarness "opencode-http" "" [] ""
    Assert.IsType<OpenCodeHarnessAdapter>(harness) |> ignore

[<Fact>]
let ``trySelectHarness acp returns Ok`` () =
    let result = AgentHarnessSelector.trySelectHarness "acp" "cmd" [] "/coord"
    Assert.True(result |> Result.isOk)

[<Fact>]
let ``trySelectHarness opencode-http returns Ok`` () =
    let result = AgentHarnessSelector.trySelectHarness "opencode-http" "" [] ""
    Assert.True(result |> Result.isOk)

[<Fact>]
let ``trySelectHarness unknown protocol returns Error`` () =
    let result = AgentHarnessSelector.trySelectHarness "grpc" "" [] ""
    match result with
    | Error msg -> Assert.Contains("grpc", msg)
    | Ok _ -> Assert.Fail("Expected error for unknown protocol")
