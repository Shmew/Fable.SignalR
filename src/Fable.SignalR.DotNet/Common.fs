namespace Fable.SignalR

open System
open System.Threading.Tasks

[<RequireQualifiedAccess>]
module internal HubMethod =
    let [<Literal>] Invoke = "Invoke"
    let [<Literal>] Send = "Send"
    let [<Literal>] StreamFrom = "StreamFrom"
    let [<Literal>] StreamTo = "StreamTo"

[<AutoOpen>]
module internal Util =
    let genTask (t: Task<_>) : Task = upcast t

    let wrapEvent (f: 'T option -> Async<unit>) =
        System.Func<'T,Task> (
            Option.ofObj
            >> f
            >> Async.StartAsTask
            >> genTask
        )
