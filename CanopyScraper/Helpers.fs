namespace Helpers

open System.Diagnostics

module ProcessHelpers =   

    let killEdgeZombies () =
        try
            Process.GetProcessesByName("msedge") 
            |> Array.iter (fun p -> try p.Kill() with _ -> ())
         
            Process.GetProcessesByName("msedgedriver")
            |> Array.iter (fun p -> try p.Kill() with _ -> ())
        with 
        | _ -> ()

