module Itr.Tests.Communication.TaskFormatterTests

open System
open Xunit
open Itr.Domain
open Itr.Domain.Tasks
open Itr.Adapters

let private makeTask id repo state =
    { Id = TaskId.create id
      SourceBacklog = BacklogId "test-backlog"
      Repo = RepoId repo
      State = state
      CreatedAt = DateOnly(2024, 1, 15) }

// ---------------------------------------------------------------------------
// TaskFormatter.formatList tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``formatList Text outputs task id and state`` () =
    let task = makeTask "abc-repo-001" "repo1" TaskState.Planning
    let rows : TaskListSummary list =
        [ { Task = task; TaskYamlPath = "/some/task.yaml"; PlanMdPath = None } ]
    let output = TestHelpers.captureOutput (fun () -> TaskFormatter.formatList Text rows)
    Assert.Contains("abc-repo-001", output)
    Assert.Contains("planning", output)

[<Fact>]
let ``formatList Json wraps in tasks object`` () =
    let task = makeTask "xyz-repo-002" "repo1" TaskState.Approved
    let rows : TaskListSummary list =
        [ { Task = task; TaskYamlPath = "/some/task.yaml"; PlanMdPath = Some "/some/plan.md" } ]
    let output = TestHelpers.captureOutput (fun () -> TaskFormatter.formatList Json rows)
    Assert.Contains("tasks", output)
    Assert.Contains("xyz-repo-002", output)
    Assert.Contains("approved", output)

[<Fact>]
let ``formatList Text empty produces no output`` () =
    let output = TestHelpers.captureOutput (fun () -> TaskFormatter.formatList Text [])
    Assert.Equal("", output.Trim())

// ---------------------------------------------------------------------------
// TaskFormatter.formatDetail tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``formatDetail Text outputs state and plan info`` () =
    let task = makeTask "task-detail-001" "repo1" TaskState.Planned
    let detail : TaskDetailView =
        { Task = task; Siblings = []; PlanExists = true; PlanApproved = false
          TaskYamlPath = "/path/task.yaml"; PlanMdPath = Some "/path/plan.md" }
    let output = TestHelpers.captureOutput (fun () -> TaskFormatter.formatDetail Text detail)
    Assert.Contains("task-detail-001", output)
    Assert.Contains("planned", output)
    Assert.Contains("plan exists", output)

[<Fact>]
let ``formatDetail Json contains id and planExists`` () =
    let task = makeTask "task-json-001" "repo1" TaskState.Approved
    let detail : TaskDetailView =
        { Task = task; Siblings = []; PlanExists = true; PlanApproved = true
          TaskYamlPath = "/path/task.yaml"; PlanMdPath = None }
    let output = TestHelpers.captureOutput (fun () -> TaskFormatter.formatDetail Json detail)
    Assert.Contains("task-json-001", output)
    Assert.Contains("planExists", output)
