module BenchMarkScratchpad

open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

module ArrayCreation =
    let append (a1: int []) (a2: int []) = Array.append a1 a2

    let createCopy (a1: int []) (a2: int []) = 
        let arr = Array.CreateInstance(typeof<int>, a1.Length + a2.Length)
        a1.CopyTo(arr, 0)
        a2.CopyTo(arr, a1.Length)

        arr

[<MemoryDiagnoser>]
type BenchClass () =
    [<Params (10,1000,10000)>] 
    member val ArrSize : int = 0 with get, set

    member self.a1 = Array.init self.ArrSize id
    member self.a2 = Array.init self.ArrSize id            

    [<Benchmark>]
    member self.Append () =
        ArrayCreation.append self.a1 self.a2

    [<Benchmark>]
    member self.CreateCopy () =
        ArrayCreation.createCopy self.a1 self.a2

[<EntryPoint>]
let main _ =
    BenchmarkRunner.Run typeof<BenchClass> |> ignore
    0
