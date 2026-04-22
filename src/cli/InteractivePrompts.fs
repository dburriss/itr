module Itr.Cli.InteractivePrompts

open System
open Spectre.Console
open Itr.Domain
open Itr.Domain.Backlogs

// ---------------------------------------------------------------------------
// Pre-filled args from explicit CLI arguments
// ---------------------------------------------------------------------------

type PrefilledArgs =
    { BacklogId: string option
      Title: string option
      Repo: string option
      ItemType: string option
      Priority: string option
      Summary: string option
      DependsOn: string list }

// ---------------------------------------------------------------------------
// Prompt helpers (virtual so tests can substitute them)
// ---------------------------------------------------------------------------

/// All side-effectful prompt operations, gathered so tests can stub them.
type PromptFunctions =
    { AskText: string -> string option -> string
      AskOptionalText: string -> string
      AskSelect: string -> string list -> string
      AskMultiSelect: string -> string list -> string list
      AskConfirm: string -> bool
      IsInputRedirected: unit -> bool }

let defaultPromptFunctions: PromptFunctions =
    { AskText =
        fun question defaultValue ->
            let prompt = TextPrompt<string>(question)
            prompt.AllowEmpty <- false
            match defaultValue with
            | Some d -> prompt.DefaultValue(d) |> ignore
            | None -> ()
            AnsiConsole.Prompt(prompt)

      AskOptionalText =
        fun question ->
            let prompt = TextPrompt<string>(question)
            prompt.AllowEmpty <- true
            AnsiConsole.Prompt(prompt)

      AskSelect =
        fun question choices ->
            let prompt = SelectionPrompt<string>()
            prompt.Title <- question
            prompt.AddChoices(choices) |> ignore
            AnsiConsole.Prompt(prompt)

      AskMultiSelect =
        fun question choices ->
            let prompt = MultiSelectionPrompt<string>()
            prompt.Title <- question
            prompt.NotRequired() |> ignore
            prompt.AddChoices(choices) |> ignore
            AnsiConsole.Prompt(prompt) |> Seq.toList

      AskConfirm =
        fun question ->
            AnsiConsole.Confirm(question)

      IsInputRedirected =
        fun () -> Console.IsInputRedirected }

// ---------------------------------------------------------------------------
// Slug validation
// ---------------------------------------------------------------------------

let private isValidSlug (s: string) =
    not (String.IsNullOrWhiteSpace(s)) &&
    not (s.Contains(' ')) &&
    System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-z0-9][a-z0-9-]*$")

// ---------------------------------------------------------------------------
// Main prompt function (internal, with injectable prompt functions for testing)
// ---------------------------------------------------------------------------

