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
    
    killEdgeZombies () 

    let nowStart = DateTime.Now
    let hourStart = nowStart.Hour 
    let minuteStart = nowStart.Minute 
    let secondStart = nowStart.Second
    
    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart
    printfn "Canopy (F#) web testing tool. Stiskni cokoliv pro pokračování testu."
    
    Console.ReadKey () |> ignore<ConsoleKeyInfo>
            
    match canopyResult >> runIO <| () with
    | Ok _      -> printfn "\nScraping a serializace proběhla v pořádku." 
    | Error err -> printfn "Nastal tento problém: %s" err 

    let result : ResponsePut = putToRestApiTest>> runIO <| ()

    printfn "%s" result.Message1 
    printfn "%s" result.Message2     

    killEdgeZombies () 

    let nowEnd = DateTime.Now
    let hourEnd = nowEnd.Hour 
    let minuteEnd = nowEnd.Minute 
    let secondEnd = nowEnd.Second

    printfn "\nThe start time: %02i:%02d:%02d" hourStart minuteStart secondStart
    printfn "The end time: %02d:%02d:%02d" hourEnd minuteEnd secondEnd

    printfn "Stiskni cokoliv pro návrat na hlavní stránku."
    Console.ReadKey() |> ignore<ConsoleKeyInfo>

    0