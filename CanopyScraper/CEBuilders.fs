namespace Helpers

module Builders =
        
    type internal MyBuilder = MyBuilder with //This CE is a monad-style control-flow helper, not a monad
        member _.Recover(m : bool * (unit -> 'a), nextFunc : unit -> 'a) : 'a =
            match m with
            | (false, handleFalse)
                -> handleFalse()
            | (true, _)
                -> nextFunc()    
        member this.Bind(m, f) = this.Recover(m, f) //an alias to prevent confusion      
        member _.Return x : 'a = x   
        member _.ReturnFrom x : 'a = x 
        member _.Using(x : 'a, _body: 'a -> 'b) : 'b = _body x    
        member _.Delay(f : unit -> 'a) = f()
        member _.Zero() = ()    
      
    let internal pyramidOfHell = MyBuilder

    //**************************************************************************************
   
    type Builder2 = Builder2 with    // This CE is a monad-style control-flow helper, not a lawful monad
        member _.Recover((m, recovery), nextFunc) =
            match m with
            | Some v -> nextFunc v
            | None   -> recovery    
        member this.Bind(m, f) = this.Recover(m, f) //an alias to prevent confusion        
        member _.Return x : 'a = x   
        member _.ReturnFrom x : 'a = x
        member _.Using(resource, binder) =
            use r = resource
            binder r
        
    let internal pyramidOfDoom = Builder2
    
    //**************************************************************************************
       
    type internal MyBuilder3 = MyBuilder3 with  // This CE is a monad-style control-flow helper, not a lawful monad
        member _.Recover(m, nextFunc) = 
            match m with
            | (Ok v, _)           
                -> nextFunc v 
            | (Error err, handler) 
                -> handler err
        member this.Bind(m, f) = this.Recover(m, f) //an alias to prevent confusion        
        member _.Zero () = ()       
        member _.Return x = x
        member _.ReturnFrom x = x     
        
    let internal pyramidOfInferno = MyBuilder3  

    //**************************************************************************************

    type internal MyBuilder5 = MyBuilder5 with   // This CE is a monad-style control-flow helper, not a lawful monad
         member _.Recover(m : bool * 'a, nextFunc : unit -> 'a) : 'a =
             match m with
             | (false, value)
                 -> value
             | (true, _)
                 -> nextFunc() 
         member this.Bind(m, f) = this.Recover(m, f) //an alias to prevent confusion              
         member _.Return x : 'a = x   
         member _.ReturnFrom x : 'a = x 
         member _.Using(x : 'a, _body: 'a -> 'b) : 'b = _body x    
         member _.Delay(f : unit -> 'a) = f()
         member _.Zero() = ()    

    let internal pyramidOfDamnation = MyBuilder5

    //**************************************************************************************
    type internal OptionAdaptedBuilder = OptionAdaptedBuilder with
        member _.Bind(m, nextFunc) =
            match m with
            | Some v -> nextFunc v
            | None   -> None    
        member _.Return x : 'a = x   
        member _.ReturnFrom x : 'a = x
        member _.Using(resource, binder) =
            use r = resource
            binder r
    
    let internal option2 = OptionAdaptedBuilder