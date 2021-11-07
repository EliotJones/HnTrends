module App

open Browser.Dom
open App
open System.Collections.Generic
open System.Text.RegularExpressions
open Fetch
open System
open Fable.MomentJs.MomentJs

type TermData =
    abstract counts : int []
    abstract scores : int []
    abstract term : string
    abstract allWords : bool
    abstract start : DateTime

type QueryTerm =
    { mutable Term: string
      mutable AllWords: bool }

type Interval =
    | Day
    | Week
    | Month
    | Year

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
    Uri(window.location.toString ())
    |> Util.UriParse
    |> collectTerms

// Get a reference to our button and cast the Element to an HTMLButtonElement
let myButton =
    document.querySelector (".my-button") :?> Browser.Types.HTMLButtonElement


let loadSingleSeries (term: QueryTerm) =
    promise {
        let url =
            $"/api/plot/{term.Term}?allWords={term.AllWords}"

        let! plotDataResult = tryFetch url []

        match plotDataResult with
        | Ok resp ->
            let! termData = resp.json<TermData> ()
            return Ok termData
        | Error ex -> return Error ex
    }

let generateSeries (rawSeries: TermData array, interval: Interval) =
    let first = rawSeries.[0]

    let earliestDate = moment.utc first.start

    let periodOfTime =
        match interval with
        | Day -> "day"
        | Week -> "week"
        | Month -> "month"
        | Year -> "year"

    let initialDate =
        earliestDate.clone().startOf (periodOfTime)

    let latestDate =
        earliestDate
            .clone()
            .add ((float (first.counts.Length + 1)), "days")

    let bucketCount =
        int (ceil (latestDate.diff (earliestDate, periodOfTime)))

    console.log (
        "There are",
        bucketCount,
        periodOfTime,
        "between",
        initialDate.format ("YYYY-MM-DD"),
        latestDate.format ("YYYY-MM-DD")
    )

    let dateBuckets =
        [ 0 .. bucketCount ]
        |> Seq.map (fun i ->
            let increment =
                match interval with
                | Day -> "days"
                | Week -> "weeks"
                | Month -> "months"
                | Year -> "years"

            let output =
                (moment.utc initialDate)
                    .add ((float i), increment)

            output)
        |> Seq.toArray

    console.log (dateBuckets)

    // Next up let's split each series into the corresponding date buckets based on the active
    // count type and then plot them.
    ()


let generateSeriesFromPromise (loadingPromise: Fable.Core.JS.Promise<Result<TermData, Exception> array>) =
    promise {
        let! allData = loadingPromise

        let successValues =
            allData
            |> Array.choose (fun r ->
                match r with
                | Ok ok -> Some ok
                | Error _ -> None)

        generateSeries (successValues, Month)
    }

let dataArrays =
    parsed
    |> Seq.map (fun term -> loadSingleSeries term.Value)
    |> Promise.Parallel

ignore (generateSeriesFromPromise (dataArrays))

// Register our listener
myButton.onclick <-
    fun _ ->
        count <- count + 1
        myButton.innerText <- sprintf "You clicked: %i times" count
