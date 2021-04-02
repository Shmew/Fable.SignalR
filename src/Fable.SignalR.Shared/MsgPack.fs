namespace Fable.SignalR.Shared

open Fable.Remoting.MsgPack
open Microsoft.AspNetCore.SignalR
open Microsoft.AspNetCore.SignalR.Protocol
open Microsoft.AspNetCore.Connections
open System
open System.IO
open System.Buffers

[<RequireQualifiedAccess>]
module MsgPackProtocol =
    open Fable.SignalR.Shared
    open Fable.SignalR.Shared.MsgPack

    let [<Literal>] private ProtocolName = "messagepack"
    let [<Literal>] private ProtocolVersion = 1
    let [<Literal>] private MaxPayloadSize = 2147483648L

    [<AutoOpen>]
    module private MsgHelpers =
        open MemoryCache

        [<RequireQualifiedAccess>]
        module Serializer =
            module Server =
                let private make<'ServerApi,'ServerStreamApi> () = Write.makeSerializer<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>()

                let get<'ServerApi,'ServerStreamApi> () = memoize make<'ServerApi,'ServerStreamApi> ()

            module Client =
                let private make<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> () = Write.makeSerializer<Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>>()

                let get<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> () = memoize make<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> ()
                
        [<RequireQualifiedAccess>]
        module Headers =
            let private toOptInner (iDict: Collections.Generic.IDictionary<_,_>) =
                match iDict with
                | null -> None
                | _ when iDict.Count > 0 ->
                    iDict
                    |> Seq.map (|KeyValue|)
                    |> Map.ofSeq
                    |> Some
                | _ -> None

            let toOpt (iDict: Collections.Generic.IDictionary<_,_>) = memoize toOptInner iDict

            let inline set (headers: Map<string,string> option) (msg: #HubInvocationMessage) =
                if headers.IsSome then
                     msg.Headers <- headers.Value :> Collections.Generic.IDictionary<string,string>

                msg

        [<RequireQualifiedAccess>]
        module StreamIds =
            let toOpt (streamIds: string []) =
                match streamIds with
                | null -> None
                | _ when Array.isEmpty streamIds -> None
                | _ -> Some streamIds

        [<RequireQualifiedAccess>]
        module String =
            let toOpt (s: string) =
                if String.IsNullOrEmpty s then None
                else Some s
                
        [<RequireQualifiedAccess>]
        module Obj =
            let inline toOptAs<'T> (o: obj) =
                match o with
                | :? 'T as o -> Some o
                | null -> None
                | _ ->
                    o.GetType().FullName
                    |> sprintf "Unsupported message type: %s"
                    |> InvalidOperationException
                    |> raise
                    
        [<RequireQualifiedAccess>]
        module Array =
            let inline elementsAs<'T> (o: obj []) =
                o |> Array.choose Obj.toOptAs<'T>

            let getMessageBytesWithPrependedLength (ms: MemoryStream) =
                if ms.Length > MaxPayloadSize then
                    sprintf "Writing messages above 2GB is not supported."
                    |> InvalidOperationException
                    |> raise
            
                use msgMs = new MemoryStream()
                Write.writeUInt64 (uint64 ms.Length) msgMs
                
                let out = Array.zeroCreate (ms.Length + msgMs.Length |> int)
                Array.Copy (msgMs.GetBuffer (), 0L, out, 0L, msgMs.Length)
                Array.Copy (ms.GetBuffer (), 0L, out, msgMs.Length, ms.Length)
                out

    [<RequireQualifiedAccess>]
    module private Server =
        module Read =
            let inline boxArr xs = xs |> Array.map box

            let message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (input: byref<ReadOnlySequence<byte>>) (message: byref<HubMessage>) =
                if input.Length = 0L then
                    message <- null
                    false
                else
                    try
                        let reader = Read.Reader(input.ToArray())
                        let len = reader.Read typeof<uint32> |> unbox<uint32> |> int64

                        input <- input.Slice(len + 1L)

                        if len > MaxPayloadSize then
                            message <- null
                            false
                        else
                            message <-
                                reader.Read typeof<Msg<'ClientStreamFromApi,'ClientApi,'ClientApi,'ClientStreamToApi>>
                                |> unbox<Msg<'ClientStreamFromApi,'ClientApi,'ClientApi,'ClientStreamToApi>>
                                |> function
                                | Msg.Invocation (headers, invocationId, target, args, streamIds) ->
                                    match invocationId with
                                    | Some invocationId ->
                                        match streamIds with
                                        | Some streamIds -> InvocationMessage(invocationId, target, boxArr args, streamIds)
                                        | None -> InvocationMessage(invocationId, target, boxArr args)
                                    | None -> InvocationMessage(target, boxArr args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.InvokeInvocation (headers, invocationId, target, msg, invokeId, streamIds) ->
                                    let args = [| box msg; box invokeId |]
                        
                                    match invocationId with
                                    | Some invocationId -> 
                                        match streamIds with
                                        | Some streamIds -> InvocationMessage(invocationId, target, args, streamIds)
                                        | None -> InvocationMessage(invocationId, target, args)
                                    | None -> InvocationMessage(target, args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.InvocationExplicit (headers, invocationId, target, args, streamIds) ->
                                    match invocationId with
                                    | Some invocationId ->
                                        match streamIds with
                                        | Some streamIds -> InvocationMessage(invocationId, target, boxArr args, streamIds)
                                        | None -> InvocationMessage(invocationId, target, boxArr args)
                                    | None -> InvocationMessage(target, boxArr args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.StreamItem (headers, invocationId, item) ->
                                    StreamItemMessage(invocationId.Value, item)
                                    |> Headers.set headers :> HubMessage
                                | Msg.Completion (headers, invocationId, error, result) ->
                                    CompletionMessage(invocationId, Option.defaultValue null error, Option.defaultValue Unchecked.defaultof<'ClientApi> result, result.IsSome)
                                    |> Headers.set headers :> HubMessage
                                | Msg.StreamInvocation (headers, invocationId, target, args, streamIds) ->
                                    match streamIds with
                                    | Some streamIds -> StreamInvocationMessage(invocationId, target, boxArr args, streamIds)
                                    | None -> StreamInvocationMessage(invocationId, target, unbox args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.CancelInvocation (headers, invocationId) ->
                                    CancelInvocationMessage(invocationId.Value)
                                    |> Headers.set headers :> HubMessage
                                | Msg.Ping -> PingMessage.Instance :> HubMessage
                                | Msg.Close (error, allowReconnect) ->
                                    let error = Option.defaultValue null error

                                    match allowReconnect with
                                    | Some allowReconnect -> CloseMessage(error, allowReconnect) :> HubMessage
                                    | None -> CloseMessage(error) :> HubMessage
                            true
                    with _ ->
                        message <- Unchecked.defaultof<HubMessage>
                        true

        module Write =
            let message<'ServerApi,'ServerStreamApi> (message: HubMessage) =
                use ms = new MemoryStream()

                let serializer = Serializer.Server.get<'ServerApi,'ServerStreamApi>()
                let serialize msg = serializer.Invoke(msg, ms)

                match message with
                | :? InvocationMessage as msg ->
                    match Array.head msg.Arguments with
                    | :? InvokeArg<'ServerApi> ->
                        Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.InvocationExplicit (
                            Headers.toOpt msg.Headers, 
                            String.toOpt msg.InvocationId, 
                            msg.Target, 
                            Array.elementsAs msg.Arguments, 
                            StreamIds.toOpt msg.StreamIds
                        )
                    | :? 'ServerApi ->
                        Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.Invocation (
                            Headers.toOpt msg.Headers, 
                            String.toOpt msg.InvocationId, 
                            msg.Target, 
                            Array.elementsAs msg.Arguments, 
                            StreamIds.toOpt msg.StreamIds
                        )
                    | arg ->
                        sprintf "Unsupported invocation argument: %A%sFor message: %A"
                            arg
                            System.Environment.NewLine
                            message
                        |> InvalidOperationException
                        |> raise
                | :? StreamItemMessage as msg ->
                    Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.StreamItem (
                        Headers.toOpt msg.Headers, 
                        String.toOpt msg.InvocationId, 
                        unbox<'ServerStreamApi> msg.Item
                    )
                | :? CompletionMessage as msg ->
                    Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.Completion (
                        Headers.toOpt msg.Headers, 
                        msg.InvocationId, 
                        String.toOpt msg.Error, 
                        Obj.toOptAs msg.Result
                    )
                | :? StreamInvocationMessage as msg ->
                    Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.StreamInvocation (
                        Headers.toOpt msg.Headers, 
                        msg.InvocationId, 
                        msg.Target, 
                        Array.elementsAs msg.Arguments, 
                        StreamIds.toOpt msg.StreamIds
                    )
                | :? CancelInvocationMessage as msg ->
                    Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.CancelInvocation (
                        Headers.toOpt msg.Headers, 
                        String.toOpt msg.InvocationId
                    )
                | :? PingMessage -> 
                    Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.Ping
                | :? CloseMessage as msg -> 
                    Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>.Close (
                        String.toOpt msg.Error, 
                        Some msg.AllowReconnect
                    )
                | _ ->
                    message.GetType().FullName
                    |> sprintf "Unsupported message type: %s"
                    |> InvalidOperationException
                    |> raise
                |> serialize

                Array.getMessageBytesWithPrependedLength ms

    [<RequireQualifiedAccess>]
    module private Client =
        module Read =
            let inline boxArr xs = xs |> Array.map box

            let message<'ServerApi,'ServerStreamApi> (input: byref<ReadOnlySequence<byte>>) (message: byref<HubMessage>) =
                if input.Length = 0L then
                    message <- null
                    false
                else
                    let reader = Read.Reader(input.ToArray())
                    let len = reader.Read typeof<uint32> |> unbox<uint32> |> int64
                    
                    input <- input.Slice(len + 1L)

                    try
                        if len > MaxPayloadSize then
                            message <- null
                            false
                        else
                            message <-
                                reader.Read typeof<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                                |> unbox<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                                |> function
                                | Msg.Invocation (headers, invocationId, target, args, streamIds) ->
                                    match invocationId with
                                    | Some invocationId ->
                                        match streamIds with
                                        | Some streamIds -> InvocationMessage(invocationId, target, boxArr args, streamIds)
                                        | None -> InvocationMessage(invocationId, target, boxArr args)
                                    | None -> InvocationMessage(target, boxArr args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.InvokeInvocation (headers, invocationId, target, msg, invokeId, streamIds) ->
                                    let args = [| box msg; box invokeId |]
                        
                                    match invocationId with
                                    | Some invocationId -> 
                                        match streamIds with
                                        | Some streamIds -> InvocationMessage(invocationId, target, args, streamIds)
                                        | None -> InvocationMessage(invocationId, target, args)
                                    | None -> InvocationMessage(target, args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.InvocationExplicit (headers, invocationId, target, args, streamIds) ->
                                    match invocationId with
                                    | Some invocationId ->
                                        match streamIds with
                                        | Some streamIds -> InvocationMessage(invocationId, target, boxArr args, streamIds)
                                        | None -> InvocationMessage(invocationId, target, boxArr args)
                                    | None -> InvocationMessage(target, boxArr args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.StreamItem (headers, invocationId, item) ->
                                    StreamItemMessage(invocationId.Value, item)
                                    |> Headers.set headers :> HubMessage
                                | Msg.Completion (headers, invocationId, error, result) ->
                                    CompletionMessage(invocationId, Option.defaultValue null error, Option.defaultValue Unchecked.defaultof<'ServerApi> result, result.IsSome)
                                    |> Headers.set headers :> HubMessage
                                | Msg.StreamInvocation (headers, invocationId, target, args, streamIds) ->
                                    match streamIds with
                                    | Some streamIds -> StreamInvocationMessage(invocationId, target, boxArr args, streamIds)
                                    | None -> StreamInvocationMessage(invocationId, target, unbox args)
                                    |> Headers.set headers :> HubMessage
                                | Msg.CancelInvocation (headers, invocationId) ->
                                    CancelInvocationMessage(invocationId.Value)
                                    |> Headers.set headers :> HubMessage
                                | Msg.Ping -> PingMessage.Instance :> HubMessage
                                | Msg.Close (error, allowReconnect) ->
                                    let error = Option.defaultValue null error

                                    match allowReconnect with
                                    | Some allowReconnect -> CloseMessage(error, allowReconnect) :> HubMessage
                                    | None -> CloseMessage(error) :> HubMessage
                            true
                    with _ ->
                        message <- Unchecked.defaultof<HubMessage>
                        true

        module Write =
            let message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (message: HubMessage) =
                use ms = new MemoryStream()

                let serializer = Serializer.Client.get<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi>()
                let serialize msg = serializer.Invoke(msg, ms)

                match message with
                | :? InvocationMessage as msg ->
                    match msg.Target with
                    | HubMethod.Invoke ->
                        match Array.head msg.Arguments with
                        | :? 'ClientApi as fstArg when msg.Arguments.Length = 2 && msg.Arguments.[1].GetType() = typeof<System.Guid> ->
                            Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.InvokeInvocation (
                                Headers.toOpt msg.Headers, 
                                String.toOpt msg.InvocationId, 
                                msg.Target, 
                                fstArg,
                                downcast msg.Arguments.[1],
                                StreamIds.toOpt msg.StreamIds
                            )
                        | :? InvokeArg<'ClientApi> ->
                            Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.InvocationExplicit (
                                Headers.toOpt msg.Headers, 
                                String.toOpt msg.InvocationId, 
                                msg.Target, 
                                Array.elementsAs msg.Arguments, 
                                StreamIds.toOpt msg.StreamIds
                            )
                        | arg ->
                            sprintf "Unsupported invocation argument: %A%sFor message: %A"
                                arg
                                System.Environment.NewLine
                                message
                            |> InvalidOperationException
                            |> raise
                    | HubMethod.Send
                    | HubMethod.StreamTo ->
                        Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Invocation (
                            Headers.toOpt msg.Headers, 
                            String.toOpt msg.InvocationId, 
                            msg.Target, 
                            Array.elementsAs msg.Arguments, 
                            StreamIds.toOpt msg.StreamIds
                        )
                    | target ->
                        sprintf "Unsupported invoation target: %s" target
                        |> InvalidOperationException
                        |> raise
                | :? StreamItemMessage as msg ->
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.StreamItem (
                        Headers.toOpt msg.Headers, 
                        String.toOpt msg.InvocationId, 
                        downcast msg.Item
                    )
                | :? CompletionMessage as msg ->
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Completion (
                        Headers.toOpt msg.Headers, 
                        msg.InvocationId, 
                        String.toOpt msg.Error, 
                        Obj.toOptAs msg.Result
                    )
                | :? StreamInvocationMessage as msg ->
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.StreamInvocation (
                        Headers.toOpt msg.Headers, 
                        msg.InvocationId, 
                        msg.Target, 
                        Array.elementsAs msg.Arguments, 
                        StreamIds.toOpt msg.StreamIds
                    )
                | :? CancelInvocationMessage as msg ->
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.CancelInvocation (
                        Headers.toOpt msg.Headers, 
                        String.toOpt msg.InvocationId
                    )
                | :? PingMessage -> 
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Ping
                | :? CloseMessage as msg -> 
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Close (
                        String.toOpt msg.Error, 
                        Some msg.AllowReconnect
                    )
                | _ ->
                    message.GetType().FullName
                    |> sprintf "Unsupported message type: %s"
                    |> InvalidOperationException
                    |> raise
                |> serialize

                Array.getMessageBytesWithPrependedLength ms

    type ClientFableHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
        interface IHubProtocol with
            member _.Name = ProtocolName

            member _.Version = ProtocolVersion

            member _.TransferFormat = TransferFormat.Binary

            member _.IsVersionSupported version = version = ProtocolVersion

            member _.WriteMessage (message: HubMessage, output: IBufferWriter<byte>) = 
                output.Write(ReadOnlySpan<byte>(Client.Write.message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> message))

            member _.TryParseMessage (input: byref<ReadOnlySequence<byte>>, _: IInvocationBinder, message: byref<HubMessage>) =
                Client.Read.message<'ServerApi,'ServerStreamApi> &input &message

            member _.GetMessageBytes (message: HubMessage) = ReadOnlyMemory<byte>(Client.Write.message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> message)

    type ServerFableHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
        interface IHubProtocol with
            member _.Name = ProtocolName

            member _.Version = ProtocolVersion

            member _.TransferFormat = TransferFormat.Binary

            member _.IsVersionSupported version = version = ProtocolVersion

            member _.WriteMessage (message: HubMessage, output: IBufferWriter<byte>) = 
                output.Write(ReadOnlySpan<byte>(Server.Write.message<'ServerApi,'ServerStreamApi> message))

            member _.TryParseMessage (input: byref<ReadOnlySequence<byte>>, _: IInvocationBinder, message: byref<HubMessage>) =
                Server.Read.message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> &input &message

            member _.GetMessageBytes (message: HubMessage) = ReadOnlyMemory<byte>(Server.Write.message<'ServerApi,'ServerStreamApi> message)
