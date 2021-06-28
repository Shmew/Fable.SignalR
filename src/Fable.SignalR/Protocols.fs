namespace Fable.SignalR

open Fable.Core
open Messages
open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never)>]
module Protocol =
    open Fable.SignalR.Shared

    [<RequireQualifiedAccess>]
    module HubRecords =
        type InvocationMessage<'T> = 
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string option
              target: string
              arguments: ResizeArray<'T> 
              streamIds: ResizeArray<string> option }

        [<Erase;RequireQualifiedAccess>]
        module InvocationMessage =
            let inline validate<'T> (msg: InvocationMessage<'T>) =
                if msg.target = "" then
                    failwith "Invalid payload for Invocation message."
                if msg.invocationId.IsSome then
                    if msg.invocationId.Value = "" then
                        failwith "Invalid payload for Invocation message."
                msg

            let inline create<'T> headers invocationId target (args: ResizeArray<'T>) streamIds =
                { ``type`` = MessageType.Invocation
                  headers = headers
                  invocationId = invocationId
                  target = target
                  arguments = args
                  streamIds = streamIds }
                |> validate
                |> unbox<Fable.SignalR.Messages.InvocationMessage<'T>>

        type StreamInvocationMessage<'T> =
            { ``type``: MessageType
              target: string
              headers: Map<string,string> option
              invocationId: string
              arguments: ResizeArray<'T> 
              streamIds: ResizeArray<string> option }

        [<Erase;RequireQualifiedAccess>]
        module StreamInvocationMessage =
            let inline create<'T> headers invocationId target (args: ResizeArray<'T>) streamIds =
                { ``type`` = MessageType.StreamInvocation
                  headers = headers
                  invocationId = invocationId
                  target = target
                  arguments = args
                  streamIds = streamIds }
                |> unbox<Fable.SignalR.Messages.StreamInvocationMessage<'T>>

        type StreamItemMessage<'T> =
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string option
              item: 'T }
              
        [<Erase;RequireQualifiedAccess>]
        module StreamItemMessage =
            let inline validate<'T> (msg: StreamItemMessage<'T>) =
                match msg.invocationId with
                | Some invocationId when invocationId <> "" -> msg
                | _ -> failwith "Invalid payload for StreamItem message."

            let inline create<'T> headers invocationId (item: 'T) =
                { ``type`` = MessageType.StreamItem
                  headers = headers
                  invocationId = invocationId
                  item = item } 
                |> validate
                |> unbox<Fable.SignalR.Messages.StreamItemMessage<'T>>

        type CompletionMessage<'T> =
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string
              error: string option
              result: 'T option }
              
        [<Erase;RequireQualifiedAccess>]
        module CompletionMessage =
            let inline validate<'T> (msg: CompletionMessage<'T>) =
                let fail () = failwith "Invalid payload for Completion message."

                match msg.result, msg.error with
                | Some _, Some _ -> fail()
                | None, Some err -> if err = "" then fail()
                | _ -> if msg.invocationId = "" then fail()

                msg

            let inline create<'T> headers invocationId error (result: 'T option) =
                { ``type`` = MessageType.Completion
                  headers = headers
                  invocationId = invocationId
                  error = error
                  result = result } 
                |> validate
                |> unbox<Fable.SignalR.Messages.CompletionMessage<'T>>

        type PingMessage =
            { ``type``: MessageType }
            
        [<Erase;RequireQualifiedAccess>]
        module PingMessage =
            let inline create () = 
                { ``type`` = MessageType.Ping }
                |> unbox<Fable.SignalR.Messages.PingMessage>

        type CloseMessage =
            { ``type``: MessageType
              error: string option
              allowReconnect: bool option }

        [<Erase;RequireQualifiedAccess>]
        module CloseMessage =
            let inline create error allowReconnect =
                { ``type`` = MessageType.Close
                  error = error
                  allowReconnect = allowReconnect } 
                |> unbox<Fable.SignalR.Messages.CloseMessage>

        type CancelInvocationMessage =
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string option }
              
        [<Erase;RequireQualifiedAccess>]
        module CancelInvocationMessage =
            let inline create headers invocationId =
                { ``type`` = MessageType.CancelInvocation
                  headers = headers
                  invocationId = invocationId } 
                |> unbox<Fable.SignalR.Messages.CancelInvocationMessage>

    [<RequireQualifiedAccess>]
    module Json =
        open Fable.SimpleJson

        type RArray<'T> = System.Collections.Generic.List<'T>

        [<RequireQualifiedAccess>]
        module TextMessageFormat =
            let private recordSeparatorChar = char 0x1e
            let private recordSeparator = unbox<string> recordSeparatorChar

            let write (output: string) =
                sprintf "%s%s" output recordSeparator

            let parse (input: string) =
                if input.EndsWith(recordSeparator) then
                    let arr = input.Split(recordSeparatorChar)
                    arr.[0..(arr.Length-2)]
                else failwith "Message is incomplete."

        type JsonProtocol () =
            member inline _.writeMessage (message: HubMessage<'ClientStreamFromApi,'ClientApi,'ClientApi,'ClientStreamToApi>) =
                TextMessageFormat.write(Json.serialize message)
                |> U2.Case1

            member inline _.processMsg<'ServerApi,'ServerStreamApi> (parsedRaw: Json, msgType: MessageType) =
                match msgType with
                | MessageType.Invocation ->
                    Json.tryConvertFromJsonAs<HubRecords.InvocationMessage<'ServerApi>> parsedRaw
                    |> function
                    | Ok res -> Ok (HubRecords.InvocationMessage.validate res |> unbox<InvocationMessage<'ServerApi>> |> U8.Case1)
                    | Error _ ->
                        Json.tryConvertFromJsonAs<HubRecords.InvocationMessage<InvokeArg<'ServerApi>>> parsedRaw
                        |> Result.map (HubRecords.InvocationMessage.validate 
                                        >> unbox<InvocationMessage<InvokeArg<'ServerApi>>> >> U8.Case2)
                | MessageType.StreamItem -> 
                    Json.tryConvertFromJsonAs<HubRecords.StreamItemMessage<'ServerStreamApi>> parsedRaw
                    |> Result.map (HubRecords.StreamItemMessage.validate >> unbox<StreamItemMessage<'ServerStreamApi>> >> U8.Case3)
                | MessageType.Completion -> 
                    Json.tryConvertFromJsonAs<HubRecords.CompletionMessage<'ServerApi>> parsedRaw
                    |> Result.map (HubRecords.CompletionMessage.validate >> unbox<CompletionMessage<'ServerApi>> >> U8.Case4)
                // This case never happens
                | MessageType.StreamInvocation -> 
                    Json.tryConvertFromJsonAs<HubRecords.StreamInvocationMessage<unit>> parsedRaw
                    |> Result.map (unbox<StreamInvocationMessage<unit>> >> U8.Case5)
                | MessageType.CancelInvocation -> 
                    Json.tryConvertFromJsonAs<HubRecords.CancelInvocationMessage> parsedRaw
                    |> Result.map (unbox<CancelInvocationMessage> >> U8.Case6)
                | MessageType.Ping -> 
                    Json.tryConvertFromJsonAs<HubRecords.PingMessage> parsedRaw
                    |> Result.map (unbox<PingMessage> >> U8.Case7)
                | MessageType.Close -> 
                    Json.tryConvertFromJsonAs<HubRecords.CloseMessage> parsedRaw
                    |> Result.map (unbox<CloseMessage> >> U8.Case8)
                // Shouldn't ever match
                | _ -> failwithf "Invalid message: %A" parsedRaw

            #if FABLE_COMPILER
            member inline this.parseMsgs<'ServerApi,'ServerStreamApi> (input: U2<string,JS.ArrayBuffer>) (logger: ILogger) =
            #else
            member this.parseMsgs<'ServerApi,'ServerStreamApi> (input: U2<string,JS.ArrayBuffer>) (logger: ILogger) =
            #endif
                match input with
                | U2.Case1 "" -> [||]
                | U2.Case1 str ->
                    try
                        TextMessageFormat.parse(str)
                        |> Array.choose(fun m ->
                            let parsedRaw = SimpleJson.parse m
                        
                            SimpleJson.readPath ["type"] parsedRaw
                            |> Option.map Json.convertFromJsonAs<MessageType>
                            |> Option.get
                            |> fun msgType ->
                                this.processMsg<'ServerApi,'ServerStreamApi>(parsedRaw, msgType)
                            |> function
                            | Ok msg -> Some msg
                            | Error e -> 
                                sprintf "Unknown message type: %s" e
                                |> logger.log LogLevel.Error
                                None)
                    with e ->
                        sprintf "An error occured during message deserialization: %s" e.Message
                        |> logger.log LogLevel.Error
                        [||]
                | _ -> 
                    logger.log LogLevel.Error "Invalid input for JSON hub protocol. Expected a string, got an array buffer instead."
                    [||]
                |> RArray

        let inline create<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
            let protocol = JsonProtocol()
            { new IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> with
                member _.name = "json"
                member _.version = 1
                member _.transferFormat = TransferFormat.Text

                member _.parseMessages (input, logger) = protocol.parseMsgs input logger
                member _.writeMessage msg = protocol.writeMessage msg }

    [<RequireQualifiedAccess>]
    module MsgPack =
        open Fable.Remoting.MsgPack
        open Fable.SignalR.Shared.MsgPack

        let [<Literal>] private MaxPayloadSize = 2147483648UL

        [<Erase>]
        module StreamIds =
            let inline toOpt (streamIds: string []) =
                if Array.isEmpty streamIds then None
                else Some streamIds
        
        [<Erase>]
        module String =
            let inline toOpt (s: string) =
                if isNull s || s = "" then None
                else Some s

        [<Erase>]
        module Obj =
            let inline toOpt (o: obj) =
                if isNull o then None
                else Some o

        [<RequireQualifiedAccess;StringEnum(CaseRules.None)>]
        type InvocationTarget =
            | Invoke
            | Send
            | StreamTo

        let parseMsg (msg: Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>) =
            match msg with
            | Msg.Invocation (headers, invocationId, target, args, streamIds) ->
                HubRecords.InvocationMessage.create<'ServerApi> headers invocationId target (ResizeArray args) (unbox<ResizeArray<string> option> streamIds)
                |> U8.Case1
            | Msg.InvocationExplicit (headers, invocationId, target, args, streamIds) ->
                HubRecords.InvocationMessage.create<InvokeArg<'ServerApi>> headers invocationId target (ResizeArray args) (unbox<ResizeArray<string> option> streamIds)
                |> U8.Case2
            | Msg.StreamItem (headers, invocationId, item) ->
                HubRecords.StreamItemMessage.create<'ServerStreamApi> headers invocationId item
                |> U8.Case3
            | Msg.Completion (headers, invocationId, error, result) ->
                HubRecords.CompletionMessage.create<'ServerApi> headers invocationId error result
                |> U8.Case4
            | Msg.CancelInvocation (headers, invocationId) ->
                HubRecords.CancelInvocationMessage.create headers invocationId
                |> U8.Case6
            | Msg.Ping -> 
                HubRecords.PingMessage.create()
                |> U8.Case7
            | Msg.Close (error, allowReconnect) ->
                HubRecords.CloseMessage.create error allowReconnect
                |> unbox<CloseMessage>
                |> U8.Case8
            // Shouldn't ever match
            | Msg.InvokeInvocation (headers, invocationId, target, msg, invokeId, streamIds) ->
                let args = ResizeArray [| box msg; box invokeId |]
                HubRecords.InvocationMessage.create<obj> headers invocationId target args (unbox<ResizeArray<string> option> streamIds)
                |> unbox
                |> U8.Case1
            // Shouldn't ever match
            | Msg.StreamInvocation (headers, invocationId, target, args, streamIds) ->
                HubRecords.StreamInvocationMessage.create<unit> headers invocationId target (ResizeArray args) (unbox<ResizeArray<string> option> streamIds)
                |> U8.Case5

        [<Erase>]
        module List =
            let inline cons xs x = x::xs

        let inline parseMsgs<'ServerApi,'ServerStreamApi> (buffer: JS.ArrayBuffer) : HubMessage<unit,'ServerApi,'ServerApi,'ServerStreamApi> list =
            let reader = Read.Reader(unbox (JS.Constructors.Uint8Array.Create(buffer)))

            let rec read pos xs =
                match (unbox<uint64> (reader.Read (typeof<uint64>))) + pos + 1UL with
                | pos when uint64 buffer.byteLength - pos > 0UL ->
                    reader.Read typeof<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                    |> unbox<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                    |> parseMsg
                    |> List.cons xs 
                    |> read pos
                | _ ->
                    reader.Read typeof<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                    |> unbox<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                    |> parseMsg
                    |> List.cons xs

            read 0UL []
            |> List.rev

        type MsgPackProtocol () =
            member inline _.writeMessage<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (message: HubMessageBase) =
                match message.``type`` with
                | MessageType.Invocation ->
                    let invocation = unbox<InvocationMessage<'ClientApi>> message

                    match invocation.target with
                    | HubMethod.Invoke ->
                        if invocation.arguments.Count = 2 then
                            Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.InvokeInvocation (
                                invocation.headers,
                                invocation.invocationId,
                                invocation.target,
                                invocation.arguments.Item(0),
                                unbox<System.Guid> (invocation.arguments.Item(1)),
                                unbox<string [] option> invocation.streamIds
                            )
                        else
                            let invocation = unbox<InvocationMessage<InvokeArg<'ClientApi>>> message

                            Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.InvocationExplicit (
                                invocation.headers,
                                invocation.invocationId,
                                invocation.target,
                                unbox invocation.arguments,
                                unbox<string [] option> invocation.streamIds
                            )
                    | HubMethod.Send 
                    | HubMethod.StreamTo ->
                        Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Invocation (
                            invocation.headers,
                            invocation.invocationId,
                            invocation.target,
                            unbox invocation.arguments,
                            unbox<string [] option> invocation.streamIds
                        )
                    | _ -> failwithf "Invalid Invocation Target: %s" invocation.target
                | MessageType.StreamItem ->
                    let streamItem = unbox<StreamItemMessage<'ClientStreamToApi>> message
                    
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.StreamItem (
                        streamItem.headers,
                        Some streamItem.invocationId, 
                        streamItem.item
                    )
                | MessageType.Completion ->
                    let completion = unbox<CompletionMessage<unit>> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Completion (
                        completion.headers, 
                        completion.invocationId, 
                        completion.error, 
                        completion.result
                    )
                | MessageType.StreamInvocation ->
                    let streamInvocation = unbox<StreamInvocationMessage<'ClientStreamFromApi>> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.StreamInvocation (
                        streamInvocation.headers, 
                        streamInvocation.invocationId, 
                        streamInvocation.target,
                        unbox streamInvocation.arguments, 
                        unbox<string [] option> streamInvocation.streamIds
                    )
                | MessageType.CancelInvocation ->
                    let cancelInvocation = unbox<CancelInvocationMessage> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.CancelInvocation(cancelInvocation.headers, cancelInvocation.invocationId)
                | MessageType.Ping -> Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Ping
                | MessageType.Close -> 
                    let close = unbox<CloseMessage> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Close(close.error, close.allowReconnect)
                // Shouldn't ever match
                | _ -> failwithf "Invalid message: %A" message
                |> fun msg -> 
                    let outArr = ResizeArray []
                    Write.Fable.writeObject msg typeof<Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>> outArr
                
                    if uint64 outArr.Count > MaxPayloadSize then
                        failwith "Messages over 2GB are not supported."
                    else
                        let msgArr = ResizeArray []
                        Write.Fable.writeObject (uint64 outArr.Count) typeof<uint64> msgArr

                        msgArr.AddRange(outArr)

                        JS.Constructors.Uint8Array.Create(msgArr).buffer
                        |> U2.Case2

            #if FABLE_COMPILER
            member inline _.parseMsgs<'ServerApi,'ServerStreamApi> (buffer: JS.ArrayBuffer) (logger: ILogger) : ResizeArray<HubMessage<unit,'ServerApi,'ServerApi,'ServerStreamApi>> =
            #else
            member _.parseMsgs<'ServerApi,'ServerStreamApi> (buffer: JS.ArrayBuffer) (logger: ILogger) : ResizeArray<HubMessage<unit,'ServerApi,'ServerApi,'ServerStreamApi>> =
            #endif
                try parseMsgs<'ServerApi,'ServerStreamApi> buffer
                with e ->
                    sprintf "An error occured during message deserialization: %s" e.Message
                    |> logger.log LogLevel.Error
                    []
                |> ResizeArray

        let inline create<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
            let protocol = MsgPackProtocol()
            { new IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> with
                member _.name = "messagepack"
                member _.version = 1
                member _.transferFormat = TransferFormat.Binary

                member _.parseMessages (input, logger) = protocol.parseMsgs<'ServerApi,'ServerStreamApi> (unbox input) logger
                member _.writeMessage msg = protocol.writeMessage<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (unbox msg) }
