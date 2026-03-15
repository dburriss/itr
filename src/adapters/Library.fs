namespace Itr.Adapters

open System
open System.IO
open Itr.Domain

/// Real filesystem adapter implementing IFileSystem
type FileSystemAdapter() =
    interface IFileSystem with
        member _.ReadFile path =
            try
                if File.Exists(path) then
                    Ok(File.ReadAllText(path))
                else
                    Error(FileNotFound path)
            with ex ->
                Error(IoException(path, ex.Message))

        member _.WriteFile path content =
            try
                let dir = Path.GetDirectoryName(path)

                if not (String.IsNullOrEmpty(dir)) && not (Directory.Exists(dir)) then
                    Directory.CreateDirectory(dir) |> ignore

                File.WriteAllText(path, content)
                Ok()
            with ex ->
                Error(IoException(path, ex.Message))

        member _.FileExists path = File.Exists(path)

        member _.DirectoryExists path = Directory.Exists(path)

/// Real environment adapter implementing IEnvironment
type EnvironmentAdapter() =
    interface IEnvironment with
        member _.GetEnvVar name =
            match Environment.GetEnvironmentVariable(name) with
            | null -> None
            | value when String.IsNullOrWhiteSpace(value) -> None
            | value -> Some value

        member _.HomeDirectory() =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
