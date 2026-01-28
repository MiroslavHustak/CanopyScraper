(*
    Code in this file uses Canopy created by Chris Holt, Amir Rajan, and Jeremy Bellows.
    Copyright (c) 2011 Chris Holt
    Licensed under the MIT License
    https://github.com/lefthandedgoat/canopy
*)

namespace MyCanopy

open System
open System.IO
open System.Net
open System.Threading

open OpenQA.Selenium.Edge

open FsHttp
open Thoth.Json.Net

open Helpers
open Helpers.Builders

open Data.InputData
open Settings.Settings
open Serialization.Serialisation

module MyCanopy = 
    
    let internal canopyResult () = 
    
        let urlsChanges = 
            2115 :: [ 2400 .. 2800 ]
            |> List.map (fun item -> sprintf "%s%s" "https://www.kodis.cz/changes/" (string item))

        let scrapeGeneral () = 
            canopy.classic.elements "a"
            |> List.map 
                (fun item 
                    ->                                                     
                    let href = string <| item.GetAttribute("href")
                    match href.EndsWith("pdf") with
                    | true  -> Some href     
                    | false -> None                                                                    
                )    

        let clickCondition () =
            try                             
                let nextButton = canopy.classic.elementWithText "a" "Další"
                nextButton.Displayed && nextButton.Enabled
            with
            | _ -> false  

        let startHeadlessEdge () =

           try
               canopy.configuration.edgeDir <- pathToDriver
   
               let service = EdgeDriverService.CreateDefaultService(canopy.configuration.edgeDir)
               service.HideCommandPromptWindow <- true
           
               let options = EdgeOptions()
   
               let edgeOptsInner = 
                   dict
                       [ 
                           "args", 
                               box 
                                   [| 
                                       "--headless=new"
                                       "--disable-gpu" 
                                       "--no-sandbox" 
                                       "--disable-dev-shm-usage"
                                       "--window-size=1920,1080"
                                       "--disable-blink-features=AutomationControlled"
                                   |] 
                       ]
   
               options.AddAdditionalCapability("ms:edgeOptions", edgeOptsInner)
               let driver = new EdgeDriver(service, options)
               canopy.classic.browser <- driver
               canopy.configuration.compareTimeout <- 100.0
   
               Ok ()
           with
           | ex 
               ->
               eprintfn "CRITICAL: Failed to start Edge driver: %s" (string ex.Message)
               eprintfn "Driver path: %s" canopy.configuration.edgeDir
               eprintfn "Make sure msedgedriver.exe matches your Edge version (edge://version/)"
               Error (string ex.Message)

        let changesLinks () = 

            match startHeadlessEdge () with
            | Error _ 
                -> 
                []
            | Ok _ 
                ->
                try
                    try
                        let linksShown () = 
                            Some (canopy.classic.elements "ul > li > div" |> Seq.length >= 1)
                  
                        let scrapeUrl (url: string) =
                            try
                                canopy.classic.url url
                                Thread.Sleep 50  
                                
                                let waitForWithTimeout (timeoutSeconds : float) (condition : unit -> bool option) =
                                    let timeout = System.TimeSpan.FromSeconds timeoutSeconds
                                    let sw = System.Diagnostics.Stopwatch.StartNew()
                                
                                    Seq.initInfinite id
                                    |> Seq.takeWhile (fun _ -> sw.Elapsed < timeout)
                                    |> Seq.tryPick 
                                        (fun _ 
                                            -> 
                                            condition () 
                                            |> Option.orElse (System.Threading.Thread.Sleep 250; None)
                                        )
                                    |> Option.defaultValue false                           
                               
                                match waitForWithTimeout 5.0 linksShown with
                                | true 
                                    ->
                                    scrapeGeneral ()
                                    |> List.choose id  
                                    |> List.distinct
                                    |> List.filter 
                                        (fun item -> item.Contains urlKodis)                                
                                | false 
                                    -> 
                                    []
                            with
                            | _ -> []

                        urlsChanges 
                        |> List.collect scrapeUrl
                        |> List.filter (fun item -> not <| (item.Contains "2022" || item.Contains "2023" || item.Contains "2024"))

                    with
                    | _ -> []
                finally
                    canopy.classic.quit()
        
        let currentAndFutureLinks () = 

            match startHeadlessEdge () with
            | Error _ 
                -> 
                []
            | Ok _ 
                ->
                try
                    try
                        let linksShown () = 
                            (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1               

                        let scrapeUrl (url : string) =
                            try
                                canopy.classic.url url

                                let pdfLinkList () =
                                    Thread.Sleep 15000            
                                
                                    canopy.classic.waitFor linksShown                         
                                
                                    let buttons = 
                                        let originalError = Console.Error
                                        try
                                            Console.SetError(System.IO.TextWriter.Null)
                                            
                                            let buttons = 
                                                try
                                                    canopy.classic.elements "button[title='Budoucí jízdní řády']"
                                                with
                                                | _ -> []
                                            
                                            Console.SetError(originalError)
                                            buttons
                                        with
                                        | ex -> 
                                            Console.SetError(originalError)
                                            []
                                 
                                    let result =  
                                        buttons
                                        |> List.mapi 
                                            (fun i button 
                                                -> 
                                                canopy.classic.click button 
                                                Thread.Sleep 2000   
                                                    
                                                let result = scrapeGeneral ()                                       
                                            
                                                match i = buttons.Length - 1 with 
                                                | true 
                                                    ->       
                                                    canopy.classic.waitForElement "[id*='headlessui-menu-item']"
                                                    canopy.classic.click button
                                                    Thread.Sleep 2000   
                                                | false 
                                                    -> 
                                                    ()

                                                canopy.classic.navigate canopy.classic.forward
                                                result 
                                            )
                                        |> List.concat    
                                        |> List.distinct   
                                    
                                    result
                                    
                                let pdfLinkList1 = pdfLinkList () |> List.distinct

                                let pdfLinkList2 = 
                                    Seq.initInfinite (fun _ -> clickCondition())
                                    |> Seq.takeWhile ((=) true) 
                                    |> Seq.collect
                                        (fun _ -> 
                                            try 
                                                canopy.classic.click (canopy.classic.elementWithText "a" "Další")
                                                pdfLinkList ()
                                            with
                                            | _ -> []
                                        )
                                    |> Seq.distinct
                                    |> Seq.toList                  

                                (pdfLinkList1 @ pdfLinkList2) |> List.choose id  

                            with
                            | _ -> []

                        urls 
                        |> List.collect scrapeUrl 
                        |> List.filter (fun item -> not (excludeYears |> List.exists item.Contains))
                    with
                    | _ -> []
                finally
                    canopy.classic.quit()

        let currentLinks () = 

            match startHeadlessEdge () with
            | Error _ 
                -> 
                []
            | Ok _ 
                ->
                try
                    try
                        let linksShown () = 
                            (canopy.classic.elements ".Card_actions__HhB_f").Length >= 1
                    
                        let scrapeUrl (url: string) =
                            try
                                canopy.classic.url url
                    
                                let pdfLinkList () =
                                    Thread.Sleep 15000  
                                    canopy.classic.waitFor linksShown  
                                    scrapeGeneral ()  
                                                
                                let pdfLinkList1 = pdfLinkList () |> List.distinct
                    
                                let pdfLinkList2 = 
                                    Seq.initInfinite (fun _ -> clickCondition())
                                    |> Seq.takeWhile ((=) true) 
                                    |> Seq.collect
                                        (fun _ 
                                            -> 
                                            try 
                                                canopy.classic.click (canopy.classic.elementWithText "a" "Další")
                                                pdfLinkList ()
                                            with
                                            | _ -> []
                                        )
                                    |> Seq.distinct
                                    |> Seq.toList                  
                    
                                (pdfLinkList1 @ pdfLinkList2) |> List.choose id  
                    
                            with
                            | _ -> []

                        urls 
                        |> List.collect scrapeUrl
                        |> List.filter (fun item -> not <| (item.Contains "2022" || item.Contains "2023" || item.Contains "2024"))
                    with
                    | _ -> []

                finally
                    canopy.classic.quit()
        
        try
            printfn "=== Starting changesLinks() ==="
            let list2 = changesLinks () |> List.distinct
            printfn "changesLinks found %d links" list2.Length
            
            printfn "\n=== Starting currentAndFutureLinks() ==="
            let currentFutureList = currentAndFutureLinks () |> List.distinct
            printfn "currentAndFutureLinks found %d links" currentFutureList.Length
            
            printfn "\n=== Starting currentLinks() ==="
            let currentList = currentLinks () |> List.distinct
            printfn "currentLinks found %d links" currentList.Length
            
            let list1 = (currentFutureList @ currentList) |> List.distinct
            let list = list2 @ list1
            
            printfn "\n=== Total unique links: %d ===" list.Length

            let dir = Path.GetDirectoryName path
            
            // create the folder if missing
            match Directory.Exists dir with
            | true  -> ()
            | false -> Directory.CreateDirectory dir |> ignore

            serializeToJsonThoth2 list path
        with
        | ex -> 
            eprintfn "CRITICAL ERROR: %s" ex.Message
            Error <| (sprintf "%s %s" <| string ex.Message <| " Error Canopy 001 combined")   

    type ResponsePut = 
        {
            Message1 : string
            Message2 : string
        }

    let private decoderPut : Decoder<ResponsePut> =
        Decode.object
            (fun get 
                ->
                {
                    Message1 = get.Required.Field "Message1" Decode.string
                    Message2 = get.Required.Field "Message2" Decode.string
                }
            )

    let internal putToRestApiTest () =
            
        let getJsonString path =

            try
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)
    
                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                     
                        use fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                        let! _ = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                        
                        
                        use reader = new StreamReader(fs)
                        let! _ = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath) 
                    
                        let jsonString = reader.ReadToEnd()
                        let! jsonString = jsonString |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                      
                                      
                        return Ok jsonString 
                    }
            with
            | ex -> Error (sprintf "%s %s" <| string ex.Message <| " Error Canopy 002")
    
        async
            {                                                      
                let thothJsonPayload =                    
                    match getJsonString path with
                    | Ok jsonString -> jsonString                                  
                    | Error _       -> String.Empty            
               
                let! response = 
                    http
                        {
                            PUT url
                            header "X-API-KEY" apiKeyTest 
                            body 
                            json thothJsonPayload
                        }
                    |> Request.sendAsync       
                        
                match response.statusCode with
                | HttpStatusCode.OK 
                    -> 
                     let! jsonMsg = Response.toTextAsync response
    
                     return                          
                         Decode.fromString decoderPut jsonMsg   
                         |> function
                             | Ok value 
                                 -> value   
                             | Error err 
                                 -> 
                                { 
                                    Message1 = String.Empty
                                    Message2 = (sprintf "%s %s" <| err <| " Error Canopy 003") 
                                }      
                | _ -> 
                     return 
                        { 
                            Message1 = String.Empty
                            Message2 = sprintf "Request failed with status code %d" (int response.statusCode)
                        }                                           
            } 
        |> Async.Catch 
        |> Async.RunSynchronously
        |> Result.ofChoice    
        |> function
            | Ok value 
                -> value 
            | Error ex
                -> 
                {
                    Message1 = String.Empty
                    Message2 = (sprintf "%s %s" <| string ex.Message <| " Error Canopy 004")
                }