module Itr.Cli.CliArgs

open Argu

[<CliPrefix(CliPrefix.DoubleDash)>]
type TakeArgs =
    | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
    | Task_Id of task_id: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "backlog item id to take"
            | Task_Id _ -> "override the generated task id (single-repo items only)"

[<CliPrefix(CliPrefix.DoubleDash)>]
type AddArgs =
    | [<MainCommand>] Backlog_Id of backlog_id: string
    | Title of title: string
    | Repo of repo: string
    | Item_Type of item_type: string
    | Summary of summary: string
    | Priority of priority: string
    | Depends_On of depends_on: string
    | [<AltCommandLine("-i")>] Interactive

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "backlog item id (slug: [a-z0-9][a-z0-9-]*)"
            | Title _ -> "short title for the backlog item"
            | Repo _ -> "repo id to assign item to (required if product has multiple repos)"
            | Item_Type _ -> "item type: feature | bug | chore | refactor | spike (default: feature)"
            | Summary _ -> "longer description of the item"
            | Priority _ -> "priority label (e.g. high, medium, low)"
            | Depends_On _ -> "backlog item id this item depends on (can be repeated)"
            | Interactive -> "prompt for each field interactively"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ListArgs =
    | View of view: string
    | Status of status: string
    | Type of type_: string
    | Output of output: string
    | [<CustomCommandLine("--exclude")>] Exclude of status: string
    | Order_By of order_by: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | View _ -> "filter by view id"
            | Status _ ->
                "filter by status: created | planning | planned | approved | in-progress | completed | archived"
            | Type _ -> "filter by item type: feature | bug | chore | refactor | spike"
            | Output _ -> "output mode: table (default) | json | text"
            | Exclude _ -> "exclude items with this status (can be repeated)"
            | Order_By _ -> "override sort order: created | priority | type"

[<CliPrefix(CliPrefix.DoubleDash)>]
type InfoArgs =
    | [<MainCommand; Mandatory>] Backlog_Id of backlog_id: string
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "backlog item id to inspect"
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type BacklogArgs =
    | [<CliPrefix(CliPrefix.None)>] Take of ParseResults<TakeArgs>
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<AddArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<InfoArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Take _ -> "take a backlog item and create task files"
            | Add _ -> "create a new backlog item"
            | List _ -> "list backlog items"
            | Info _ -> "show detailed information about a backlog item"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfileAddArgs =
    | [<MainCommand; Mandatory>] Name of name: string
    | Git_Name of git_name: string
    | Git_Email of git_email: string
    | Set_Default

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Name _ -> "profile name (slug: [a-z0-9][a-z0-9-]*)"
            | Git_Name _ -> "git user name for this profile"
            | Git_Email _ -> "git user email for this profile"
            | Set_Default -> "set this profile as the default"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfileListArgs =
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfileSetDefaultArgs =
    | [<MainCommand; Mandatory>] Name of name: string
    | Local
    | Global

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Name _ -> "profile name to set as default"
            | Local -> "update the local itr.json in the current product root"
            | Global -> "update the global itr.json"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProfileArgs =
    | [<CliPrefix(CliPrefix.None)>] Add of ParseResults<ProfileAddArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ProfileListArgs>
    | [<CliPrefix(CliPrefix.None)>] Set_Default of ParseResults<ProfileSetDefaultArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Add _ -> "add a new profile to the portfolio"
            | List _ -> "list all profiles in the portfolio"
            | Set_Default _ -> "set an existing profile as the default"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductInitArgs =
    | [<MainCommand; Mandatory>] Path of path: string
    | Id of id: string
    | Repo_Id of repo_id: string
    | Coord_Path of coord_path: string
    | Coord_Mode of coord_mode: string
    | Register_Profile of register_profile: string
    | No_Register

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "target directory for the new product"
            | Id _ -> "product id (slug: [a-z0-9][a-z0-9-]*)"
            | Repo_Id _ -> "repo id (defaults to product id)"
            | Coord_Path _ -> "coordination directory path (default: .itr)"
            | Coord_Mode _ -> "coordination mode: primary-repo or standalone (default: primary-repo)"
            | Register_Profile _ -> "register the new product in this portfolio profile"
            | No_Register -> "skip registration in itr.json"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductRegisterArgs =
    | [<MainCommand; Mandatory>] Path of path: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Path _ -> "path to an existing product root directory"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductListArgs =
    | Profile of profile: string
    | Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "profile name (defaults to active profile)"
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductInfoArgs =
    | [<MainCommand>] Product_Id of product_id: string
    | [<AltCommandLine("-o")>] Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Product_Id _ -> "product id to inspect (optional; auto-detected from current directory if omitted)"
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ProductArgs =
    | [<CliPrefix(CliPrefix.None)>] Init of ParseResults<ProductInitArgs>
    | [<CliPrefix(CliPrefix.None)>] Register of ParseResults<ProductRegisterArgs>
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ProductListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<ProductInfoArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Init _ -> "scaffold a new product"
            | Register _ -> "register an existing product root in the portfolio"
            | List _ -> "list products registered in the active profile"
            | Info _ -> "show detailed information about a product"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskListArgs =
    | [<AltCommandLine("--backlog")>] Backlog_Id of backlog_id: string
    | [<AltCommandLine("--repo")>] Repo_Id of repo_id: string
    | State of state: string
    | [<AltCommandLine("-o")>] Output of output: string
    | Exclude of state: string
    | Order_By of field: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Backlog_Id _ -> "filter by backlog item id"
            | Repo_Id _ -> "filter by repo id"
            | State _ ->
                "filter by task state (planning | planned | approved | in_progress | implemented | validated | archived)"
            | Output _ -> "output mode: table (default) | json | text"
            | Exclude _ ->
                "exclude tasks with this state (planning | planned | approved | in_progress | implemented | validated | archived)"
            | Order_By _ -> "sort order: created | state"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskInfoArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string
    | [<AltCommandLine("-o")>] Output of output: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Task_Id _ -> "task id to inspect"
            | Output _ -> "output mode: table (default) | json | text"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskPlanArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string
    | Ai
    | Debug

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Task_Id _ -> "task id to plan"
            | Ai -> "use OpenCode AI to generate plan content"
            | Debug -> "print raw HTTP responses to stderr during AI interaction"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskApproveArgs =
    | [<MainCommand; Mandatory>] Task_Id of task_id: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Task_Id _ -> "task id to approve"

