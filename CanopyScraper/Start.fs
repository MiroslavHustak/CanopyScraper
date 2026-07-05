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

open MyCanopy.KodisCanopy           //Edge
//open MyCanopyChrome.MyCanopyChrome //Chrome // Kubernetes

open MyCanopy.ApiClient

open Drivers.EdgeDriver

open Settings.SettingsCanopy
open Serialization.Serialisation

open Helpers.ProcessHelpers
open Helpers.InteractiveHelpers 
open Helpers.Haskell_IO_Monad_Simulation

type [<Struct>] private endTime =
    {
        nowEnd : DateTime
        hourEnd : int
        minuteEnd : int
        secondEnd : int
    }

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

    let mainProcess shutDownInvoked = 

        match Environment.OSVersion.Platform = PlatformID.Win32NT with
        | true  
            -> 
            result
               {    
                   do! ensureDriver ()
                   killEdgeZombies ()

                   eprintfn "\nPress any key to continue"
                   Console.ReadKey() |> ignore<ConsoleKeyInfo>

                   do! canopyResultKodis >> runIO <| ()

                   return eprintfn "Scraping and serialization completed successfully"
               }
        | false
            -> 
            result
                {
                    //do! canopyResultKodis >> runIO <| ()
                    return () //eprintfn "Scraping and serialization completed successfully"
                }

        |> function
            | Ok _ 
                -> 
                let result : ResponsePut = putToRestApiTest >> runIO <| ()
                
                eprintfn "%s" result.Message1 
                eprintfn "%s" result.Message2     
                
                match Environment.OSVersion.Platform = PlatformID.Win32NT with
                | true  -> killEdgeZombies () 
                | false -> killChromeZombies () 
                
                let nowEnd = DateTime.Now
                let hourEnd = nowEnd.Hour 
                let minuteEnd = nowEnd.Minute 
                let secondEnd = nowEnd.Second
                
                eprintfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart
                eprintfn "The end time: %02d:%02d:%02d" hourEnd minuteEnd secondEnd
                
                match isInKubernetes || isInContainer with
                | false  
                    ->
                    eprintfn "Press any key to continue to the main page"
                    shutDownInvoked |> function false -> Console.ReadKey() |> ignore<ConsoleKeyInfo> | true -> ()
                    { nowEnd = nowEnd; hourEnd = hourEnd; minuteEnd = minuteEnd; secondEnd = secondEnd }
                | _true 
                    -> 
                    { nowEnd = nowEnd; hourEnd = hourEnd; minuteEnd = minuteEnd; secondEnd = secondEnd }         
            
            | Error err
                ->  
                eprintfn "A problem appeared: %s" err
                { nowEnd = DateTime.Now; hourEnd = DateTime.Now.Hour ; minuteEnd = DateTime.Now.Minute; secondEnd = DateTime.Now.Second }        
    
    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart

    try
        match isInKubernetes || isInContainer with
        | false  
            ->
            printfn "Canopy (F#) web testing tool. Shutdown PC after finishing? [y/N]: "

            match Console.ReadKey().Key with    
            | ConsoleKey.Y
                -> 
                let shutDownInvoked = true
                let result = mainProcess shutDownInvoked 
                let list = 
                    [ sprintf "Scraping and serialization completed successfully at %02i:%02d:%02d" result.hourEnd result.minuteEnd result.secondEnd
                      sprintf "The start time: %02i:%02d:%02d" hourStart minuteStart secondStart
                      sprintf "The end time: %02i:%02d:%02d" result.hourEnd result.minuteEnd result.secondEnd
                    ]
                (runIO <| serializeWithThothSync list pathShutDownMsg) |> ignore<Result<unit, string>> 
                System.Diagnostics.Process.Start("shutdown", "/s /t 60") |> ignore<Diagnostics.Process>
            | _ -> 
                let shutDownInvoked = false
                //Console.ReadKey() |> ignore<ConsoleKeyInfo> 
                mainProcess shutDownInvoked |> ignore<endTime>
        | true
            -> 
            let shutDownInvoked = false
            printfn "Canopy (F#) web testing tool."
            mainProcess shutDownInvoked |> ignore<endTime>
    with
    | ex -> eprintfn "A fatal problem appeared: %s" (string ex.Message)

    0