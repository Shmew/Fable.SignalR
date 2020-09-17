namespace SignalRApp

open System

type AppleStock =
    { Date: DateTime
      High: float
      Low: float }

module SignalRHub =
    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int

    [<RequireQualifiedAccess>]
    type Response =
        | NewCount of int
        | TickerCount of int

    module StreamFrom =
        [<RequireQualifiedAccess>]
        type Action =
            | AppleStocks
        
        [<RequireQualifiedAccess>]
        type Response =
            | AppleStock of AppleStock

module Endpoints =   
    let [<Literal>] Root = "/SignalR"
