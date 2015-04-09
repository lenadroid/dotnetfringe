#load "credentials.fsx"

open System
open System.IO
open System.Collections.Generic
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Flow
open MBrace.Workflows
open Nessos.Streams

let cluster = Runtime.GetHandle(config)
cluster.ShowProcesses()
cluster.ShowWorkers()
cluster.AttachClientLogger(ConsoleLogger())

let channel = cluster.StoreClient.Channel
let sendPort1, receivePort1 = channel.Create<string>()
let sendPort2, receivePort2 = channel.Create<string>()

let doSomething (receive : IReceivePort<string>) (send : ISendPort<string>) = 
    cloud {
        while true do
            let! result = Cloud.Catch <| receive.Receive()
            let randomNumber = (new System.Random()).Next(1, 7)
            match result with
            | Choice1Of2 x -> do! send.Send(String.replicate randomNumber x)
            | Choice2Of2 _ -> ()
    }
let job = cluster.CreateProcess(doSomething receivePort1 sendPort2)

let sendSomething message = async { return! channel.SendAsync(sendPort1, message) } 
let receiveMessages = async {
        while true do
            let! result = channel.ReceiveAsync(receivePort2) 
            printfn "Received: %A" result
}

// start receiving response messages
let receiveCancel = new System.Threading.CancellationTokenSource()
Async.Start(receiveMessages, cancellationToken = receiveCancel.Token)    

// send some message
sendSomething "FSharp "|> Async.Start
sendSomething "Cloud " |> Async.Start
sendSomething "Monad " |> Async.Start

// kill all jobs
receiveCancel.Cancel()
job.Kill()
channel.Delete(receivePort1)
channel.Delete(receivePort2)


