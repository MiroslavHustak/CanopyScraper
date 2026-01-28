open System
open System.Diagnostics

open MyCanopy.ApiClient
open Helpers.ProcessHelpers

(*
    Code in this file uses Canopy created by Chris Holt, Amir Rajan, and Jeremy Bellows.
    Copyright (c) 2011 Chris Holt
    Licensed under the MIT License
    https://github.com/lefthandedgoat/canopy
*)

[<EntryPoint>] 
let main argv =      
    
    killEdgeZombies () 

    let nowStart = DateTime.Now
    let hourStart =  nowStart.Hour 
    let minuteStart = nowStart.Minute 
    let secondStart = nowStart.Second

    printfn "Canopy (F#) web testing tool. Stiskni cokoliv pro pokračování testu."
    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart
    Console.ReadKey () |> ignore
            
    match MyCanopy.MyCanopy.canopyResult() with
    | Ok _      -> printfn "\nScraping a serializace proběhla v pořádku." 
    | Error err -> printfn "Nastal tento problém: %s" err 

    let result : ResponsePut = putToRestApiTest ()
    printfn "%s" result.Message1 
    printfn "%s" result.Message2 

    Console.ReadKey () |> ignore

    let nowEnd = DateTime.Now
    let hourEnd  =  nowEnd.Hour 
    let minuteEnd  = nowEnd.Minute 
    let secondEnd  = nowEnd.Second

    killEdgeZombies () 

    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart
    printfn "The end time: %02d:%02d:%02d" hourEnd minuteEnd secondEnd

    printfn "Stiskni cokoliv pro návrat na hlavní stránku."
    Console.ReadKey() |> ignore

    0

