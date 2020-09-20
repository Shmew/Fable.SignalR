module BenchMarkScratchpad

open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

module IDictionary =
    let mutable cache = Map.empty<int,Map<string,string>>

    let toMap iDict =
        iDict
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq

    let toMapCached (iDict: Collections.Generic.IDictionary<string,string>) =
        let hashCode = iDict.GetHashCode()
        match cache.TryFind hashCode with
        | Some v -> v
        | None -> 
            let v = toMap iDict
            cache <- cache.Add(hashCode, v)
            v

[<MemoryDiagnoser>]
type BenchClass () =
    [<Params (10,1000,10000)>] 
    member val ListSize : int = 0 with get, set

    member self.dict = 
        List.init self.ListSize (fun i -> string i, string i)
        |> Map.ofList
        |> fun map -> map :> Collections.Generic.IDictionary<string,string>

    [<Benchmark>]
    member self.ToMap () =
        IDictionary.toMap self.dict |> ignore
        IDictionary.toMap self.dict

    [<Benchmark>]
    member self.ToMapOnce () =
        IDictionary.toMap self.dict

    [<Benchmark>]
    member self.ToMapCachedOnce () =
        IDictionary.toMapCached self.dict

    [<Benchmark>]
    member self.ToMapCached () =
        IDictionary.toMapCached self.dict |> ignore
        IDictionary.toMapCached self.dict

[<EntryPoint>]
let main _ =
    BenchmarkRunner.Run typeof<BenchClass> |> ignore
    0
