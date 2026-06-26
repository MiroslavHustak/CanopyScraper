module ApiKeys

open System
open System.IO
open FsToolkit.ErrorHandling

open Thoth.Json.Net
open Helpers.SafeFullPath
open Helpers.Haskell_IO_Monad_Simulation

type Secrets =
    {
        ApiKey : string
    }

module Secrets =

    let private decoder : Decoder<Secrets> =
        Decode.object
            (fun get
                ->
                {
                    ApiKey = get.Required.Field "ApiKey" Decode.string
                }
            )

    let internal loadApiKey (path : string) : Result<Secrets, string> =
        try            
            match runIO << safeFullPathResult <| path with
            | Ok path
                -> 
                let json = System.IO.File.ReadAllText path
                Decode.fromString decoder json
            | Error err
                ->  
                Error (sprintf "Failed to read secrets file: %s" err)
          
        with
        | ex -> Error (sprintf "Failed to read secrets file: %s" (string ex.Message))

    (*
    Do <ItemGroup>
    pridej
    <Content Update="Secrets\secrets.json">
    	<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>    
    *)