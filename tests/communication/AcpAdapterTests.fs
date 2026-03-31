module Itr.Tests.Communication.AcpAdapterTests

open System
open System.IO
open Xunit
open Itr.Adapters

// ---------------------------------------------------------------------------
// 6.1 buildInitialize
// ---------------------------------------------------------------------------

[<Fact>]
let ``buildInitialize returns valid JSON-RPC shape`` () =
    let msg = AcpMessages.buildInitialize ()
    use doc = System.Text.Json.JsonDocument.Parse(msg)
    let root = doc.RootElement

    Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString())
    Assert.Equal(0, root.GetProperty("id").GetInt32())
    Assert.Equal("initialize", root.GetProperty("method").GetString())

// ---------------------------------------------------------------------------
// 6.2 buildSessionNew
// ---------------------------------------------------------------------------

[<Fact>]
let ``buildSessionNew includes cwd in params`` () =
    let msg = AcpMessages.buildSessionNew 1 "/my/project"
    use doc = System.Text.Json.JsonDocument.Parse(msg)
    let root = doc.RootElement

    Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString())
    Assert.Equal(1, root.GetProperty("id").GetInt32())
    Assert.Equal("session/new", root.GetProperty("method").GetString())
    Assert.Equal("/my/project", root.GetProperty("params").GetProperty("cwd").GetString())

// ---------------------------------------------------------------------------
// 6.3 buildSessionPrompt
// ---------------------------------------------------------------------------

[<Fact>]
let ``buildSessionPrompt includes sessionId and prompt content block`` () =
    let msg = AcpMessages.buildSessionPrompt 2 "sess-123" "Hello agent"
    use doc = System.Text.Json.JsonDocument.Parse(msg)
    let root = doc.RootElement

    Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString())
    Assert.Equal(2, root.GetProperty("id").GetInt32())
    Assert.Equal("session/prompt", root.GetProperty("method").GetString())

    let paramsEl = root.GetProperty("params")
    Assert.Equal("sess-123", paramsEl.GetProperty("sessionId").GetString())

    let prompt = paramsEl.GetProperty("prompt")
    Assert.Equal(1, prompt.GetArrayLength())

    let block = prompt.[0]
    Assert.Equal("text", block.GetProperty("type").GetString())
    Assert.Equal("Hello agent", block.GetProperty("text").GetString())

// ---------------------------------------------------------------------------
// 6.4 extractChunkText
// ---------------------------------------------------------------------------

[<Fact>]
let ``extractChunkText returns Some text from valid session/update message`` () =
    let json =
        """{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"sess-abc","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"Hello"}}}}"""
    let result = AcpMessages.extractChunkText json
    Assert.Equal(Some "Hello", result)

[<Fact>]
let ``extractChunkText returns None for non-session/update message`` () =
    let json = """{"jsonrpc":"2.0","id":1,"result":{"sessionId":"abc"}}"""
    let result = AcpMessages.extractChunkText json
    Assert.Equal(None, result)

[<Fact>]
let ``extractChunkText returns None for malformed input`` () =
    let result = AcpMessages.extractChunkText "not json at all"
    Assert.Equal(None, result)

[<Fact>]
let ``extractChunkText returns None when content has no agent_message_chunk blocks`` () =
    let json =
        """{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"sess-abc","update":{"sessionUpdate":"tool_call","content":{"type":"text","text":"ignored"}}}}"""
    let result = AcpMessages.extractChunkText json
    Assert.Equal(None, result)

// ---------------------------------------------------------------------------
// 6.5 extractSessionId
// ---------------------------------------------------------------------------

[<Fact>]
let ``extractSessionId returns Ok sessionId from valid session/new response`` () =
    let json = """{"jsonrpc":"2.0","id":1,"result":{"sessionId":"sess-abc"}}"""
    match AcpMessages.extractSessionId json with
    | Ok sid -> Assert.Equal("sess-abc", sid)
    | Error e -> Assert.Fail($"Expected Ok, got Error: {e}")

[<Fact>]
let ``extractSessionId returns Error on malformed input`` () =
    match AcpMessages.extractSessionId "not json" with
    | Ok sid -> Assert.Fail($"Expected Error, got Ok: {sid}")
    | Error _ -> () // expected

[<Fact>]
let ``extractSessionId returns Error when sessionId field is missing`` () =
    let json = """{"jsonrpc":"2.0","id":1,"result":{}}"""
    match AcpMessages.extractSessionId json with
    | Ok sid -> Assert.Fail($"Expected Error, got Ok: {sid}")
    | Error _ -> () // expected

[<Fact>]
let ``extractSessionId returns Error on JSON-RPC error response`` () =
    let json = """{"jsonrpc":"2.0","id":1,"error":{"code":-32600,"message":"Invalid Request"}}"""
    match AcpMessages.extractSessionId json with
    | Ok sid -> Assert.Fail($"Expected Error, got Ok: {sid}")
    | Error msg -> Assert.Contains("Invalid Request", msg)

// ---------------------------------------------------------------------------
// 6.6 LoadLocalConfig
// ---------------------------------------------------------------------------

[<Fact>]
let ``LoadLocalConfig returns Some AgentConfig when file present with agent section`` () =
    let dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    Directory.CreateDirectory(dir) |> ignore
    try
        let json = """{"agent":{"protocol":"acp","command":"my-agent","args":["--flag"]}}"""
        File.WriteAllText(Path.Combine(dir, "itr.json"), json)
        match PortfolioAdapter.LoadLocalConfig dir with
        | None -> Assert.Fail("Expected Some, got None")
        | Some config ->
            Assert.Equal("acp", config.Protocol)
            Assert.Equal("my-agent", config.Command)
            Assert.Equal<string list>(["--flag"], config.Args)
    finally
        Directory.Delete(dir, true)

[<Fact>]
let ``LoadLocalConfig returns None when file is absent`` () =
    let dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    Directory.CreateDirectory(dir) |> ignore
    try
        let result = PortfolioAdapter.LoadLocalConfig dir
        Assert.Equal(None, result)
    finally
        Directory.Delete(dir, true)

[<Fact>]
let ``LoadLocalConfig returns None when file is present but has no agent section`` () =
    let dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    Directory.CreateDirectory(dir) |> ignore
    try
        let json = """{"someOtherField":"value"}"""
        File.WriteAllText(Path.Combine(dir, "itr.json"), json)
        let result = PortfolioAdapter.LoadLocalConfig dir
        Assert.Equal(None, result)
    finally
        Directory.Delete(dir, true)
