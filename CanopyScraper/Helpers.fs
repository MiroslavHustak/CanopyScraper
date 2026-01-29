namespace Helpers

open System.IO
open System.Diagnostics

module ProcessHelpers =   

    let internal killEdgeZombies () =
        try
            Process.GetProcessesByName("msedge") 
            |> Array.iter (fun p -> try p.Kill() with _ -> ())
         
            Process.GetProcessesByName("msedgedriver")
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
   
