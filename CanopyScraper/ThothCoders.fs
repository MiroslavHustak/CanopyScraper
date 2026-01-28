(*
    Code in this file uses Thoth.Json.Net by Maxime Mangel.
    Copyright (c) [2019] [Mangel Maxime]
    Licensed under the MIT License
    https://thoth-org.github.io/Thoth.Json/
*)

namespace Serialization.Coders

open Thoth.Json.Net

module ThothCoders =

    let internal encoder list = Encode.object [ "list", list |> List.map Encode.string |> Encode.list ]

    //Decode.field by melo byt OK, nemusi byt Decode.object
    let internal decoder : Decoder<string list> = Decode.field "list" (Decode.list Decode.string)