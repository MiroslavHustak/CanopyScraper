namespace Serialization

open System.IO
open Thoth.Json.Net

open Helpers
open Helpers.Builders
open Helpers.Haskell_IO_Monad_Simulation

open Serialization.Coders.ThothCoders

// Implement 'try with' block for serialization at each location in the code where it is used.
module Serialisation =

    let internal serializeWithThothSync (list : string list) (jsonFile : string) : IO<Result<unit, string>> =
       
        IO (fun ()
                ->
                try      
                    pyramidOfDoom
                        {
                            let! path = SafeFullPath.safeFullPathOption >> runIO <| jsonFile, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při čtení cesty k souboru " jsonFile)                               
                                                               
                            let json = Encode.toString 2 (encoder list) |> Option.ofNullEmpty // Serialize the record to JSON with indentation, 2 = the number of spaces used for indentation in the JSON structure
                            let! json = json, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při serializaci do " jsonFile)
    
                            use writer = new StreamWriter(path, false)  
                            let! _ = writer |> Option.ofNull, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při serializaci do " jsonFile)
                            writer.Write json

                            return Ok ()
                        }
                with
                | ex -> Error <| string ex.Message
        )