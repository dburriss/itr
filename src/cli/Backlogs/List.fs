module Itr.Cli.Backlogs.List

open Argu
open Itr.Domain
open Itr.Domain.Backlogs
open Itr.Adapters
open Itr.Cli.CliArgs
open Itr.Cli.CliParsers
open Itr.Cli.ErrorFormatting

let handle
    (deps: #IBacklogStore & #ITaskStore & #IViewStore)
    (resolved: ResolvedProduct)
    (listArgs: ParseResults<ListArgs>)
    : Result<unit, string> =
    let coordRoot = resolved.CoordRoot.AbsolutePath
    let backlogStore = deps :> IBacklogStore
    let taskStore = deps :> ITaskStore
    let viewStore = deps :> IViewStore

    let viewFilter = listArgs.TryGetResult ListArgs.View
    let format = listArgs.TryGetResult ListArgs.Output |> OutputFormat.tryParse

    let statusFilter =
        listArgs.TryGetResult ListArgs.Status
        |> Option.bind tryParseBacklogItemStatus

    let typeFilter =
        listArgs.TryGetResult ListArgs.Type
        |> Option.bind (fun t ->
            match BacklogItemType.tryParse t with
            | Ok bt -> Some bt
            | Error _ -> None)

    let excludeStatuses =
        listArgs.GetResults ListArgs.Exclude
        |> List.choose tryParseBacklogItemStatus

    let orderBy = listArgs.TryGetResult ListArgs.Order_By

    let filter: Backlogs.Query.BacklogListFilter =
        { ViewId = viewFilter
          Status = statusFilter
          ItemType = typeFilter
          ExcludeStatuses = excludeStatuses
          OrderBy = orderBy }

    match Backlogs.Query.loadSnapshot backlogStore taskStore viewStore coordRoot with
    | Error e -> Error(formatBacklogError e)
    | Ok snapshot ->
        let items = Backlogs.Query.list filter snapshot
        BacklogFormatter.formatList format items
        Ok()