let promptBacklogAddWith
    (fns: PromptFunctions)
    (backlogStore: IBacklogStore)
    (coordRoot: string)
    (productConfig: ProductConfig)
    (prefilled: PrefilledArgs)
    : Result<Backlogs.Create.Input, string> =

    // 3.3 Non-TTY detection
    // NOTE: Console.IsInputRedirected can return true in contexts that are still
    // interactive (e.g. Zellij command panes), causing a false-positive exit.
    // The guard below is left here for reference but disabled; Spectre.Console
    // will surface its own error if it genuinely cannot read input.
    //
    // if fns.IsInputRedirected() then
    //     Error "Interactive mode requires a terminal. Please provide required fields as CLI arguments instead."
    // else

    // 3.4 backlog-id prompt
    let backlogId =
        match prefilled.BacklogId with
        | Some id -> id
        | None ->
            let mutable result = ""
            while not (isValidSlug result) do
                result <- fns.AskText "Backlog ID (slug: lowercase letters, numbers, hyphens):" None
                if not (isValidSlug result) then
                    AnsiConsole.MarkupLine("[red]ID must match [a-z0-9][a-z0-9-]* with no spaces.[/]")
            result

    // 3.5 title prompt
    let title =
        match prefilled.Title with
        | Some t -> t
        | None ->
            let mutable result = ""
            while String.IsNullOrWhiteSpace(result) do
                result <- fns.AskText "Title:" None
                if String.IsNullOrWhiteSpace(result) then
                    AnsiConsole.MarkupLine("[red]Title cannot be empty.[/]")
            result

    // 3.6 type prompt
    let itemType =
        match prefilled.ItemType with
        | Some t -> t
        | None ->
            fns.AskSelect "Type:" ["feature"; "bug"; "chore"; "refactor"; "spike"]

    // 3.7 priority prompt
    let priority =
        match prefilled.Priority with
        | Some p -> p
        | None ->
            fns.AskSelect "Priority:" ["low"; "medium"; "high"]

    // 3.8 summary prompt (optional)
    let summary =
        match prefilled.Summary with
        | Some s -> if String.IsNullOrWhiteSpace(s) then None else Some s
        | None ->
            let s = fns.AskOptionalText "Summary (optional, press Enter to skip):"
            if String.IsNullOrWhiteSpace(s) then None else Some s

    // 3.9 repo logic
    let repo =
        match prefilled.Repo with
        | Some r -> r
        | None ->
            let repos = productConfig.Repos |> Map.toList |> List.map (fun (RepoId k, _) -> k)
            match repos with
            | [singleRepo] -> singleRepo
            | _ ->
                fns.AskSelect "Repo:" repos

    // 3.10 dependencies prompt
    let dependencies =
        match prefilled.DependsOn with
        | _ :: _ -> prefilled.DependsOn
        | [] ->
            match backlogStore.ListBacklogItems coordRoot with
            | Error _ -> []
            | Ok items ->
                let itemList = items |> List.map fst
                if itemList.IsEmpty then []
                else
                    let sorted = itemList |> List.sortBy (fun i -> BacklogId.value i.Id) |> List.map (fun i -> BacklogId.value i.Id)
                    fns.AskMultiSelect "Dependencies (space to select, enter to confirm):" sorted

    // 3.11 acceptance criteria loop
    let acceptanceCriteria =
        let mutable criteria = []
        let mutable continueLoop = true
        while continueLoop do
            let entry = fns.AskOptionalText "Acceptance criterion (leave empty to stop):"
            if String.IsNullOrWhiteSpace(entry) then
                continueLoop <- false
            else
                criteria <- criteria @ [entry]
        criteria

    // 3.12 confirmation summary
    let summaryStr = summary |> Option.defaultValue "(none)"
    let depsStr = if dependencies.IsEmpty then "(none)" else String.concat ", " dependencies
    let acStr = if acceptanceCriteria.IsEmpty then "(none)" else acceptanceCriteria |> List.mapi (fun i c -> $"{i+1}. {c}") |> String.concat "; "

    let table = Spectre.Console.Table()
    table.AddColumn("Field") |> ignore
    table.AddColumn("Value") |> ignore
    table.AddRow("backlog-id", backlogId) |> ignore
    table.AddRow("title", title) |> ignore
    table.AddRow("type", itemType) |> ignore
    table.AddRow("priority", priority) |> ignore
    table.AddRow("summary", summaryStr) |> ignore
    table.AddRow("repo", repo) |> ignore
    table.AddRow("dependencies", depsStr) |> ignore
    table.AddRow("acceptance criteria", acStr) |> ignore
    AnsiConsole.Write(table)

    // 3.13 confirmation
    if fns.AskConfirm "Create this backlog item?" then
        Ok
            { BacklogId = backlogId
              Title = title
              Repos = [repo]
              ItemType = Some itemType
              Priority = Some priority
              Summary = summary
              AcceptanceCriteria = acceptanceCriteria
              DependsOn = dependencies }
    else
        Error "Cancelled"

/// Prompt the user for all backlog add fields interactively using the real terminal.
/// Returns Ok CreateBacklogItemInput on confirmation, Error on cancellation or non-TTY.
let promptBacklogAdd
    (backlogStore: IBacklogStore)
    (coordRoot: string)
    (productConfig: ProductConfig)
    (prefilled: PrefilledArgs)
    : Result<Backlogs.Create.Input, string> =
    promptBacklogAddWith defaultPromptFunctions backlogStore coordRoot productConfig prefilled
