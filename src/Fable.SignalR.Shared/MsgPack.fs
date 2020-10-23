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

        module Serializer =
            let inline private make<'ServerApi,'ServerStreamApi> () = Write.makeSerializer<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>()

            let inline get<'ServerApi,'ServerStreamApi> () = memoize make<'ServerApi,'ServerStreamApi> ()

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

        module StreamIds =
            let toOpt (streamIds: string []) =
                match streamIds with
                | null -> None
                | _ when Array.isEmpty streamIds -> None
                | _ -> Some streamIds

        module String =
            let toOpt (s: string) =
                if String.IsNullOrEmpty s then None
                else Some s

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

        module Array =
            let inline elementsAs<'T> (o: obj []) =
                o |> Array.choose Obj.toOptAs<'T>

    [<RequireQualifiedAccess>]
    module private Read =
        let inline boxArr xs = xs |> Array.map box

        let message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (input: byref<ReadOnlySequence<byte>>) (message: byref<HubMessage>) =
            let reader = Read.Reader(input.ToArray())

            let len = reader.Read typeof<uint32> |> unbox<uint32> |> int64

            input <- input.Slice(len + 1L)

            try
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
            with e ->
                message <- Unchecked.defaultof<HubMessage>
                true

    [<RequireQualifiedAccess>]
    module private Write =
        let message<'ServerApi,'ServerStreamApi> (message: HubMessage) =
            use ms = new MemoryStream()

            let serializer = Serializer.get<'ServerApi,'ServerStreamApi>()
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

            if ms.Length > MaxPayloadSize then
                sprintf "Writing messages above 2GB is not supported."
                |> InvalidOperationException
                |> raise
            
            use msgMs = new MemoryStream()

            Write.writeUInt64 (uint64 ms.Length) msgMs
            
            ms.ToArray()
            |> Array.append (msgMs.ToArray())

    type FableHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
        interface IHubProtocol with
            member _.Name = ProtocolName

            member _.Version = ProtocolVersion

            member _.TransferFormat = TransferFormat.Binary

            member _.IsVersionSupported version = version = ProtocolVersion

            member _.WriteMessage (message: HubMessage, output: IBufferWriter<byte>) = 
                output.Write(ReadOnlySpan<byte>(Write.message<'ServerApi,'ServerStreamApi> message))

            member _.TryParseMessage (input: byref<ReadOnlySequence<byte>>, _: IInvocationBinder, message: byref<HubMessage>) =
                Read.message<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> &input &message

            member _.GetMessageBytes (message: HubMessage) = ReadOnlyMemory<byte>(Write.message<'ServerApi,'ServerStreamApi> message)
