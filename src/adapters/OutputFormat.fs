namespace Itr.Adapters

/// Shared output format discriminated union, replacing ad-hoc `bool outputJson` parameters.
type OutputFormat =
    | Json
    | Text
    | Table

[<RequireQualifiedAccess>]
module OutputFormat =

    /// Parse a format string to `OutputFormat`. Returns `None` for unrecognised values.
    let tryParse (value: string option) : OutputFormat =
        match value with
        | Some "json" -> Json
        | Some "text" -> Text
        | _ -> Table
