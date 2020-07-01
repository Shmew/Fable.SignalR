namespace SignalRApp

module Stocks =
    open SignalRApp
    open System
    open System.IO

    let appleStocks () =
        let fullData =
            File.ReadAllText("demo/Server/Data/apple.csv").Trim().Split('\n') 
            |> Array.map (fun s -> s.Split(','))

        fullData
        |> Array.tail
        |> Array.fold (fun (state: AppleStock list) (data: string []) -> 
            { Date = data.[0] |> DateTime.Parse
              High = data.[2] |> float
              Low = data.[3] |> float }::state
        ) []
        |> List.rev
