module Itr.Tests.Acceptance.BacklogAddInteractiveTests

open System
open System.IO
open Xunit
open Itr.Domain
open Itr.Adapters.YamlAdapter
open Itr.Features
open Itr.Cli.InteractivePrompts

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private mkSingleRepoProductConfig () =
    let productId =
        match ProductId.tryCreate "test-product" with
        | Ok id -> id
        | Error e -> failwithf "%A" e

    let repos =
        [ RepoId "main-repo", ({ Path = "./"; Url = None }: RepoConfig) ]
        |> Map.ofList

    { Id = productId; Repos = repos }

let private mkMultiRepoProductConfig () =
    let productId =
        match ProductId.tryCreate "test-product" with
        | Ok id -> id
        | Error e -> failwithf "%A" e

    let repos =
        [ RepoId "main-repo", ({ Path = "./"; Url = None }: RepoConfig)
          RepoId "second-repo", ({ Path = "../second"; Url = None }: RepoConfig) ]
        |> Map.ofList

    { Id = productId; Repos = repos }

/// Build a stub PromptFunctions that returns the provided values in sequence
let private stubFns
    (backlogId: string)
    (title: string)
    (itemType: string)
    (priority: string)
    (summary: string)
    (repo: string)
    (multiSelectResult: string list)
    (acEntries: string list)
    (confirm: bool)
    : PromptFunctions =

    let selectAnswers = System.Collections.Generic.Queue<string>([itemType; priority; repo])
    let textAnswers = System.Collections.Generic.Queue<string>([backlogId; title])
    let optionalAnswers = System.Collections.Generic.Queue<string>(acEntries @ [""])

    { AskText = fun _q _d -> textAnswers.Dequeue()
      AskOptionalText = fun _q ->
            if optionalAnswers.Count > 0 then optionalAnswers.Dequeue()
            else ""
      AskSelect = fun _q _choices -> selectAnswers.Dequeue()
      AskMultiSelect = fun _q _choices -> multiSelectResult
      AskConfirm = fun _q -> confirm
      IsInputRedirected = fun () -> false }

let private emptyBacklogStore () =
    let root = Path.Combine(Path.GetTempPath(), $"itr-interactive-{Guid.NewGuid():N}")
    let coordRoot = Path.Combine(root, ".itr")
    Directory.CreateDirectory(Path.Combine(coordRoot, "BACKLOG")) |> ignore
    let store = BacklogStoreAdapter() :> IBacklogStore
    (store, coordRoot, root)

// ---------------------------------------------------------------------------
// 5.1 Argument-merging: explicit args skip their prompts
// ---------------------------------------------------------------------------

[<Fact>]
let ``5.1 explicit title skips title prompt and uses provided value`` () =
    let (store, coordRoot, root) = emptyBacklogStore()
    try
        let productConfig = mkSingleRepoProductConfig()

        // All select prompts: type, priority (repo auto-filled for single-repo)
        let selectAnswers = System.Collections.Generic.Queue<string>(["feature"; "low"])
        let textAnswers = System.Collections.Generic.Queue<string>(["my-feature"])   // only id prompt
        let optionalAnswers = System.Collections.Generic.Queue<string>([""])          // AC: empty -> stop

        let fns: PromptFunctions =
            { AskText = fun _q _d -> textAnswers.Dequeue()
              AskOptionalText = fun _q ->
                    if optionalAnswers.Count > 0 then optionalAnswers.Dequeue() else ""
              AskSelect = fun _q _choices -> selectAnswers.Dequeue()
              AskMultiSelect = fun _q _choices -> []
              AskConfirm = fun _q -> true
              IsInputRedirected = fun () -> false }

        let prefilled: PrefilledArgs =
            { BacklogId = None
              Title = Some "Pre-filled Title"
              Repo = None
              ItemType = None
              Priority = None
              Summary = None
              DependsOn = [] }

        match promptBacklogAddWith fns store coordRoot productConfig prefilled with
        | Error msg -> failwithf "expected Ok, got Error: %s" msg
        | Ok input ->
            Assert.Equal("Pre-filled Title", input.Title)
            // id came from text prompt
            Assert.Equal("my-feature", input.BacklogId)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``5.1 explicit backlog-id skips id prompt and uses provided value`` () =
    let (store, coordRoot, root) = emptyBacklogStore()
    try
        let productConfig = mkSingleRepoProductConfig()

        let selectAnswers = System.Collections.Generic.Queue<string>(["feature"; "low"])
        let textAnswers = System.Collections.Generic.Queue<string>(["Prompted Title"])   // only title prompt
        let optionalAnswers = System.Collections.Generic.Queue<string>([""])

        let fns: PromptFunctions =
            { AskText = fun _q _d -> textAnswers.Dequeue()
              AskOptionalText = fun _q ->
                    if optionalAnswers.Count > 0 then optionalAnswers.Dequeue() else ""
              AskSelect = fun _q _choices -> selectAnswers.Dequeue()
              AskMultiSelect = fun _q _choices -> []
              AskConfirm = fun _q -> true
              IsInputRedirected = fun () -> false }

        let prefilled: PrefilledArgs =
            { BacklogId = Some "prefilled-id"
              Title = None
              Repo = None
              ItemType = None
              Priority = None
              Summary = None
              DependsOn = [] }

        match promptBacklogAddWith fns store coordRoot productConfig prefilled with
        | Error msg -> failwithf "expected Ok, got Error: %s" msg
        | Ok input ->
            Assert.Equal("prefilled-id", input.BacklogId)
            Assert.Equal("Prompted Title", input.Title)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

