namespace Fable.SignalR.Shared

type InvokeArg<'ServerApi> =
    { connectionId: string
      invocationId: System.Guid
      message: 'ServerApi }

module MsgPack =
    let [<Literal>] MaxLengthPrefixSize = 5

    [<RequireQualifiedAccess>]
    type Msg<'StreamInvocation,'Args,'Completion,'StreamItem> =
        | Invocation of 
            headers:Map<string,string> option * 
            invocationId:string option * 
            target:string * 
            args:'Args [] * 
            streamIds:string [] option
        | InvokeInvocation of
            headers:Map<string,string> option * 
            invocationId:string option * 
            target:string * 
            msg:'Args *
            invokeId: System.Guid *
            streamIds:string [] option
        | InvocationExplicit of 
            headers:Map<string,string> option * 
            invocationId:string option * 
            target:string *
            args:InvokeArg<'Args> [] * 
            streamIds:string [] option
        | StreamItem of 
            headers:Map<string,string> option * 
            invocationId:string option * 
            item:'StreamItem
        | Completion of 
            headers:Map<string,string> option * 
            invocationId:string * 
            errMsg:string option *
            // Should always be 'ServerApi
            result:'Completion option
        | StreamInvocation of 
            headers:Map<string,string> option * 
            invocationId:string * 
            target:string *
            // Should always be 'ClientStreamFromApi
            args:'StreamInvocation [] * 
            streamIds:string [] option
        | CancelInvocation of 
            headers:Map<string,string> option * 
            invocationId:string option
        | Ping
        | Close of 
            errMsg:string option * 
            allowReconnect:bool option

[<RequireQualifiedAccess>]
module HubMethod =
    let [<Literal>] Invoke = "Invoke"
    let [<Literal>] Send = "Send"
    let [<Literal>] StreamFrom = "StreamFrom"
    let [<Literal>] StreamTo = "StreamTo"
