namespace SignalRApp

module Stocks =
    open FSharp.Control
    open SignalRApp
    open System
    open System.IO

    let appleStocks =
        asyncSeq {
            let fullData = File.ReadLines(__SOURCE_DIRECTORY__ + "/Data/apple.csv")

            for line in fullData do
                let line = line.Split(',')

                yield
                    { Date = line.[0] |> DateTime.Parse
                      High = float line.[2]
                      Low = float line.[3] }
        }
