namespace Itr.Adapters

open System
open System.Net.Http
open System.Text
open Itr.Domain

/// Adapter that implements IAgentHarness against a locally running OpenCode server.
/// Base URL is hardcoded to http://127.0.0.1:4096 per design decision.
type OpenCodeHarnessAdapter() =
    let baseUrl = "http://127.0.0.1:4096"

    // Shared HttpClient — fine for CLI (short-lived process).
    // Timeout.InfiniteTimeSpan: AI responses can take several minutes; default 100s is too short.
    let client = new HttpClient(Timeout = System.Threading.Timeout.InfiniteTimeSpan)

    /// Print a debug line to stderr if debug mode is enabled.
    let debugPrint (debug: bool) (label: string) (body: string) =
        if debug then
            eprintfn "[debug] %s: %s" label body

    /// Extract plain text content from an OpenCode message response JSON.
    /// The response is an array of parts; we concatenate text parts.
    let extractTextContent (json: string) : string =
        // Simple extraction without a JSON library dependency:
        // Response format: [{"type":"text","text":"<content>"},...] or
        // wrapped: {"parts":[...],...}
        // We scan for "text":" patterns and extract the values.
        let parts = System.Collections.Generic.List<string>()
        let mutable i = 0
        let key = "\"text\":\""
        while i < json.Length do
            let idx = json.IndexOf(key, i, StringComparison.Ordinal)
            if idx < 0 then
                i <- json.Length
            else
                let start = idx + key.Length
                // Find closing quote, handling escaped quotes
                let mutable j = start
                let mutable found = false
                let sb = StringBuilder()
                while j < json.Length && not found do
                    if json.[j] = '\\' && j + 1 < json.Length then
                        match json.[j + 1] with
                        | '"' -> sb.Append('"') |> ignore; j <- j + 2
                        | '\\' -> sb.Append('\\') |> ignore; j <- j + 2
                        | 'n' -> sb.Append('\n') |> ignore; j <- j + 2
                        | 'r' -> sb.Append('\r') |> ignore; j <- j + 2
                        | 't' -> sb.Append('\t') |> ignore; j <- j + 2
                        | c -> sb.Append(c) |> ignore; j <- j + 2
                    elif json.[j] = '"' then
                        found <- true
                    else
                        sb.Append(json.[j]) |> ignore
                        j <- j + 1
                if found then
                    parts.Add(sb.ToString())
                i <- start + 1
        parts |> Seq.filter (fun s -> s.Length > 0) |> String.concat ""

    /// Extract a session id from a JSON response body.
    let extractSessionId (json: string) : string option =
        let key = "\"id\":\""
        let idx = json.IndexOf(key, StringComparison.Ordinal)
        if idx < 0 then None
        else
            let start = idx + key.Length
            let endIdx = json.IndexOf('"', start)
            if endIdx < 0 then None
            else Some (json.[start..endIdx - 1])

    interface IAgentHarness with
        member _.Prompt (prompt: string) (debug: bool) : Result<string, string> =
            try
                // 1. Health check
                let healthTask = client.GetAsync($"{baseUrl}/global/health")
                let healthResp = healthTask.Result
                let healthBody = healthResp.Content.ReadAsStringAsync().Result
                debugPrint debug "health" healthBody

                if not healthResp.IsSuccessStatusCode then
                    Error $"OpenCode server not reachable at {baseUrl}. Try running: opencode serve"
                else

                // 2. Create session
                let sessionName = $"[itr] planning session"
                let sessionJson = sprintf """{"title":"%s"}""" sessionName
                let sessionContent = new StringContent(sessionJson, Encoding.UTF8, "application/json")
                let sessionTask = client.PostAsync($"{baseUrl}/session", sessionContent)
                let sessionResp = sessionTask.Result
                let sessionBody = sessionResp.Content.ReadAsStringAsync().Result
                debugPrint debug "session" sessionBody

                if not sessionResp.IsSuccessStatusCode then
                    Error $"Failed to create OpenCode session: {sessionBody}"
                else

                match extractSessionId sessionBody with
                | None -> Error $"Could not parse session id from response: {sessionBody}"
                | Some sessionId ->

                // 3. Send message
                let msgJson = sprintf """{"parts":[{"type":"text","text":"%s"}]}""" (prompt.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r"))
                let msgContent = new StringContent(msgJson, Encoding.UTF8, "application/json")
                let msgTask = client.PostAsync($"{baseUrl}/session/{sessionId}/message", msgContent)
                let msgResp = msgTask.Result
                let msgBody = msgResp.Content.ReadAsStringAsync().Result
                debugPrint debug "message" msgBody

                if not msgResp.IsSuccessStatusCode then
                    Error $"Failed to send message to OpenCode session: {msgBody}"
                else
                    let content = extractTextContent msgBody
                    Ok content

            with ex ->
                Error $"OpenCode server not reachable at {baseUrl}. Try running: opencode serve ({ex.Message})"