[<CliPrefix(CliPrefix.DoubleDash)>]
type TaskArgs =
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<TaskListArgs>
    | [<CliPrefix(CliPrefix.None)>] Info of ParseResults<TaskInfoArgs>
    | [<CliPrefix(CliPrefix.None)>] Plan of ParseResults<TaskPlanArgs>
    | [<CliPrefix(CliPrefix.None)>] Approve of ParseResults<TaskApproveArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | List _ -> "list all tasks across a product"
            | Info _ -> "show detailed information about a task"
            | Plan _ -> "generate a plan for a task"
            | Approve _ -> "approve a task plan"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ViewListArgs =
    | [<AltCommandLine("-o")>] Output of output: string
    | Product of product: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Output _ -> "output mode: table (default) | json | text"
            | Product _ -> "product id (defaults to product resolved from working directory)"

[<CliPrefix(CliPrefix.DoubleDash)>]
type ViewArgs =
    | [<CliPrefix(CliPrefix.None)>] List of ParseResults<ViewListArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | List _ -> "list all named backlog views for a product"

type CliArgs =
    | [<AltCommandLine("-p")>] Profile of string
    | Output of string
    | [<CliPrefix(CliPrefix.None)>] Backlog of ParseResults<BacklogArgs>
    | [<CustomCommandLine("profile")>] ProfileCmd of ParseResults<ProfileArgs>
    | [<CliPrefix(CliPrefix.None)>] Product of ParseResults<ProductArgs>
    | [<CliPrefix(CliPrefix.None)>] Task of ParseResults<TaskArgs>
    | [<CliPrefix(CliPrefix.None)>] View of ParseResults<ViewArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Profile _ -> "select active portfolio profile"
            | Output _ -> "set output mode (for example: json)"
            | Backlog _ -> "backlog commands"
            | ProfileCmd _ -> "profile management commands"
            | Product _ -> "product management commands"
            | Task _ -> "task commands"
            | View _ -> "view commands"
