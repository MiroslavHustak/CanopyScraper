namespace Drivers

open System
open System.IO
open System.Net
open System.IO.Compression

open Microsoft.Win32

open FsHttp
open FsToolkit.ErrorHandling

open Settings.SettingsEdgeDriver

open Helpers
open Helpers.ProcessHelpers

module EdgeDriver = 

    let private getDriverVersion path =

        try
            let psi = Diagnostics.ProcessStartInfo() //psi process start info   
            psi.FileName <- path
            psi.Arguments <- "--version"
            psi.RedirectStandardOutput <- true
            psi.UseShellExecute <- false
            
            let fInfo = FileInfo path

            match fInfo.Exists with  //potential TOCTOU caught in try with block
            | false  
                ->
                printfn "Driver not found, automatic driver download and installation activated"
                None
            | true 
                -> 
                use p = Diagnostics.Process.Start psi
                let output = p.StandardOutput.ReadToEnd().Trim()
                p.WaitForExit()   
    
                // Find the token that looks like a version number x.x.x.x
                output.Split(' ')
                |> Array.tryFind (fun s -> s.Split('.').Length >= 3 && s.[0] |> System.Char.IsDigit)
    
        with 
        | _ ->
            printfn "Driver not found, automatic driver download and installation activated"
            None
      
    let private getEdgeVersion () =       
        
        [
            fun () 
                ->
                use hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                hive.OpenSubKey subKeyPath
            fun ()
                ->
                use hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)
                hive.OpenSubKey subKeyPath
            // HKLM fallbacks in case it differs on another machine
            fun () 
                ->
                use hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                hive.OpenSubKey subKeyPath
            fun () 
                ->
                use hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                hive.OpenSubKey subKeyPath
        ] 
        |> List.tryPick
            (fun openKey 
                ->
                try                    
                    openKey() 
                    |> Option.ofNull'
                    |> Option.bind
                        (fun k 
                            ->
                            use k = k
                            k.GetValue "version"
                            |> Option.ofNullEmptySpace
                        )                  
                with
                | _ -> None
            )
 
    let private getMajorVersion (v: string) =  //not used yet

        v.Trim().Split('.')
        |> Array.tryHead
        |> Option.bind (fun s -> match Int32.TryParse s with true, n -> Some n | _ -> None)   


    let private tryDownload (version: string) =

        asyncResult
            {
                let downloadUrl = sprintf "https://msedgedriver.microsoft.com/%s/edgedriver_win64.zip" version
                eprintfn "Downloading from: %s" downloadUrl

                let downloadRequest =
                    http
                        {
                            GET downloadUrl
                            header "User-Agent" "FsHttp/Windows"
                        }

                let! response =
                    downloadRequest
                    |> Request.sendAsync
                    |> Async.map Ok

                use response = response

                match response.statusCode with
                | HttpStatusCode.OK
                    ->
                    let! bytes =
                        response.content.ReadAsByteArrayAsync()
                        |> Async.AwaitTask
                        |> Async.map Ok

                    return Some bytes

                | status
                    ->
                    eprintfn "Download for %s failed with status code: %A" version status
                    return None
            }

    let private tryManualDownload () =  //last resort, ask the user to paste a link 

        asyncResult
            {
                eprintfn "%s" <| String.replicate 50 "="
                eprintfn "Automatic driver download failed for all known versions."
                eprintfn "Paste a direct edgedriver_win64.zip URL, or just a version"
                eprintfn "number (e.g. 150.0.4078.48), or press Enter to give up:"
                
                let input = 
                    Console.ReadLine()
                    |> Option.ofNullEmptySpace
                    |> Option.map (fun s -> s.Trim())

                match input with
                | None 
                    ->
                    return None
                | Some entry
                    ->
                    let manualUrl =
                        match entry.StartsWith("http", StringComparison.OrdinalIgnoreCase) with
                        | true  -> entry
                        | false -> sprintf "https://msedgedriver.microsoft.com/%s/edgedriver_win64.zip" entry

                    eprintfn "Trying manually entered link: %s" manualUrl

                    let downloadRequest =
                        http
                            {
                                GET manualUrl
                                header "User-Agent" "FsHttp/Windows"
                            }

                    let! response =
                        downloadRequest
                        |> Request.sendAsync
                        |> Async.map Ok

                    use response = response

                    match response.statusCode with
                    | HttpStatusCode.OK
                        ->
                        let! bytes =
                            response.content.ReadAsByteArrayAsync()
                            |> Async.AwaitTask
                            |> Async.map Ok

                        return Some bytes

                    | status
                        ->
                        eprintfn "Manual link also failed with status code: %A" status
                        return None
            }

    let private getLatestEdgeDriver (browserVersionFallback: string option) =

        asyncResult
            {                
                let versionRequest =
                    http
                        {
                            GET versionUri
                            header "User-Agent" "FsHttp/Windows"
                        }

                let! response = 
                    versionRequest
                    |> Request.sendAsync
                    |> Async.map Ok

                use response = response

                match response.statusCode with
                | HttpStatusCode.OK
                    ->
                    let! version =
                        response.content.ReadAsStringAsync()
                        |> Async.AwaitTask 
                        |> Async.map Ok

                    let cleanVersion = version.Trim()
                    eprintfn "The latest stable version reported: %s" cleanVersion

                    try
                        File.Delete zipPath
                    with
                    | _ -> ()

                    // Tier 1: the version reported by Microsoft's feed
                    let! bytesOpt = tryDownload cleanVersion

                    // Tier 2: fall back to the exact installed browser version
                    let! bytesOpt =
                        match bytesOpt, browserVersionFallback with
                        | Some _, _ 
                            -> 
                            AsyncResult.ok bytesOpt
                        | None, Some fallback when fallback <> cleanVersion
                            ->
                            eprintfn "Falling back to installed browser version: %s" fallback
                            tryDownload fallback
                        | None, _ 
                            -> 
                            AsyncResult.ok None

                    
                    //let! bytesOpt = AsyncResult.ok None  //for simulating a failure to test the manual download path

                    // Tier 3: last resort — ask the user to paste a link/version                    
                    let! bytesOpt =
                        match bytesOpt with
                        | Some _ -> AsyncResult.ok bytesOpt
                        | None   -> tryManualDownload ()

                    match bytesOpt with
                    | Some bytes
                        ->
                        do! 
                            File.WriteAllBytesAsync(zipPath, bytes) 
                            |> Async.AwaitTask
                            |> Async.map Ok

                        eprintfn "Download complete: %s" zipPath
                        
                        try
                            Directory.Delete(extractPath, true)
                        with
                        | _ -> ()
                        
                        try
                            File.Delete finalPath
                        with
                        | _ -> ()

                        Directory.CreateDirectory extractPath |> ignore<DirectoryInfo>
                        eprintfn "Extracting to: %s" extractPath
                        ZipFile.ExtractToDirectory(zipPath, extractPath)

                        try
                            File.Delete zipPath
                        with
                        | _ -> ()

                        let! exeFile =
                            Directory.GetFiles(extractPath, "*.exe", SearchOption.AllDirectories)
                            |> Array.tryFind
                                (fun f -> Path.GetFileName(f).ToLowerInvariant().Contains("msedgedriver"))
                            |> Option.toResult (sprintf "Could not find msedgedriver.exe in extracted archive")
                        
                        File.Move(exeFile, finalPath, true)
                        eprintfn "Driver moved to: %s" finalPath
                        Directory.Delete(extractPath, true)
                        eprintfn "Done! Driver is located at: %s" finalPath
                        eprintfn "%s" <| String.replicate 50 "="                        

                        return killEdgeZombies ()

                    | None
                        ->
                        eprintfn "Could not download a matching driver — gave up after manual attempt."
                        return ()

                | status
                    ->
                    eprintf "Version request failed with status code: %A" status
                    return ()
            }
        |> AsyncResult.catch (fun ex -> sprintf "%s %s" (string ex.Message) "#EdgeDriverError")

    let internal ensureDriver () =
        
        let driverVersion  = getDriverVersion finalPath
        let browserVersion = getEdgeVersion ()

        printfn "EdgeVersion: %s" (browserVersion |> Option.defaultValue "Unknown")
        printfn "DriverVersion: %s" (driverVersion |> Option.defaultValue "Unknown")

        //getLatestEdgeDriver browserVersion |> Async.RunSynchronously //for simulating a failure to test the manual download path, comment out the match below and uncomment this line
                
        match browserVersion, driverVersion with    
        | Some browser, Some driver
            when browser = driver
            -> Ok ()   
        | None, _ 
            -> Error "Could not determine Edge browser version, ensure first that Edge is installed on this PC, then run this app again."    
        | _ -> getLatestEdgeDriver browserVersion |> Async.RunSynchronously