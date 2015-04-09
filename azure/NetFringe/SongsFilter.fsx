#load "credentials.fsx"

open System
open System.IO
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Flow
open MBrace.Workflows
open Nessos.Streams
open System.Text
open System.Net
open System.Threading
open Visify

let runtime = Runtime.GetHandle(config)
runtime.ShowWorkers()
runtime.ShowProcesses()
runtime.AttachClientLogger(ConsoleLogger())

let download (url : string) = async {
    let http = new System.Net.WebClient()
    let! html = http.AsyncDownloadString(Uri url)
    printfn "%A" html
    return html
}

let split (list: list<'a>) = 
    let rec split' (n:int, ls: list<'a>) = 
        match n, ls with
            |(n,[])->([],[])
            |(1,x::xs)->(x::[],xs)
            |(n,x::xs)->  let (f, s) = split'(n-1,xs) in (x::f, s)
    split'(list.Length/2, list)

let urls = 
    [
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjeHN2dDlJVkY3ZVE";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjU1gtN0Mxb2Z4TVU";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjRnBpVkJMRkx3SU0";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjS2ZXdld1dUJGbVU";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjRFNKNDhrNUVwR2c";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjWGtPcE5YaHloc2c";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjV3RfSnJ6T09UczA";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjci1MaEZpV1dsRTA";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjOHIzVGNkODJVSmM";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjbXFJUUpobHE2V3c";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjbkdha1QtUzlHc1E";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjRnBoVWJDSkRsWmM";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjXzdYa0cwUE00UHM";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjOFl2WHV5S1pydmc";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjQ29aZGlVXzVwSzA";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjTWx2WFlxNFFFc00";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjYVRaY0tpSTYyQlE";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjQWxLalhwWHhUeFU";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjMnJ2UW9fanQ0c1U";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjejJJNHJUM2lZdjA";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjV1JVTkNoOThoWkE";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjdV95bnppSHhaXzQ";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjNFllV2JybVRLM1k";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjYUFHTGtKaWpXSUE";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjNlVXcFlldXYyd0U";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjaWQzRnNBUkhOTVk";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjXzd2VGs5elpJWHc";
        @"https://drive.google.com/uc?export=download&id=0B9njtBs_LTJjSUt2SjFJNDBYSUk";
    ]

let restricted = [|"die"; "sick"; "kill";"bad"; "mad"; "war"; "weapon"; "rape"; "gun"|]

[<Cloud>]
let rec mapReduce (map: 'T -> Cloud<'R>)
                  (reduce: 'R -> 'R -> Cloud<'R>)
                  (identity: 'R) (input: 'T list) =
                    cloud {
                        match input with
                            | [] -> return identity
                            | [value] -> return! map value
                            | _ ->
                                let left, right = split input

                                let! r1, r2 =
                                    (mapReduce map reduce identity left)
                                    <||>
                                    (mapReduce map reduce identity right)
                                return! reduce r1 r2
                    }


[<Cloud>] 
let map (uri : string) = 
    cloud {
        let! text = Cloud.OfAsync <| download uri
        let restrictedCount = text.ToLower().Split(restricted, System.StringSplitOptions.None).Length - 1
        let firstLine = text.Substring(0,text.IndexOf("\n")-1)
        return [|(firstLine, restrictedCount)|]
    }

[<Cloud>]
let reduce (occurences1 : (string * int)[]) (occurences2 : (string * int)[]) =
    cloud {
        return
            Seq.append occurences1 occurences2
            |> Seq.sortBy (fun (firstLine, count) -> -count)
            |> Seq.toArray
    }

let mapreduceprocess = runtime.CreateProcess (mapReduce map reduce [||] urls)
let result = mapreduceprocess.AwaitResult()
result |> displayResults
