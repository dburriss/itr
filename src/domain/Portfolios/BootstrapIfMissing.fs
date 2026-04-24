module Itr.Domain.Portfolios.BootstrapIfMissing

open Itr.Domain

/// Default content written to itr.json on first run
let private defaultConfigContent = """{"defaultProfile": null, "profiles": {}}"""

/// Check if config file exists; if absent, write default itr.json.
/// Returns Ok true if file was created, Ok false if it already existed,
/// or BootstrapWriteError if the write fails.
let execute<'deps when 'deps :> IFileSystem> (configPath: string) : EffectResult<'deps, bool, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let fs = deps :> IFileSystem

        if fs.FileExists configPath then
            Ok false
        else
            fs.WriteFile configPath defaultConfigContent
            |> Result.map (fun () -> true)
            |> Result.mapError (fun ioErr ->
                let msg =
                    match ioErr with
                    | IoException(_, m) -> m
                    | FileNotFound p -> $"File not found: {p}"
                    | DirectoryNotFound p -> $"Directory not found: {p}"

                BootstrapWriteError(configPath, msg)))
