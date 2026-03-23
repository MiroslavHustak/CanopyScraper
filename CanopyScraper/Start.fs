(*
    Code in this file uses Canopy created by Chris Holt, Amir Rajan, and Jeremy Bellows.
    Copyright (c) 2011 Chris Holt
    Licensed under the MIT License
    https://github.com/lefthandedgoat/canopy
*)

open System

open MyCanopy.MyCanopy
open MyCanopy.ApiClient

open Helpers.ProcessHelpers
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

    let isInteractive = System.Environment.GetEnvironmentVariable("INTERACTIVE")

    match isInteractive = "true" with
    | true  
        ->
        printfn "Canopy (F#) web testing tool. Stiskni cokoliv pro pokračování testu."
        Console.ReadKey() |> ignore<ConsoleKeyInfo>
    | false
        -> 
        printfn "Canopy (F#) web testing tool."
            
    match canopyResult >> runIO <| () with
    //match MyCanopyChrome.MyCanopyChrome.canopyResult >> runIO <| () with
    | Ok _      -> printfn "\nScraping a serializace proběhla v pořádku." 
    | Error err -> printfn "Nastal tento problém: %s" err 

    let result : ResponsePut = putToRestApiTest>> runIO <| ()

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

    match isInteractive = "true" with
    | true  
        ->
        printfn "Stiskni cokoliv pro návrat na hlavní stránku."
        Console.ReadKey() |> ignore<ConsoleKeyInfo>
    | false
        -> 
        ()

    0