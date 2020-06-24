namespace SignalRApp

module SignalRHub =
    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int
        | RandomCharacter
        | SayHello

    [<RequireQualifiedAccess>]
    type Response =
        | Howdy
        | NewCount of int
        | RandomCharacter of string

    module StreamFrom =
        [<RequireQualifiedAccess>]
        type Action =
            | GenInts
        
        [<RequireQualifiedAccess>]
        type Response =
            | GetInts of int

    module StreamTo =
        [<RequireQualifiedAccess>]
        type Action =
            | GiveInt of int

module Endpoints =
    let port = 8080us

    let baseUrl = sprintf "http://localhost:%i" port
    
    let [<Literal>] Root = "/SignalR"
