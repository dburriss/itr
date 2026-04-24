namespace Itr.Adapters

open System
open System.Diagnostics
open System.Text
open System.Text.Json
open Itr.Domain

// ---------------------------------------------------------------------------
// ACP JSON-RPC message construction (pure functions)
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module AcpMessages =

    /// Build an ACP `initialize` request (id=0).
    let buildInitialize () : string =
        """{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":1,"clientCapabilities":{},"clientInfo":{"name":"itr","version":"1.0.0"}}}"""

    /// Build an ACP `session/new` request with the given cwd.
    let buildSessionNew (id: int) (cwd: string) : string =
        let escapedCwd = cwd.Replace("\\", "\\\\").Replace("\"", "\\\"")

        sprintf
            """{"jsonrpc":"2.0","id":%d,"method":"session/new","params":{"cwd":"%s","mcpServers":[]}}"""
            id
            escapedCwd

    /// Build an ACP `session/prompt` request with the given session id and prompt text.
    let buildSessionPrompt (id: int) (sessionId: string) (prompt: string) : string =
        let escapedPrompt =
            prompt
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")

        let escapedSessionId = sessionId.Replace("\\", "\\\\").Replace("\"", "\\\"")

        sprintf
            """{"jsonrpc":"2.0","id":%d,"method":"session/prompt","params":{"sessionId":"%s","prompt":[{"type":"text","text":"%s"}]}}"""
            id
            escapedSessionId
            escapedPrompt

    // ---------------------------------------------------------------------------
    // Parsing helpers (pure functions)
    // ---------------------------------------------------------------------------

    /// Extract the session id from a `session/new` response JSON line.
    /// Returns `Ok sessionId` on success or `Error message` if not found.
    let extractSessionId (json: string) : Result<string, string> =
        try
            use doc = JsonDocument.Parse(json)
            let root = doc.RootElement

            // Check for JSON-RPC error
            if root.TryGetProperty("error", ref Unchecked.defaultof<_>) then
                let errEl = root.GetProperty("error")

                let msg =
                    if errEl.TryGetProperty("message", ref Unchecked.defaultof<_>) then
                        errEl.GetProperty("message").GetString()
                    else
                        json

                Error $"ACP session/new error: {msg}"
            else
                match root.TryGetProperty("result") with
                | true, resultEl ->
                    match resultEl.TryGetProperty("sessionId") with
                    | true, sidEl -> Ok(sidEl.GetString())
                    | _ -> Error $"ACP session/new response missing 'result.sessionId': {json}"
                | _ -> Error $"ACP session/new response missing 'result': {json}"
        with ex ->
            Error $"Failed to parse ACP session/new response: {ex.Message}"

    /// Extract the text content from an ACP `session/update` chunk message.
    /// Returns `Some text` when the message contains an `agent_message_chunk` update, otherwise `None`.
    /// Expected shape: {"method":"session/update","params":{"update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"..."}}}}
    let extractChunkText (json: string) : string option =
        try
            use doc = JsonDocument.Parse(json)
            let root = doc.RootElement

            match root.TryGetProperty("method") with
            | true, methodEl when methodEl.GetString() = "session/update" ->
                match root.TryGetProperty("params") with
                | true, paramsEl ->
                    match paramsEl.TryGetProperty("update") with
                    | true, updateEl ->
                        match updateEl.TryGetProperty("sessionUpdate") with
                        | true, suEl when suEl.GetString() = "agent_message_chunk" ->
                            match updateEl.TryGetProperty("content") with
                            | true, contentEl ->
                                match contentEl.TryGetProperty("text") with
                                | true, textEl ->
                                    let text = textEl.GetString()

                                    if not (isNull text) && text.Length > 0 then
                                        Some text
                                    else
                                        None
                                | _ -> None
                            | _ -> None
                        | _ -> None
                    | _ -> None
                | _ -> None
            | _ -> None
        with _ ->
            None

    /// Trim any preamble (thinking, explanation) that a model may have emitted before the plan document.
    /// Finds the first occurrence of a markdown `#` heading at the start of a line and returns the
    /// content from that point onward. If no such heading is found the original string is returned unchanged.
    let trimPreamble (content: string) : string =
        if content.StartsWith("#") then
            content
        else
            match content.IndexOf("\n#") with
            | -1 -> content
            | idx -> content.[idx + 1 ..]

    /// Check whether a JSON line is the final `session/prompt` response (has a result, not just a notification).
    let isFinalResponse (json: string) : bool =
        try
            use doc = JsonDocument.Parse(json)
            let root = doc.RootElement
            // Final response is a JSON-RPC response with an "id" field and a "result" field
            // (not a notification which lacks "id")
            let hasId =
                match root.TryGetProperty("id") with
                | true, idEl -> idEl.ValueKind <> JsonValueKind.Null
                | _ -> false

            let hasResult = fst (root.TryGetProperty("result"))
            let hasError = fst (root.TryGetProperty("error"))
            hasId && (hasResult || hasError)
        with _ ->
            false

