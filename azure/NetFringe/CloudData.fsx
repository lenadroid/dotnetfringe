#load "credentials.fsx"

open System
open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime
open MBrace.Flow

let cluster = Runtime.GetHandle(config)
cluster.ShowProcesses()
cluster.ShowWorkers()
cluster.AttachClientLogger(ConsoleLogger())

let data = "Some data" 

let cloudData = data |> CloudCell.New |> cluster.Run

let lengthOfData = 
    cloud { let! data = CloudCell.Read cloudData 
            return data.Length }
    |> cluster.Run

let vectorOfData = [| for i in 0 .. 1000 do for j in 0 .. i do yield (i,j) |]

let cloudVector = CloudVector.New(vectorOfData,100000L) |> cluster.Run

cloudVector.PartitionCount

let lengthsJob = 
    cloudVector
    |> CloudFlow.ofCloudVector
    |> CloudFlow.map (fun (a,b) -> a+b)
    |> CloudFlow.sum
    |> cluster.CreateProcess

lengthsJob.ShowInfo()

lengthsJob.Completed

let lengths =  lengthsJob.AwaitResult()

