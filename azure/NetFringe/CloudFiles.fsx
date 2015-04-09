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

let cluster = Runtime.GetHandle(config)
cluster.ShowProcesses()
cluster.ShowWorkers()
cluster.AttachClientLogger(ConsoleLogger())

let directory = cluster.StoreClient.FileStore.Directory.Create()

let songFileNames = 
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

let download (url : string) = async {
    let http = new System.Net.WebClient() 
    return! http.AsyncDownloadString(Uri url)
}

let songCloudFile (song:string) (dir: CloudDirectory) = cloud { 
    let songName = song.Substring(0, song.IndexOf("\n") - 1).Trim().Replace("\"", "")
    let fileName = Path.Combine(dir.Path, songName)
    do! CloudFile.Delete fileName 
    return! CloudFile.WriteAllText(song, path = fileName) 
}

let songCloudFilesProcess = 
    songFileNames
    |> Array.map (fun i -> cloud {
        let! text =  download i |> Cloud.OfAsync
        return! songCloudFile text directory
    })
    |> Cloud.Parallel 
    |> cluster.Run

let displayLyrics (song: string) (dir: CloudDirectory) = cloud {
    let fileName = Path.Combine(dir.Path, song)
    return! CloudFile.ReadAllText fileName 
}

let songLyrics = displayLyrics "Smells Like Teen Spirit" directory
                 |> cluster.Run

let showDirectoryFiles (dir: string) = cloud {
    let! exists = CloudDirectory.Exists dir
    if exists then 
        let! cloudFiles = CloudFile.Enumerate dir
        return cloudFiles |> Array.map (fun f -> f.Path.Replace("%20", " "))
    else return [||]
}

showDirectoryFiles directory.Path |> cluster.Run

let clearDirectory (dir: string) = cloud {
    do! CloudDirectory.Delete dir
}

clearDirectory directory.Path |> cluster.Run

cloud {
    use directory = cluster.StoreClient.FileStore.Directory.Create()
    let! exists = CloudDirectory.Exists directory
    if exists then 
        let! cloudFiles = CloudFile.Enumerate directory
        return cloudFiles |> Array.map (fun f -> f.Path.Replace("%20", " "))
    else return [||]
} |> cluster.Run

