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

    let internal loadApiKey2 (path : string) : Result<Secrets, string> =
        try
            let fullPath = runIO << safeFullPathResult <| path
            match fullPath with
            | Ok path 
                ->
                let json = System.IO.File.ReadAllText path// @"e:\source\repos\CanopyScraper\CanopyScraper\Secrets\secrets.json"
                Decode.fromString decoder json
                | Error err ->  Error (sprintf "Failed to read secrets file: %s" err)
        with
        | ex -> Error (sprintf "Failed to read secrets file: %s" (string ex.Message))

    let internal loadApiKey (path : string) : Result<Secrets, string> =
        try
            let fullPath = Path.Combine(AppContext.BaseDirectory, path) //AppContext.BaseDirectory always points to where your compiled app lives, regardless of what the process working directory happens to be
            let json = System.IO.File.ReadAllText fullPath
            Decode.fromString decoder json
        with
        | ex -> Error (sprintf "Failed to read secrets file: %s" (string ex.Message))

    (*
    Do <ItemGroup>
    pridej
   	<Content Include="Secrets\secrets.json">
   		<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   		<Link>Secrets\secrets.json</Link>
   	</Content>  
    *)