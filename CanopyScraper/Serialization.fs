namespace Serialization

open System.IO
open Thoth.Json.Net

open Helpers
open Helpers.Builders

open Serialization.Coders.ThothCoders

// Implement 'try with' block for serialization at each location in the code where it is used.
module Serialisation =
   
    //Thoth.Json.Net, Thoth.Json + StreamWriter (System.IO (File.WriteAllText) did not work)    
    let internal serializeToJsonThoth2 (list : string list) (jsonFile : string) =
         
        pyramidOfDoom
            {
                let filepath = Path.GetFullPath jsonFile |> Option.ofNullEmpty 
                let! filepath = filepath, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při čtení cesty k souboru " jsonFile)
    
                let json = Encode.toString 2 (encoder list) |> Option.ofNullEmpty // Serialize the record to JSON with indentation, 2 = the number of spaces used for indentation in the JSON structure
                let! json = json, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při serializaci do " jsonFile)
    
                use writer = new StreamWriter(filepath, false)                
                let! _ = writer |> Option.ofNull, Error (sprintf "%s%s" "Zadané hodnoty nebyly uloženy, chyba při serializaci do " jsonFile)

                writer.Write json

                return Ok ()
            }