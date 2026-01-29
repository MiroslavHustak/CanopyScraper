namespace Helpers

open System
            
module Result =    
          
    //Applicative functor      
    let inline internal sequence aListOfResults = //gets the first error - see the book Domain Modelling Made Functional
        let prepend firstR restR =
            match firstR, restR with
            | Ok first, Ok rest   -> Ok (first :: rest) | Error err1, Ok _ -> Error err1
            | Ok _, Error err2    -> Error err2
            | Error err1, Error _ -> Error err1

        let initialValue = Ok [] 
        List.foldBack prepend aListOfResults initialValue  

    let internal fromOption = 
        function   
        | Some value -> Ok value
        | None       -> Error String.Empty  

    let internal toOption = 
        function   
        | Ok value -> Some value 
        | Error _  -> None  

    let inline internal fromBool ok err =                               
        function   
        | true  -> Ok ok  
        | false -> Error err

    let internal toBool =                               
        function   
        | Ok _    -> true  
        | Error _ -> false

    let inline internal ofChoice (c: Choice<'T,'E>) : Result<'T,'E> =
        match c with
        | Choice1Of2 v -> Ok v
        | Choice2Of2 e -> Error e