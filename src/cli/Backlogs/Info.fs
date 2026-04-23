module Itr.Cli.Backlogs.Info

open Argu
open Itr.Domain
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.ErrorFormatting

let handle
    (deps: #IBacklogStore & #ITaskStore & #IViewStore)
    (resolved: ResolvedProduct)
    (infoArgs: ParseResults<InfoArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let rawBacklogId = infoArgs.GetResult InfoArgs.Backlog_Id
    let format = infoArgs.TryGetResult InfoArgs.Output |> OutputFormat.tryParse

    match BacklogId.tryCreate rawBacklogId with
    | Error _ -> Error $"Invalid backlog id '{rawBacklogId}': must match [a-z0-9][a-z0-9-]*"
    | Ok backlogId ->
        let backlogStore = deps :> IBacklogStore
        let taskStore = deps :> ITaskStore
        let viewStore = deps :> IViewStore

        match Backlogs.Query.getDetail backlogStore taskStore viewStore coordRoot backlogId with
        | Error e -> Error(formatBacklogError e)
        | Ok detail ->
            BacklogFormatter.formatDetail format detail
            Ok()
