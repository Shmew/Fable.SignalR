namespace Fable.SignalR

module internal Bindings =
    open Fable.Core
    open Fable.Core.JsInterop

    [<Erase>]
    type SignalR =
        [<Emit("new $0.HubConnectionBuilder()")>]
        member _.HubConnectionBuilder () : IHubConnectionBuilder<'ClientApi,'ServerApi> = jsNative

        [<Emit("$0.NullLogger.instance")>]
        member _.NullLogger () : NullLogger = jsNative

        [<Emit("$0.Subject()")>]
        member _.Subject<'T> () : Subject<'T> = jsNative

    let signalR : SignalR = importAll "@microsoft/signalr"
