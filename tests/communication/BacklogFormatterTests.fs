module Itr.Tests.Communication.BacklogFormatterTests

open System
open Xunit
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Adapters

let private makeItem id title =
    { Id = BacklogId id
      Title = title
      Repos = [ RepoId "repo1" ]
      Type = BacklogItemType.Feature
      Priority = Some "high"
      Summary = Some "A test summary"
      AcceptanceCriteria = [ "must work" ]
      Dependencies = []
      CreatedAt = DateOnly(2024, 1, 15) }

// ---------------------------------------------------------------------------
// BacklogFormatter.formatList tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``formatList Text outputs id and status`` () =
    let summary : BacklogItemSummary =
        { Item = makeItem "my-feature" "My Feature"
          Status = BacklogItemStatus.Created
          ViewId = None
          TaskCount = 0
          Path = "/some/path/item.yaml" }
    let output = TestHelpers.captureOutput (fun () -> BacklogFormatter.formatList Text [ summary ])
    Assert.Contains("my-feature", output)
    Assert.Contains("created", output)

[<Fact>]
let ``formatList Json produces array output`` () =
    let summary : BacklogItemSummary =
        { Item = makeItem "my-feature" "My Feature"
          Status = BacklogItemStatus.Created
          ViewId = None
          TaskCount = 2
          Path = "/some/path/item.yaml" }
    let output = TestHelpers.captureOutput (fun () -> BacklogFormatter.formatList Json [ summary ])
    Assert.Contains("[", output)
    Assert.Contains("my-feature", output)
    Assert.Contains("feature", output)

[<Fact>]
let ``formatList Json empty produces brackets`` () =
    let output = TestHelpers.captureOutput (fun () -> BacklogFormatter.formatList Json [])
    Assert.Contains("[", output)
    Assert.Contains("]", output)

// ---------------------------------------------------------------------------
// BacklogFormatter.formatDetail tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``formatDetail Text outputs key fields`` () =
    let detail : BacklogItemDetail =
        { Item = makeItem "my-feature" "My Feature"
          Status = BacklogItemStatus.Planning
          ViewId = Some "sprint-1"
          Tasks = []
          Path = "/some/path/item.yaml" }
    let output = TestHelpers.captureOutput (fun () -> BacklogFormatter.formatDetail Text detail)
    Assert.Contains("my-feature", output)
    Assert.Contains("My Feature", output)
    Assert.Contains("planning", output)

[<Fact>]
let ``formatDetail Json contains status field`` () =
    let detail : BacklogItemDetail =
        { Item = makeItem "the-item" "The Item"
          Status = BacklogItemStatus.Approved
          ViewId = None
          Tasks = []
          Path = "/path/item.yaml" }
    let output = TestHelpers.captureOutput (fun () -> BacklogFormatter.formatDetail Json detail)
    Assert.Contains("the-item", output)
    Assert.Contains("approved", output)
