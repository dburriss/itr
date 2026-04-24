module Itr.Domain.Portfolios.InitProduct

open Itr.Domain

type Input =
    { Id: string
      Path: string
      RepoId: string
      CoordPath: string
      CoordinationMode: string
      RegisterProfile: string option }

/// Scaffold a new product on disk: writes product.yaml, PRODUCT.md, ARCHITECTURE.md,
/// and creates the coordination directory sentinel. Optionally registers the product.
let execute<'deps when 'deps :> IFileSystem and 'deps :> IPortfolioConfig and 'deps :> IProductConfig>
    (configPath: string)
    (input: Input)
    : EffectResult<'deps, Portfolio option, PortfolioError> =
    Effect(fun (deps: 'deps) ->
        let fs = deps :> IFileSystem

        match ProductId.tryCreate input.Id with
        | Error e -> Error e
        | Ok _ ->

            let absPath = System.IO.Path.GetFullPath(input.Path)

            if not (fs.DirectoryExists absPath) then
                Error(ProductConfigError(absPath, $"Directory does not exist: {absPath}"))
            else

                let productYamlPath = System.IO.Path.Combine(absPath, "product.yaml")
                let productMdPath = System.IO.Path.Combine(absPath, "PRODUCT.md")
                let archMdPath = System.IO.Path.Combine(absPath, "ARCHITECTURE.md")

                if fs.FileExists productYamlPath then
                    Error(ProductConfigError(absPath, $"product.yaml already exists at: {productYamlPath}"))
                elif fs.FileExists productMdPath then
                    Error(ProductConfigError(absPath, $"PRODUCT.md already exists at: {productMdPath}"))
                elif fs.FileExists archMdPath then
                    Error(ProductConfigError(absPath, $"ARCHITECTURE.md already exists at: {archMdPath}"))
                else

                    let productYamlContent =
                        let baseYaml =
                            $"""id: {input.Id}
docs:
  product: PRODUCT.md
  architecture: ARCHITECTURE.md
repos:
  {input.RepoId}:
    path: .
coordination:
  mode: {input.CoordinationMode}
"""

                        if input.CoordinationMode = "standalone" then
                            baseYaml + $"  path: {input.CoordPath}\n"
                        else
                            baseYaml + $"  repo: {input.RepoId}\n  path: {input.CoordPath}\n"

                    let productMdContent =
                        $"""# Product: {input.Id}

## Purpose

<!-- TODO: Describe the purpose of this product -->
"""

                    let archMdContent =
                        $"""# Architecture: {input.Id}

## Technology Stack

<!-- TODO: Describe the technology stack -->
"""

                    match fs.WriteFile productYamlPath productYamlContent with
                    | Error ioErr ->
                        let msg =
                            match ioErr with
                            | IoException(_, m) -> m
                            | FileNotFound p -> $"File not found: {p}"
                            | DirectoryNotFound p -> $"Directory not found: {p}"

                        Error(ProductConfigError(absPath, msg))
                    | Ok() ->

                        let coordPath = System.IO.Path.Combine(absPath, input.CoordPath, ".gitkeep")

                        match fs.WriteFile coordPath "" with
                        | Error ioErr ->
                            let msg =
                                match ioErr with
                                | IoException(_, m) -> m
                                | FileNotFound p -> $"File not found: {p}"
                                | DirectoryNotFound p -> $"Directory not found: {p}"

                            Error(ProductConfigError(absPath, msg))
                        | Ok() ->

                            match fs.WriteFile productMdPath productMdContent with
                            | Error ioErr ->
                                let msg =
                                    match ioErr with
                                    | IoException(_, m) -> m
                                    | FileNotFound p -> $"File not found: {p}"
                                    | DirectoryNotFound p -> $"Directory not found: {p}"

                                Error(ProductConfigError(absPath, msg))
                            | Ok() ->

                                match fs.WriteFile archMdPath archMdContent with
                                | Error ioErr ->
                                    let msg =
                                        match ioErr with
                                        | IoException(_, m) -> m
                                        | FileNotFound p -> $"File not found: {p}"
                                        | DirectoryNotFound p -> $"Directory not found: {p}"

                                    Error(ProductConfigError(absPath, msg))
                                | Ok() ->

                                    match input.RegisterProfile with
                                    | None -> Ok None
                                    | Some profile ->
                                        let regInput: RegisterProduct.Input =
                                            { Path = absPath
                                              Profile = Some profile }

                                        let portfolioConfig = deps :> IPortfolioConfig

                                        RegisterProduct.execute configPath regInput
                                        |> Effect.run deps
                                        |> Result.bind (fun updatedPortfolio ->
                                            portfolioConfig.SaveConfig configPath updatedPortfolio
                                            |> Result.map (fun () -> Some updatedPortfolio)))