[<Fact>]
let ``5.1 explicit depends-on skips dependencies prompt`` () =
    let (store, coordRoot, root) = emptyBacklogStore()
    try
        let productConfig = mkSingleRepoProductConfig()

        let selectAnswers = System.Collections.Generic.Queue<string>(["feature"; "low"])
        let textAnswers = System.Collections.Generic.Queue<string>(["my-id"; "My Title"])
        let optionalAnswers = System.Collections.Generic.Queue<string>([""])

        let fns: PromptFunctions =
            { AskText = fun _q _d -> textAnswers.Dequeue()
              AskOptionalText = fun _q ->
                    if optionalAnswers.Count > 0 then optionalAnswers.Dequeue() else ""
              AskSelect = fun _q _choices -> selectAnswers.Dequeue()
              AskMultiSelect = fun _q _choices -> failwith "multi-select should not be called"
              AskConfirm = fun _q -> true
              IsInputRedirected = fun () -> false }

        let prefilled: PrefilledArgs =
            { BacklogId = None
              Title = None
              Repo = None
              ItemType = None
              Priority = None
              Summary = None
              DependsOn = ["dep-1"; "dep-2"] }

        match promptBacklogAddWith fns store coordRoot productConfig prefilled with
        | Error msg -> failwithf "expected Ok, got Error: %s" msg
        | Ok input ->
            Assert.Equal<string list>(["dep-1"; "dep-2"], input.DependsOn)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 5.2 Non-TTY guard returns error
// ---------------------------------------------------------------------------

[<Fact>]
let ``5.2 non-TTY returns error with helpful message`` () =
    let (store, coordRoot, root) = emptyBacklogStore()
    try
        let productConfig = mkSingleRepoProductConfig()

        let fns: PromptFunctions =
            { AskText = fun _q _d -> failwith "should not be called"
              AskOptionalText = fun _q -> failwith "should not be called"
              AskSelect = fun _q _choices -> failwith "should not be called"
              AskMultiSelect = fun _q _choices -> failwith "should not be called"
              AskConfirm = fun _q -> failwith "should not be called"
              IsInputRedirected = fun () -> true }  // simulate non-TTY

        let prefilled: PrefilledArgs =
            { BacklogId = None; Title = None; Repo = None
              ItemType = None; Priority = None; Summary = None; DependsOn = [] }

        match promptBacklogAddWith fns store coordRoot productConfig prefilled with
        | Ok _ -> failwith "expected Error for non-TTY"
        | Error msg ->
            Assert.Contains("terminal", msg)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)

// ---------------------------------------------------------------------------
// 5.3 Missing Backlog_Id without --interactive returns error
// 5.4 Missing Title without --interactive returns error
//
// These validate the guard logic in handleBacklogAdd at the use-case level:
// we exercise Backlog.createBacklogItem which enforces both validations.
// ---------------------------------------------------------------------------

[<Fact>]
let ``5.3 createBacklogItem with empty backlog-id returns error`` () =
    let productConfig = mkSingleRepoProductConfig()

    let input: Backlog.CreateBacklogItemInput =
        { BacklogId = ""
          Title = "Some Title"
          Repos = ["main-repo"]
          ItemType = None
          Priority = None
          Summary = None
          AcceptanceCriteria = []
          DependsOn = [] }

    let today = DateOnly(2026, 3, 22)

    match Backlog.createBacklogItem productConfig input today with
    | Ok _ -> failwith "expected error for empty backlog id"
    | Error e ->
        // BacklogId.tryCreate rejects empty string
        let msg = sprintf "%A" e
        Assert.False(String.IsNullOrEmpty(msg))

[<Fact>]
let ``5.4 createBacklogItem with empty title returns MissingTitle error`` () =
    let productConfig = mkSingleRepoProductConfig()

    let input: Backlog.CreateBacklogItemInput =
        { BacklogId = "some-id"
          Title = ""
          Repos = ["main-repo"]
          ItemType = None
          Priority = None
          Summary = None
          AcceptanceCriteria = []
          DependsOn = [] }

    let today = DateOnly(2026, 3, 22)

    match Backlog.createBacklogItem productConfig input today with
    | Ok _ -> failwith "expected MissingTitle error"
    | Error e -> Assert.Equal(MissingTitle, e)

// ---------------------------------------------------------------------------
// 5.2 Confirmation rejected returns Cancelled
// ---------------------------------------------------------------------------

[<Fact>]
let ``cancelled confirmation returns Error Cancelled`` () =
    let (store, coordRoot, root) = emptyBacklogStore()
    try
        let productConfig = mkSingleRepoProductConfig()

        let selectAnswers = System.Collections.Generic.Queue<string>(["feature"; "low"])
        let textAnswers = System.Collections.Generic.Queue<string>(["my-id"; "My Title"])
        let optionalAnswers = System.Collections.Generic.Queue<string>([""])

        let fns: PromptFunctions =
            { AskText = fun _q _d -> textAnswers.Dequeue()
              AskOptionalText = fun _q ->
                    if optionalAnswers.Count > 0 then optionalAnswers.Dequeue() else ""
              AskSelect = fun _q _choices -> selectAnswers.Dequeue()
              AskMultiSelect = fun _q _choices -> []
              AskConfirm = fun _q -> false   // reject
              IsInputRedirected = fun () -> false }

        let prefilled: PrefilledArgs =
            { BacklogId = None; Title = None; Repo = None
              ItemType = None; Priority = None; Summary = None; DependsOn = [] }

        match promptBacklogAddWith fns store coordRoot productConfig prefilled with
        | Ok _ -> failwith "expected Error on cancellation"
        | Error msg -> Assert.Equal("Cancelled", msg)
    finally
        if Directory.Exists(root) then Directory.Delete(root, true)
