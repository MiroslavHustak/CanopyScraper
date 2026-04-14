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
    
            use p = Diagnostics.Process.Start psi
            let output = p.StandardOutput.ReadToEnd().Trim()
            p.WaitForExit()   
    
            // Find the token that looks like a version number x.x.x.x
            output.Split(' ')
            |> Array.tryFind (fun s -> s.Split('.').Length >= 3 && s.[0] |> System.Char.IsDigit)
    
        with 
        | ex ->
            printfn "DRIVER EX: %s" <| string ex.Message
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
     
    let private getLatestEdgeDriver () =

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

                    eprintfn "It is necessary to download the latest stable version: %s" cleanVersion

                    let downloadUrl = sprintf "https://msedgedriver.microsoft.com/%s/edgedriver_win64.zip" cleanVersion
                 
                    eprintfn "Downloading from: %s" downloadUrl

                    try
                        File.Delete zipPath
                    with
                    | _ -> ()

                    let downloadRequest =
                        http
                            {
                                GET downloadUrl
                                header "User-Agent" "FsHttp/Windows"
                            }

                    let! response2 =
                        downloadRequest
                        |> Request.sendAsync
                        |> Async.map Ok

                    use response2 = response2

                    match response2.statusCode with
                    | HttpStatusCode.OK
                        ->
                        let! bytes =
                            response2.content.ReadAsByteArrayAsync()
                            |> Async.AwaitTask
                            |> Async.map Ok
                
                        do! 
                            File.WriteAllBytesAsync(zipPath, bytes) 
                            |> Async.AwaitTask
                            |> Async.map Ok

                        eprintfn "Download complete: %s" zipPath

                        killEdgeZombies ()
                        
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
                        eprintfn "******************"                        

                        return ()

                    | status
                        ->
                        eprintf "Download failed with status code: %A" status
                        return ()

                | status
                    ->
                    eprintf "Version request failed with status code: %A" status
                    return ()
            }
        |> AsyncResult.catch (fun ex -> string ex.Message)
        
    let internal ensureDriver () =
        
        //printfn "EdgeVersion %A" <| getEdgeVersion ()
        //printfn "DriverVersion %A" <| getDriverVersion finalPath

        //let driverVersion  = getDriverVersion finalPath |> Option.bind getMajorVersion //major version
        //let browserVersion = getEdgeVersion () |> Option.bind getMajorVersion  //major version

        let driverVersion  = getDriverVersion finalPath //exact version
        let browserVersion = getEdgeVersion () //exact version

        printfn "EdgeVersion %s" (browserVersion |> Option.defaultValue "Unknown")
        printfn "DriverVersion %s" (driverVersion |> Option.defaultValue "Unknown")

        match browserVersion, driverVersion with    
        | Some browser, Some driver
            when browser = driver
            -> Ok ()    
        | _ -> getLatestEdgeDriver () |> Async.RunSynchronously 