namespace SignalRApp

module SignalRHub =
    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int
        | RandomCharacter
        | SayHello
        | GetInts

    [<RequireQualifiedAccess>]
    type Response =
        | Howdy
        | NewCount of int
        | RandomCharacter of string
        | GetInts of int

    module Stream =
        [<RequireQualifiedAccess>]
        type Action =
            | GenInts
        
        [<RequireQualifiedAccess>]
        type Response =
            | GetInts of int

module Endpoints =
    let port = 8080us

    let baseUrl = sprintf "http://localhost:%i" port
    
    let [<Literal>] Root = "/SignalR"
