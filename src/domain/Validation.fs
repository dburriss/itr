namespace Itr.Domain

open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module Validation =
    let slugRegex = Regex("^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled)

    let isValidSlug (value: string) : bool =
        not (System.String.IsNullOrWhiteSpace(value)) && slugRegex.IsMatch(value)
