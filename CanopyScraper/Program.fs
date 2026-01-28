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
    
    let private safeElements selector =
        try
            canopy.classic.elements selector
        with
        | _ -> []

    let private safeElementWithText tag text =
        try
            Some (canopy.classic.elementWithText tag text)
        with
        | _ -> None

    let internal canopyResult () = 
    
        let urlsChanges = 
            2115 :: [ 2400 .. 2800 ]
            |> List.map (fun item -> sprintf "%s%s" "https://www.kodis.cz/changes/" (string item))

        let scrapeGeneral () = 
            safeElements "a"
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
                match safeElementWithText "a" "Další" with
                | Some nextButton -> nextButton.Displayed && nextButton.Enabled
                | None -> false
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
                            Some (safeElements "ul > li > div" |> Seq.length >= 1)
                  
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
                                | true ->
                                    scrapeGeneral ()
                                    |> List.choose id  
                                    |> List.distinct
                                    |> List.filter 
                                        (fun item -> item.Contains urlKodis)
                                | false -> []
                            with
                            | _ -> []

                        urlsChanges 
                        |> List.collect scrapeUrl
                        |> List.filter (fun item -> not (excludeYears |> List.exists item.Contains))
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
                            (safeElements ".Card_actions__HhB_f").Length >= 1               

                        let scrapeUrl (url : string) =
                            try
                                canopy.classic.url url

                                let pdfLinkList () =
                                    Thread.Sleep 15000            
                                
                                    canopy.classic.waitFor linksShown
                                
                                    let buttons = 
                                        safeElements "button[title='Budoucí jízdní řády']"
                                    
                                    buttons
                                    |> List.mapi (fun i button -> 
                                        canopy.classic.click button
                                        Thread.Sleep 2000   
                                            
                                        let result = scrapeGeneral ()                                       
                                        
                                        match i = buttons.Length - 1 with 
                                        | true ->
                                            safeElementWithText "button" "Budoucí jízdní řády"
                                            |> Option.iter (fun b -> canopy.classic.click b; Thread.Sleep 2000)
                                        | false -> ()

                                        canopy.classic.navigate canopy.classic.forward
                                        result
                                    )
                                    |> List.concat    
                                    |> List.distinct   
                                
                                let pdfLinkList1 = pdfLinkList () |> List.distinct

                                let pdfLinkList2 = 
                                    Seq.initInfinite (fun _ -> clickCondition())
                                    |> Seq.takeWhile ((=) true) 
                                    |> Seq.collect
                                        (fun _ -> 
                                            try 
                                                safeElementWithText "a" "Další"
                                                |> Option.iter canopy.classic.click
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
                            (safeElements ".Card_actions__HhB_f").Length >= 1
                        
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
                                        (fun _ -> 
                                            try 
                                                safeElementWithText "a" "Další"
                                                |> Option.iter canopy.classic.click
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
            
            match Directory.Exists dir with
            | true  -> ()
            | false -> Directory.CreateDirectory dir |> ignore

            serializeToJsonThoth2 list path
        with
        | ex -> 
            eprintfn "CRITICAL ERROR: %s" ex.Message
            Error <| (sprintf "%s %s" <| string ex.Message <| " Error Canopy 001 combined")
