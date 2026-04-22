namespace Itr.Adapters

open System
open Spectre.Console
open Itr.Domain
open Itr.Domain.Tasks

// ---------------------------------------------------------------------------
// Input types
// ---------------------------------------------------------------------------

type TaskListSummary =
    { Task: ItrTask
      TaskYamlPath: string
      PlanMdPath: string option }

type TaskDetailView =
    { Task: ItrTask
      Siblings: Query.SiblingTask list
      PlanExists: bool
      PlanApproved: bool
      TaskYamlPath: string
      PlanMdPath: string option }

// ---------------------------------------------------------------------------
// TaskFormatter
// ---------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module TaskFormatter =

    let private taskStateToDisplayString (state: TaskState) : string =
        match state with
        | TaskState.Planning -> "planning"
        | TaskState.Planned -> "planned"
        | TaskState.Approved -> "approved"
        | TaskState.InProgress -> "in_progress"
        | TaskState.Implemented -> "implemented"
        | TaskState.Validated -> "validated"
        | TaskState.Archived -> "archived"

    /// Format and print a list of task summaries.
    let formatList (format: OutputFormat) (ordered: TaskListSummary list) : unit =
        if ordered.IsEmpty then
            match format with
            | Text -> () // no output in text mode for empty results
            | _ -> printfn "No tasks found."
        else
            match format with
            | Json ->
                let items =
                    ordered
                    |> List.map (fun s ->
                        let id = TaskId.value s.Task.Id
                        let repo = RepoId.value s.Task.Repo
                        let state = taskStateToDisplayString s.Task.State
                        let taskYamlPath = s.TaskYamlPath.Replace("\\", "\\\\")
                        let planMdPathJson =
                            match s.PlanMdPath with
                            | Some p -> sprintf "\"%s\"" (p.Replace("\\", "\\\\"))
                            | None -> "null"
                        sprintf "    { \"id\": \"%s\", \"repo\": \"%s\", \"state\": \"%s\", \"taskYamlPath\": \"%s\", \"planMdPath\": %s }"
                            id repo state taskYamlPath planMdPathJson)
                    |> String.concat ",\n"
                printfn "{ \"tasks\": ["
                printfn "%s" items
                printfn "] }"
            | Text ->
                ordered
                |> List.iter (fun s ->
                    let id = TaskId.value s.Task.Id
                    let repo = RepoId.value s.Task.Repo
                    let state = taskStateToDisplayString s.Task.State
                    let taskYamlPath = s.TaskYamlPath
                    let planMdPath = s.PlanMdPath |> Option.defaultValue ""
                    printfn "%s\t%s\t%s\t%s\t%s" id repo state taskYamlPath planMdPath)
            | Table ->
                let table = Table()
                table.AddColumn("Id") |> ignore
                table.AddColumn("Repo") |> ignore
                table.AddColumn("State") |> ignore
                table.AddColumn("Task YAML") |> ignore
                table.AddColumn("Plan MD") |> ignore

                ordered
                |> List.iter (fun s ->
                    let id = TaskId.value s.Task.Id
                    let repo = RepoId.value s.Task.Repo
                    let state = taskStateToDisplayString s.Task.State
                    let taskYamlPath = s.TaskYamlPath
                    let planMdPath = s.PlanMdPath |> Option.defaultValue ""
                    table.AddRow(id, repo, state, taskYamlPath, planMdPath) |> ignore)

                AnsiConsole.Write(table)

    /// Format and print a single task detail.
    let formatDetail (format: OutputFormat) (detail: TaskDetailView) : unit =
        let task = detail.Task
        let id = TaskId.value task.Id
        let backlog = BacklogId.value task.SourceBacklog
        let repo = RepoId.value task.Repo
        let state = taskStateToDisplayString task.State
        let createdAt = task.CreatedAt.ToString("yyyy-MM-dd")
        let planExistsStr = if detail.PlanExists then "yes" else "no"
        let planApprovedStr = if detail.PlanApproved then "yes" else "no"
        let planMdPathStr = detail.PlanMdPath |> Option.defaultValue ""

        match format with
        | Json ->
            let siblingsJson =
                detail.Siblings
                |> List.map (fun s ->
                    sprintf "    { \"id\": \"%s\", \"repo\": \"%s\", \"state\": \"%s\" }"
                        (TaskId.value s.Id)
                        (RepoId.value s.Repo)
                        (taskStateToDisplayString s.State))
                |> String.concat ",\n"
            let planMdPathJson =
                match detail.PlanMdPath with
                | Some p -> sprintf "\"%s\"" (p.Replace("\\", "\\\\"))
                | None -> "null"
            printfn "{"
            printfn "  \"id\": \"%s\"," id
            printfn "  \"backlog\": \"%s\"," backlog
            printfn "  \"repo\": \"%s\"," repo
            printfn "  \"state\": \"%s\"," state
            printfn "  \"planExists\": %b," detail.PlanExists
            printfn "  \"planApproved\": %b," detail.PlanApproved
            printfn "  \"createdAt\": \"%s\"," createdAt
            printfn "  \"taskYamlPath\": \"%s\"," (detail.TaskYamlPath.Replace("\\", "\\\\"))
            printfn "  \"planMdPath\": %s," planMdPathJson
            printfn "  \"siblings\": ["
            if not (List.isEmpty detail.Siblings) then printfn "%s" siblingsJson
            printfn "  ]"
            printfn "}"
        | Text ->
            let siblingsStr =
                if detail.Siblings.IsEmpty then "-"
                else detail.Siblings |> List.map (fun s -> TaskId.value s.Id) |> String.concat ","
            printfn "id\t%s" id
            printfn "backlog\t%s" backlog
            printfn "repo\t%s" repo
            printfn "state\t%s" state
            printfn "plan exists\t%s" planExistsStr
            printfn "plan approved\t%s" planApprovedStr
            printfn "created\t%s" createdAt
            printfn "siblings\t%s" siblingsStr
            printfn "taskYamlPath\t%s" detail.TaskYamlPath
            printfn "planMdPath\t%s" planMdPathStr
        | Table ->
            let infoTable = Table()
            infoTable.AddColumn("Field") |> ignore
            infoTable.AddColumn("Value") |> ignore
            infoTable.AddRow("id", id) |> ignore
            infoTable.AddRow("backlog", backlog) |> ignore
            infoTable.AddRow("repo", repo) |> ignore
            infoTable.AddRow("state", state) |> ignore
            infoTable.AddRow("plan exists", planExistsStr) |> ignore
            infoTable.AddRow("plan approved", planApprovedStr) |> ignore
            infoTable.AddRow("created", createdAt) |> ignore
            infoTable.AddRow("Task YAML", detail.TaskYamlPath) |> ignore
            infoTable.AddRow("Plan MD", planMdPathStr) |> ignore
            AnsiConsole.Write(infoTable)

            if detail.Siblings.IsEmpty then
                printfn "siblings: (none)"
            else
                let sibTable = Table()
                sibTable.AddColumn("Id") |> ignore
                sibTable.AddColumn("Repo") |> ignore
                sibTable.AddColumn("State") |> ignore
                detail.Siblings
                |> List.iter (fun s ->
                    sibTable.AddRow(
                        TaskId.value s.Id,
                        RepoId.value s.Repo,
                        taskStateToDisplayString s.State) |> ignore)
                AnsiConsole.Write(sibTable)