// ---------------------------------------------------------------------------
// ACP Harness Adapter
// ---------------------------------------------------------------------------

/// Adapter that launches an ACP-compatible agent subprocess and communicates
/// via JSON-RPC 2.0 over stdin/stdout.
type AcpHarnessAdapter(command: string, args: string list, cwd: string) =

    let debugPrint (debug: bool) (label: string) (body: string) =
        if debug then
            eprintfn "[debug][acp] %s: %s" label body

    interface IAgentHarness with
        member _.Prompt (prompt: string) (debug: bool) : Result<string, string> =
            let argStr = args |> String.concat " "
            let psi = ProcessStartInfo(command, argStr)
            psi.UseShellExecute <- false
            psi.RedirectStandardInput <- true
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true

            psi.WorkingDirectory <-
                if String.IsNullOrWhiteSpace(cwd) then
                    Environment.CurrentDirectory
                else
                    cwd

            psi.StandardInputEncoding <- Encoding.UTF8
            psi.StandardOutputEncoding <- Encoding.UTF8

            let proc =
                try
                    Process.Start(psi) |> Ok
                with ex ->
                    Error $"Failed to start ACP agent '{command}': {ex.Message}"

            match proc with
            | Error e -> Error e
            | Ok p ->
                try
                    let stdin = p.StandardInput
                    let stdout = p.StandardOutput
                    let stderr = p.StandardError

                    // Drain stderr asynchronously so it does not block the process
                    let stderrLines = System.Collections.Concurrent.ConcurrentQueue<string>()

                    let stderrTask =
                        System.Threading.Tasks.Task.Run(fun () ->
                            let mutable line = stderr.ReadLine()

                            while not (isNull line) do
                                stderrLines.Enqueue(line)
                                line <- stderr.ReadLine())

                    let writeLine (msg: string) =
                        debugPrint debug "send" msg
                        stdin.WriteLine(msg)
                        stdin.Flush()

                    let readLine () =
                        // Use a 120-second timeout via async
                        let readTask = System.Threading.Tasks.Task.Run(fun () -> stdout.ReadLine())

                        if readTask.Wait(TimeSpan.FromSeconds(120.0)) then
                            readTask.Result
                        else
                            null // timeout

                    // Step 1: initialize
                    let initMsg = AcpMessages.buildInitialize ()
                    writeLine initMsg

                    // Read initialize response
                    let initResp = readLine ()

                    if isNull initResp then
                        p.Kill()
                        Error "ACP agent timed out waiting for initialize response"
                    else
                        debugPrint debug "recv" initResp

                        // Step 2: session/new
                        let sessionNewMsg = AcpMessages.buildSessionNew 1 cwd
                        writeLine sessionNewMsg

                        let sessionNewResp = readLine ()

                        if isNull sessionNewResp then
                            p.Kill()
                            Error "ACP agent timed out waiting for session/new response"
                        else
                            debugPrint debug "recv" sessionNewResp

                            match AcpMessages.extractSessionId sessionNewResp with
                            | Error e ->
                                p.Kill()
                                Error e
                            | Ok sessionId ->

                                // Step 3: session/prompt
                                let promptMsg = AcpMessages.buildSessionPrompt 2 sessionId prompt
                                writeLine promptMsg

                                // Step 4: read chunks until final response
                                let accumulated = StringBuilder()
                                let mutable finished = false
                                let mutable errorMsg = None

                                while not finished do
                                    let line = readLine ()

                                    if isNull line then
                                        finished <- true
                                        errorMsg <- Some "ACP agent timed out waiting for session/prompt response"
                                    else
                                        debugPrint debug "recv" line
                                        // Try to parse as JSON; skip non-JSON lines (debug-log them)
                                        try
                                            use doc = JsonDocument.Parse(line)
                                            ignore doc
                                            // Try to extract a chunk
                                            match AcpMessages.extractChunkText line with
                                            | Some text ->
                                                printf "%s" text
                                                accumulated.Append(text) |> ignore
                                            | None -> ()
                                            // Check if this is the final response
                                            if AcpMessages.isFinalResponse line then
                                                finished <- true
                                        with _ ->
                                            // Non-JSON line — log and skip
                                            debugPrint debug "non-json" line

                                // Drain remaining stderr lines for debug output
                                stderrTask.Wait(TimeSpan.FromSeconds(1.0)) |> ignore
                                let mutable stderrLine = ""

                                while stderrLines.TryDequeue(&stderrLine) do
                                    debugPrint debug "stderr" stderrLine

                                p.Kill()

                                match errorMsg with
                                | Some e -> Error e
                                | None -> Ok(accumulated.ToString())
                with ex ->
                    try
                        p.Kill()
                    with _ ->
                        ()

                    Error $"ACP adapter error: {ex.Message}"
