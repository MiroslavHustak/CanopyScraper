(*
    Code in this file uses Canopy created by Chris Holt, Amir Rajan, and Jeremy Bellows.
    Copyright (c) 2011 Chris Holt
    Licensed under the MIT License
    https://github.com/lefthandedgoat/canopy
*)

open System

open FsToolkit.ErrorHandling

// Edge <=> Chrome/Kubernetes switch 
//***************************************

open MyCanopy.MyCanopy           //Edge
//open MyCanopyChrome.MyCanopyChrome //Chrome // Kubernetes

open MyCanopy.ApiClient

open Drivers.EdgeDriver

open Helpers.ProcessHelpers
open Helpers.InteractiveHelpers 
open Helpers.Haskell_IO_Monad_Simulation

[<EntryPoint>] 
let main argv =      
    
    match Environment.OSVersion.Platform = PlatformID.Win32NT with
    | true  -> killEdgeZombies () 
    | false -> killChromeZombies () 

    let nowUtc = DateTime.UtcNow
    let ostravaTz = TimeZoneInfo.FindSystemTimeZoneById "Central European Standard Time"
    let nowOstrava = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ostravaTz)

    //let nowStart = DateTime.Now
    let nowStart = nowOstrava
    let hourStart = nowStart.Hour 
    let minuteStart = nowStart.Minute 
    let secondStart = nowStart.Second
    
    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart

    match isInKubernetes || isInContainer with
    | false  
        ->
        printfn "Canopy (F#) web testing tool. Press any key to continue."
        Console.ReadKey() |> ignore<ConsoleKeyInfo>
    | true
        -> 
        printfn "Canopy (F#) web testing tool."
    
    match Environment.OSVersion.Platform = PlatformID.Win32NT with
    | true  
        -> 
        result
           {
               do! ensureDriver ()
               killEdgeZombies ()

               printfn "Press any key to continue."
               Console.ReadKey() |> ignore<ConsoleKeyInfo>
               eprintfn "The relevant scraping process is continuing ... "

               do! canopyResult >> runIO <| ()

               return eprintfn "Scraping and serialization completed successfully."
           }
    | false
        -> 
        result
            {
                do! canopyResult >> runIO <| ()
                return eprintfn "Scraping and serialization completed successfully."
            }

    |> Result.defaultWith (fun err -> eprintfn "A problem appeared: %s" err)     

    let result : ResponsePut = putToRestApiTest >> runIO <| ()

    printfn "%s" result.Message1 
    printfn "%s" result.Message2     

    match Environment.OSVersion.Platform = PlatformID.Win32NT with
    | true  -> killEdgeZombies () 
    | false -> killChromeZombies () 

    let nowEnd = DateTime.Now
    let hourEnd = nowEnd.Hour 
    let minuteEnd = nowEnd.Minute 
    let secondEnd = nowEnd.Second

    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart
    printfn "The end time: %02d:%02d:%02d" hourEnd minuteEnd secondEnd

    match isInKubernetes || isInContainer with
    | false  
        ->
        printfn "Press any key to continue to the main page."
        Console.ReadKey() |> ignore<ConsoleKeyInfo>
    | _true 
        -> 
        ()

    0