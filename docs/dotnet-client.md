# .NET Client

The API (mostly) matches that of the Fable client, either the sections titled
`Native` when just using `Fable.SignalR.DotNet`, and `Elmish` when 
additionally using the `Fable.SignalR.DotNet.Elmish`.

There are a couple differences: 

* You cannot define `OnMessage` at the declaration site of the hub, 
    but rather at the points where you want to listen for a message.

    In most cases this may simply be right after you define the hub, but as it returns
    a `System.IDisposable` you can control when and where you listen for messages.
* Streaming is done via `IAsyncEnumerable` rather than the custom types used in the
    Fable client. [FSharp.Control.AsyncSeq](https://github.com/fsprojects/FSharp.Control.AsyncSeq) 
    is highly recommended for handling these.
* Instance methods are uppercase rather than lowercase.
* You cannot use generic types when using `SignalR.Connect`. If something is unused (like streaming), specify `unit` rather than an underscore.
