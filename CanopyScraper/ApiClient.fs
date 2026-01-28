namespace MyCanopy

open System
open System.IO
open System.Net

open FsHttp
open Thoth.Json.Net

open Helpers
open Helpers.Builders

open Settings.Settings

module ApiClient = 

    type internal ResponsePut = 
        {
            Message1 : string
            Message2 : string
        }

    let private decoderPut : Decoder<ResponsePut> =
        Decode.object
            (fun get 
                ->
                {
                    Message1 = get.Required.Field "Message1" Decode.string
                    Message2 = get.Required.Field "Message2" Decode.string
                }
            )

    let internal putToRestApiTest () =
            
        let getJsonString path =

            try
                pyramidOfDoom
                    {
                        let filepath = Path.GetFullPath path |> Option.ofNullEmpty 
                        let! filepath = filepath, Error (sprintf "%s%s" "Chyba při čtení cesty k souboru " path)
    
                        let fInfodat : FileInfo = FileInfo filepath
                        let! _ = fInfodat.Exists |> Option.ofBool, Error (sprintf "Soubor %s nenalezen" path) 
                     
                        use fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None) 
                        let! _ = fs |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                        
                        
                        use reader = new StreamReader(fs)
                        let! _ = reader |> Option.ofNull, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath) 
                    
                        let jsonString = reader.ReadToEnd()
                        let! jsonString = jsonString |> Option.ofNullEmpty, Error (sprintf "%s%s" "Chyba při čtení dat ze souboru " filepath)                      
                                      
                        return Ok jsonString 
                    }
            with
            | ex -> Error (sprintf "%s %s" <| string ex.Message <| " Error Canopy 002")
    
        async
            {                                                      
                let thothJsonPayload =                    
                    match getJsonString path with
                    | Ok jsonString -> jsonString                                  
                    | Error _       -> String.Empty            
               
                let! response = 
                    http
                        {
                            PUT url
                            header "X-API-KEY" apiKeyTest 
                            body 
                            json thothJsonPayload
                        }
                    |> Request.sendAsync       
                        
                match response.statusCode with
                | HttpStatusCode.OK 
                    -> 
                     let! jsonMsg = Response.toTextAsync response
    
                     return                          
                         Decode.fromString decoderPut jsonMsg   
                         |> function
                             | Ok value 
                                 -> value   
                             | Error err 
                                 -> 
                                { 
                                    Message1 = String.Empty
                                    Message2 = (sprintf "%s %s" <| err <| " Error Canopy 003") 
                                }      
                | _ -> 
                     return 
                        { 
                            Message1 = String.Empty
                            Message2 = sprintf "Request failed with status code %d" (int response.statusCode)
                        }                                           
            } 
        |> Async.Catch 
        |> Async.RunSynchronously
        |> Result.ofChoice    
        |> function
            | Ok value 
                -> value 
            | Error ex
                -> 
                {
                    Message1 = String.Empty
                    Message2 = (sprintf "%s %s" <| string ex.Message <| " Error Canopy 004")
                }