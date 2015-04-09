#load "credentials.fsx"

open System
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open System.Net

let runtime = Runtime.GetHandle(config)
runtime.ShowWorkers()
runtime.ShowProcesses()
runtime.AttachClientLogger(ConsoleLogger())

let getContentLenght (url : string) = async {
    let request = HttpWebRequest.Create(url)
    let! response = request.AsyncGetResponse()  
    return response.ContentLength
}

let maxContentLenght = cloud {
    let lengthsJob = 
        [|
            "https://github.com";
            "http://www.microsoft.com/"
        |] |> Array.map (getContentLenght >> Cloud.OfAsync)
     
    let! lengths = Cloud.Parallel lengthsJob
    return Array.max lengths
}