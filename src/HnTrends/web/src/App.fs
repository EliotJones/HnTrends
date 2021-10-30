module App

open Browser.Dom
open App
open System.Collections.Generic
open System.Text.RegularExpressions
open Fetch
open System

type TermData =
    abstract counts : int []
    abstract scores : int []
    abstract term : string
    abstract allWords : bool

type QueryTerm =
    { mutable Term: string
      mutable AllWords: bool }

// Mutable variable to count the number of times we clicked the button
let mutable count = 0

let private parseAllWord (s: string) =
    match bool.TryParse s with
    | true, value -> value
    | _ -> false

let private unpack (termIndex: int, input: string, dict: Dictionary<int, QueryTerm>, isTerm: bool) =
    match dict.TryGetValue termIndex with
    | true, value ->
        if isTerm then
            value.Term <- input
        else
            value.AllWords <- parseAllWord (input)
    | _ ->
        dict.Add(
            termIndex,
            { Term = (if isTerm then input else "")
              AllWords =
                (if isTerm then
                     true
                 else
                     parseAllWord (input)) }
        )

let private matchAllWords (key: string, value: string, dict: Dictionary<int, QueryTerm>) =
    let allWordMatch =
        Regex.Match(key, @"allWords(?<index>\d+)")

    match allWordMatch.Success with
    | true -> unpack (System.Int32.Parse(allWordMatch.Groups.["index"].Value), value, dict, false)
    | false -> ()

let private collectTerms (query: IDictionary<string, string>) =
    let myDict = Dictionary<int, QueryTerm>()

    query
    |> Seq.iter (fun x ->
        let termMatch =
            Regex.Match(x.Key, @"trend(?<index>\d+)")

        match termMatch.Success with
        | true -> unpack (System.Int32.Parse(termMatch.Groups.["index"].Value), x.Value, myDict, true)
        | false -> matchAllWords (x.Key, x.Value, myDict))

    myDict


let parsed =
    System.Uri(window.location.toString ())
    |> Util.UriParse
    |> collectTerms

// Get a reference to our button and cast the Element to an HTMLButtonElement
let myButton =
    document.querySelector (".my-button") :?> Browser.Types.HTMLButtonElement


let loadSingleSeries (term: QueryTerm) =
    let url =
        $"/api/plot/{term.Term}?allWords={term.AllWords}"

    let resultPromise =
        fetch url []
        |> Promise.bind (fun response ->
            let arr = response.json<TermData> ()
            arr)
        |> Promise.map (Ok)
        |> Promise.catch (Error)

    resultPromise

let filterOutErrors (input: Result<TermData, exn>) =
    match input with
    | Ok v -> true
    | _ -> false

let dataArrays =
    parsed
    |> Seq.map (fun term -> loadSingleSeries term.Value)
    |> Promise.Parallel
    |> Promise.tap (fun allData ->
        let values, err =
            allData |> Array.partition filterOutErrors

        let principle = values.[0]
        ())

// Register our listener
myButton.onclick <-
    fun _ ->
        count <- count + 1
        myButton.innerText <- sprintf "You clicked: %i times" count
