namespace SignalRApp

module SignalRHub =
    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int
        | RandomCharacter

    [<RequireQualifiedAccess>]
    type Response =
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
    let [<Literal>] Root = "/SignalR"
    let [<Literal>] Root2 = "/SignalR2"
    let [<Literal>] RootAkka = "/SignalRAkka"
    let [<Literal>] RootAkka2 = "/SignalRAkka2"
