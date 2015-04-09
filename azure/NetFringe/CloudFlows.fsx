#load "credentials.fsx"

open System
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Flow
open MBrace.Workflows
open System.Net

let cluster = Runtime.GetHandle(config)
cluster.ShowWorkers()
cluster.ShowProcesses()
cluster.AttachClientLogger(ConsoleLogger())

let download (url : string) =  
    let http = new System.Net.WebClient()
    let html = http.DownloadString(Uri url)
    printfn "%A" html
    html

let songFiles = 
    [|
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
    |]
    |> CloudFlow.ofArray
    |> CloudFlow.map download
    |> CloudFlow.filter (fun s -> s.Length > 300)
    |> CloudFlow.map (fun s -> s.Substring(0, s.IndexOf("\n") - 1))
    |> CloudFlow.toArray
    |> cluster.CreateProcess

songFiles.AwaitResult()


