namespace Fable.SignalR

module internal Bindings =
    open Fable.Core
    open Fable.Core.JsInterop

    [<Erase>]
    type SignalR =
        [<Emit("new $0.HubConnectionBuilder()")>]
        member inline _.HubConnectionBuilder () : IHubConnectionBuilder<'ClientApi,'ServerApi> = jsNative
        [<Emit("$0.NullLogger.instance")>]
        member inline _.NullLogger () : NullLogger = jsNative

    let signalR : SignalR = importAll "@microsoft/signalr"
