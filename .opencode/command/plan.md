---
name: plan
description: Plan next feature based on BACKLOG and design docs
---
# Plan creation

## Context

`<itr root>` is set dynamically based on configuration. It contains a BACKLOG folder, among other project files. The BACKLOG folder contains subdirectories for each backlog item, each containing an `item.yaml` file describing the feature or task to be implemented, and a `tasks/` subdirectory containing task files.
 
Plans are created in `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/` and should be named `plan.md`. Each plan should be a detailed document that outlines the steps needed to implement the feature described in the corresponding BACKLOG item, along with any necessary context, dependencies, and acceptance criteria.

A backlog item can be broken into multiple tasks if it is large or complex, and each task would have its own plan. The plan should be detailed enough to guide the implementation process without needing to refer back to the design docs, but concise enough to be easily digestible by the development team or agent.

## Role

You are a software engineer on the team with a focus on agile project management and technical documentation.

## Objective

Create a plan for the next task to work on. The task id is provided as `$ARGUMENTS` (e.g. `backlog-take`). The task file is found at `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml`.

## Instructions

1. Review @PRODUCT.md for the overall project roadmap.
2. Review @ARCHITECTURE.md for the system design and architectural constraints.
3. Read the task file at `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/task.yaml` to identify the source backlog id and repo scope. The task id is provided via `$ARGUMENTS`.
4. Read the backlog item at `<itr root>/BACKLOG/<backlog-id>/item.yaml` for the full feature description, acceptance criteria, and dependencies.
5. Ask questions if any clarifications are needed about the task, its requirements, or its context before creating the plan.
6. Create a detailed plan for implementing the task, including:
    - A Status: Draft indicator at the top of the file.
    - A clear description of the task and its purpose.
    - Scope of the work involved, including any specific tasks or steps needed to implement the task.
    - Any dependencies or prerequisites that must be addressed before starting.
    - Impact on existing code and any necessary refactoring.
    - Acceptance criteria for when the task can be considered complete.
    - Testing strategy to ensure the task works as intended and does not break existing functionality.
    - Risks or challenges that may arise during implementation and how to mitigate them.
    - Open questions that need to be resolved before or during implementation.
7. Save the plan in a new file at `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/plan.md`.

## Constraints

- The plan should be detailed enough to guide the implementation process without needing to refer back to the design docs.
- The plan should not be verbose; focus on clarity and actionable steps. 2-page maximum is ideal.
- Do not implement the task or write any code; this is strictly for planning.

## Output

The output should be a markdown file at `<itr root>/BACKLOG/<backlog-id>/tasks/<task-id>/plan.md` with a detailed plan for implementing the next task, following the structure outlined in step 5. The file should be well-organized and clearly written to guide the implementation process.
