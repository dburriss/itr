namespace Itr.Adapters

open System
open Spectre.Console
open Itr.Domain

[<RequireQualifiedAccess>]
module BacklogFormatter =

    let private backlogItemStatusToString (status: BacklogItemStatus) : string =
        match status with
        | BacklogItemStatus.Created -> "created"
        | BacklogItemStatus.Planning -> "planning"
        | BacklogItemStatus.Planned -> "planned"
        | BacklogItemStatus.Approved -> "approved"
        | BacklogItemStatus.InProgress -> "in-progress"
        | BacklogItemStatus.Completed -> "completed"
        | BacklogItemStatus.Archived -> "archived"

    /// Format and print a backlog item list.
    let formatList (format: OutputFormat) (items: BacklogItemSummary list) : unit =
        match format with
        | Json ->
            let jsonItems =
                items
                |> List.map (fun s ->
                    let id = BacklogId.value s.Item.Id
                    let itemType = BacklogItemType.toString s.Item.Type
                    let priority = s.Item.Priority |> Option.defaultValue ""
                    let status = backlogItemStatusToString s.Status
                    let viewId = s.ViewId |> Option.defaultValue ""
                    let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")

                    sprintf
                        """  { "id": "%s", "type": "%s", "priority": "%s", "status": "%s", "view": "%s", "taskCount": %d, "createdAt": "%s", "path": "%s" }"""
                        id
                        itemType
                        priority
                        status
                        viewId
                        s.TaskCount
                        createdAt
                        (s.Path.Replace("\\", "\\\\")))
                |> String.concat ",\n"

            printfn "["

            if not (List.isEmpty items) then
                printfn "%s" jsonItems

            printfn "]"
        | Text ->
            items
            |> List.iter (fun s ->
                let id = BacklogId.value s.Item.Id
                let itemType = BacklogItemType.toString s.Item.Type
                let priority = s.Item.Priority |> Option.defaultValue "-"
                let status = backlogItemStatusToString s.Status
                let viewId = s.ViewId |> Option.defaultValue "-"
                let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")
                let title = s.Item.Title

                printfn
                    "%s\t%s\t%s\t%s\t%s\t%d\t%s\t%s\t%s"
                    id
                    itemType
                    priority
                    status
                    viewId
                    s.TaskCount
                    createdAt
                    title
                    s.Path)
        | Table ->
            let table = Table()
            table.AddColumn("ID") |> ignore
            table.AddColumn("Type") |> ignore
            table.AddColumn("Priority") |> ignore
            table.AddColumn("Status") |> ignore
            table.AddColumn("View") |> ignore
            table.AddColumn("Tasks") |> ignore
            table.AddColumn("Created") |> ignore
            table.AddColumn("Path") |> ignore

            items
            |> List.iter (fun s ->
                let id = BacklogId.value s.Item.Id
                let itemType = BacklogItemType.toString s.Item.Type
                let priority = s.Item.Priority |> Option.defaultValue "-"
                let status = backlogItemStatusToString s.Status
                let viewId = s.ViewId |> Option.defaultValue "-"
                let createdAt = s.Item.CreatedAt.ToString("yyyy-MM-dd")

                table.AddRow(id, itemType, priority, status, viewId, string s.TaskCount, createdAt, s.Path)
                |> ignore)

            AnsiConsole.Write(table)

    /// Format and print a single backlog item detail.
    let formatDetail (format: OutputFormat) (detail: BacklogItemDetail) : unit =
        let item = detail.Item
        let id = BacklogId.value item.Id
        let itemType = BacklogItemType.toString item.Type
        let priority = item.Priority |> Option.defaultValue ""
        let status = backlogItemStatusToString detail.Status
        let viewId = detail.ViewId |> Option.defaultValue ""
        let createdAt = item.CreatedAt.ToString("yyyy-MM-dd")
        let summary = item.Summary |> Option.defaultValue ""
        let ac = item.AcceptanceCriteria
        let deps_ = item.Dependencies |> List.map BacklogId.value
        let repos = item.Repos |> List.map RepoId.value

        match format with
        | Json ->
            let acJson =
                ac
                |> List.map (fun s -> sprintf "    \"%s\"" (s.Replace("\"", "\\\"")))
                |> String.concat ",\n"

            let depsJson =
                deps_ |> List.map (fun s -> sprintf "    \"%s\"" s) |> String.concat ",\n"

            let reposJson =
                repos |> List.map (fun s -> sprintf "    \"%s\"" s) |> String.concat ",\n"

            let tasksJson =
                detail.Tasks
                |> List.map (fun t ->
                    let tid = TaskId.value t.Id
                    let repo = RepoId.value t.Repo

                    let state =
                        match t.State with
                        | TaskState.Planning -> "planning"
                        | TaskState.Planned -> "planned"
                        | TaskState.Approved -> "approved"
                        | TaskState.InProgress -> "in-progress"
                        | TaskState.Implemented -> "implemented"
                        | TaskState.Validated -> "validated"
                        | TaskState.Archived -> "archived"

                    sprintf "    { \"id\": \"%s\", \"repo\": \"%s\", \"state\": \"%s\" }" tid repo state)
                |> String.concat ",\n"

            printfn "{"
            printfn "  \"id\": \"%s\"," id
            printfn "  \"title\": \"%s\"," (item.Title.Replace("\"", "\\\""))
            printfn "  \"type\": \"%s\"," itemType
            printfn "  \"priority\": \"%s\"," priority
            printfn "  \"status\": \"%s\"," status
            printfn "  \"summary\": \"%s\"," (summary.Replace("\"", "\\\""))
            printfn "  \"acceptanceCriteria\": ["

            if not (List.isEmpty ac) then
                printfn "%s" acJson

            printfn "  ],"
            printfn "  \"dependencies\": ["

            if not (List.isEmpty deps_) then
                printfn "%s" depsJson

            printfn "  ],"
            printfn "  \"repos\": ["

            if not (List.isEmpty repos) then
                printfn "%s" reposJson

            printfn "  ],"
            printfn "  \"createdAt\": \"%s\"," createdAt
            printfn "  \"tasks\": ["

            if not (List.isEmpty detail.Tasks) then
                printfn "%s" tasksJson

            printfn "  ],"
            printfn "  \"path\": \"%s\"" (detail.Path.Replace("\\", "\\\\"))
            printfn "}"
        | Text ->
            let priorityStr = item.Priority |> Option.defaultValue "-"
            let viewStr = detail.ViewId |> Option.defaultValue "-"
            let reposStr = if repos.IsEmpty then "-" else String.concat "," repos
            let depsStr = if deps_.IsEmpty then "-" else String.concat "," deps_
            let summaryStr = summary.Replace('\n', ' ').Replace('\r', ' ')

            let acStr =
                if ac.IsEmpty then
                    "-"
                else
                    ac
                    |> List.map (fun s -> s.Replace('\n', ' ').Replace('\r', ' '))
                    |> String.concat ","

            let tasksStr =
                if detail.Tasks.IsEmpty then
                    "-"
                else
                    detail.Tasks |> List.map (fun t -> TaskId.value t.Id) |> String.concat ","

            printfn "id\t%s" id
            printfn "title\t%s" item.Title
            printfn "type\t%s" itemType
            printfn "priority\t%s" priorityStr
            printfn "status\t%s" status
            printfn "view\t%s" viewStr
            printfn "summary\t%s" summaryStr
            printfn "acceptance criteria\t%s" acStr
            printfn "dependencies\t%s" depsStr
            printfn "repos\t%s" reposStr
            printfn "created\t%s" createdAt
            printfn "taskCount\t%d" detail.Tasks.Length
            printfn "tasks\t%s" tasksStr
            printfn "path\t%s" detail.Path
        | Table ->
            let infoTable = Table()
            infoTable.AddColumn("Field") |> ignore
            infoTable.AddColumn("Value") |> ignore
            infoTable.AddRow("id", id) |> ignore
            infoTable.AddRow("title", item.Title) |> ignore
            infoTable.AddRow("type", itemType) |> ignore
            infoTable.AddRow("priority", priority) |> ignore
            infoTable.AddRow("status", status) |> ignore
            infoTable.AddRow("view", viewId) |> ignore
            infoTable.AddRow("summary", summary) |> ignore
            infoTable.AddRow("acceptance criteria", String.concat "\n" ac) |> ignore
            infoTable.AddRow("dependencies", String.concat ", " deps_) |> ignore
            infoTable.AddRow("repos", String.concat ", " repos) |> ignore
            infoTable.AddRow("created", createdAt) |> ignore
            infoTable.AddRow("path", detail.Path) |> ignore
            AnsiConsole.Write(infoTable)

            let tasksTable = Table()
            tasksTable.AddColumn("Task ID") |> ignore
            tasksTable.AddColumn("Repo") |> ignore
            tasksTable.AddColumn("State") |> ignore

            detail.Tasks
            |> List.iter (fun t ->
                let tid = TaskId.value t.Id
                let repo = RepoId.value t.Repo

                let state =
                    match t.State with
                    | TaskState.Planning -> "planning"
                    | TaskState.Planned -> "planned"
                    | TaskState.Approved -> "approved"
                    | TaskState.InProgress -> "in-progress"
                    | TaskState.Implemented -> "implemented"
                    | TaskState.Validated -> "validated"
                    | TaskState.Archived -> "archived"

                tasksTable.AddRow(tid, repo, state) |> ignore)

            AnsiConsole.Write(tasksTable)
