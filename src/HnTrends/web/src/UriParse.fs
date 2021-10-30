namespace App

open Microsoft.FSharp.Collections

module Util =
    let private splitSingleKvp (value: string) =
        match value.IndexOf('=') with
        | -1 -> ("", value)
        | x -> (value.Substring(0, x), value.Substring(x + 1))

    let UriParse (uri: System.Uri) =
        if System.String.IsNullOrEmpty uri.Query then
            dict Seq.empty<(string * string)>
        else
            uri
                .Query
                .Trim([| '?' |])
                .Split([| "&" |], System.StringSplitOptions.RemoveEmptyEntries)
            |> Seq.map splitSingleKvp
            |> dict
