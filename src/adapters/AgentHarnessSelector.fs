namespace Itr.Adapters

open Itr.Domain

[<RequireQualifiedAccess>]
module AgentHarnessSelector =

    /// Select the appropriate IAgentHarness implementation based on the protocol string.
    /// Raises an InvalidOperationException for unrecognised protocols.
    let selectHarness (protocol: string) (command: string) (args: string list) (coordRoot: string) : IAgentHarness =
        match protocol with
        | "acp" ->
            AcpHarnessAdapter(command, args, coordRoot) :> IAgentHarness
        | "opencode-http" | _ ->
            OpenCodeHarnessAdapter() :> IAgentHarness

    /// Select harness, returning Error for unknown protocols instead of defaulting.
    let trySelectHarness (protocol: string) (command: string) (args: string list) (coordRoot: string) : Result<IAgentHarness, string> =
        match protocol with
        | "acp" ->
            Ok(AcpHarnessAdapter(command, args, coordRoot) :> IAgentHarness)
        | "opencode-http" ->
            Ok(OpenCodeHarnessAdapter() :> IAgentHarness)
        | other ->
            Error $"Unknown agent protocol '{other}': must be 'acp' or 'opencode-http'"
