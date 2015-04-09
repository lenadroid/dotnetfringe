#load "credentials.fsx"

open System
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Workflows

let cluster = Runtime.GetHandle(config)
cluster.ShowProcesses()
cluster.ShowWorkers()
cluster.AttachClientLogger(ConsoleLogger())

async {
    let value = ref 0
    let! _ = Async.Parallel [| for i in 1 .. 1000000 -> async { incr value } |]
    printfn "%A" !value 
} |> Async.RunSynchronously

cloud {
    let value = ref 0
    let! _ = Cloud.Parallel [| for i in 1 .. 100 -> cloud { incr value } |]
    return !value 
} |> cluster.Run

let atomicValue = CloudAtom.New(42) |> cluster.Run

let readAtomValue = atomicValue |> CloudAtom.Read |> cluster.Run

cloud {
    do!
        [ for i in 1..1000 -> cloud { return! CloudAtom.Update (atomicValue, fun i -> i + 1) }]
        |> Cloud.Parallel
        |> Cloud.Ignore
    return! CloudAtom.Read atomicValue
} |> cluster.Run

atomicValue  |> CloudAtom.Delete |> cluster.Run
