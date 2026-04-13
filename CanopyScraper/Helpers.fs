namespace Helpers

open System.IO
open System.Diagnostics

module InteractiveHelpers =

    let internal isInKubernetes = 
        System.Environment.GetEnvironmentVariable "KUBERNETES_SERVICE_HOST"
        |> Option.ofNullEmptySpace
        |> Option.isSome

    let internal isInContainer = 
        System.Environment.GetEnvironmentVariable "DOTNET_RUNNING_IN_CONTAINER"
        |> Option.ofNullEmptySpace
        |> Option.isSome

module ProcessHelpers =   

    let internal killEdgeZombies () =

        try
            [
                "msedgedriver"
                "msedge"
                "MicrosoftWebDriver"
            ]
            |> List.iter
                (fun name 
                    ->
                    Process.GetProcessesByName name
                    |> Array.iter 
                        (fun p 
                            ->
                            try 
                                p.Kill()
                                p.WaitForExit 3000 |> ignore
                            with 
                            | _ -> ()
                        )
                )
        with 
        | _ -> ()

    let internal killChromeZombies () =
        try
            Process.GetProcessesByName("chrome") 
            |> Array.iter (fun p -> try p.Kill() with _ -> ())
         
            Process.GetProcessesByName("chromedriver")
            |> Array.iter (fun p -> try p.Kill() with _ -> ())
        with 
        | _ -> ()

module Haskell_IO_Monad_Simulation =    
    
    type [<Struct>] internal IO<'a> = IO of (unit -> 'a) // wrapping custom type simulating Haskell's IO Monad (without the monad, of course)

    let internal runIO (IO action) = action () 

module SafeFullPath =

    let internal safeFullPathResult path =

        Haskell_IO_Monad_Simulation.IO 
            (fun ()
                ->
                try
                    Path.GetFullPath path
                    |> Option.ofNullEmpty 
                    |> Option.toResult "Failed getting path"  
                with
                | ex -> Error <| sprintf "Path is invalid: %s" (string ex.Message)
        )

    let internal safeFullPathOption path =

        Haskell_IO_Monad_Simulation.IO
            (fun ()
                ->
                try
                    Path.GetFullPath path
                    |> Option.ofNullEmpty 
                with
                | _ -> None
        )
   
