# Push Streaming

Sometimes instead of pulling down new data you want to 
immediately dispatch new messages as something occurs. 
You can use the tried and true `IObservable` to accomplish this:

```fsharp
let subscribe (observable: IObservable<'T>) =
    observable
    |> AsyncSeq.ofObservableBuffered
    |> AsyncSeq.distinctUntilChanged
    |> AsyncSeq.toAsyncEnum
```

You can naturally apply changes in this pipeline at the various 
steps as it moves from `IObservable` -> `AsyncSeq` -> `IAsyncEnumerable`.
