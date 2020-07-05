namespace Fable.SignalR

open Microsoft.AspNetCore.SignalR
open Microsoft.AspNetCore.SignalR.Protocol
open Microsoft.AspNetCore.Connections
open System
open System.IO
open System.Buffers

[<RequireQualifiedAccess>]
module MsgPackProtocol =
    module BinaryMessageParser =
        let [<Literal>] MaxLengthPrefixSize = 5

        let tryParse (buffer: byref<ReadOnlySequence<byte>>) (payload: byref<ReadOnlySequence<byte>>) =
            if buffer.IsEmpty then
                payload <- Unchecked.defaultof<ReadOnlySequence<byte>>
                false
            else
                let lengthPrefixBuffer : ReadOnlySequence<byte> = buffer.Slice(0L, Math.Min(int64 MaxLengthPrefixSize, buffer.Length))
                
                let span =
                    if lengthPrefixBuffer.IsSingleSegment then
                        lengthPrefixBuffer.First.Span
                    else ReadOnlySpan<byte>(lengthPrefixBuffer.ToArray())

                let mutable length = 0uy
                let mutable numBytes = 0

                let mutable byteRead = 0uy

                while numBytes < (int lengthPrefixBuffer.Length) && (byteRead &&& 0x80uy) <> 0uy do
                    byteRead <- span.[numBytes]
                    length <- length ||| ((byteRead &&& 0x7fuy) <<< numBytes * 7)
                    numBytes <- numBytes + 1

                if ((byteRead &&& 0x80uy) <> 0uy) && (numBytes < MaxLengthPrefixSize) then
                    payload <- Unchecked.defaultof<ReadOnlySequence<byte>>
                    false
                elif (byteRead &&& 0x80uy) <> 0uy || (numBytes = MaxLengthPrefixSize && byteRead > 7uy) then
                    failwith "Messages over 2GB in size are not supported"
                elif buffer.Length < (int64 length) + int64 numBytes then
                    payload <- Unchecked.defaultof<ReadOnlySequence<byte>>
                    false
                else
                    payload <- buffer.Slice(numBytes, int length)
                    buffer <- buffer.Slice(int64 (numBytes + int length))
                    true

    [<RequireQualifiedAccess>]
    module CompletionKind =
        let [<Literal>] ErrorResult = 1
        let [<Literal>] VoidResult = 2
        let [<Literal>] NonVoidResult = 3

    type FableHubProtocol () =
        let [<Literal>] ProtocolName = "messagepack"
        let [<Literal>] ProtocolVersion = 1

        member inline private _.ReadInvocationId (reader: byref<MsgPack.Reader>) = 
            reader.ReadString(12)

        member inline private _.ApplyHeaders (source: Map<string,string>) (destination: #HubInvocationMessage) =
            if source.Count > 0 then
                destination.Headers <- source
            
            destination

        member inline private _.MapReadString (reader: byref<MsgPack.Reader>) =
            let result = reader.TryReadString()
            fun i -> result

        member inline private _.MapReadTwoStrings (reader: byref<MsgPack.Reader>) =
            let one,two = reader.TryReadString().Value, reader.TryReadString().Value
            fun i -> one,two

        member inline private this.ReadHeaders (reader: byref<MsgPack.Reader>) =
            match reader.TryReadMapHeader().Value with
            | 0 -> None
            | count ->
                [0 .. count]
                |> List.map (this.MapReadTwoStrings(ref reader))
                |> Map.ofList
                |> Some
            |> Option.get

        member inline private this.ReadStreamIds (reader: byref<MsgPack.Reader>) =
            let chooser = this.MapReadString(ref reader)
            
            reader.TryReadArrayHeader().Value
            |> fun streamIdCount ->
                if streamIdCount > 0 then
                    [|0 .. streamIdCount|]
                    |> Array.choose chooser
                else [||]

        member inline private this.CreateInvocationMessage (reader: byref<MsgPack.Reader>, binder: IInvocationBinder, message: byref<HubMessage>, itemCount: int) =
            let headers = this.ReadHeaders(&reader)
            let invocationId = this.ReadInvocationId(&reader)
            let target = reader.TryReadString().Value

            let arguments =
                binder.GetParameterTypes(target)
                |> Array.ofSeq
                |> Array.map reader.Read

            let streamIds =
                if itemCount > 0 then this.ReadStreamIds(&reader)
                else [||]

            message <-
                InvocationMessage(invocationId, target, arguments, streamIds)
                |> this.ApplyHeaders headers :> HubMessage

            true

        member inline private this.CreateStreamInvocationMessage (reader: byref<MsgPack.Reader>, binder: IInvocationBinder, message: byref<HubMessage>, itemCount: int) =
            let headers = this.ReadHeaders(&reader)
            let invocationId = this.ReadInvocationId(&reader)
            let target = reader.TryReadString().Value

            let arguments =
                binder.GetParameterTypes(target)
                |> Array.ofSeq
                |> Array.map reader.Read

            let streamIds =
                if itemCount > 0 then this.ReadStreamIds(&reader)
                else [||]

            message <-
                StreamInvocationMessage(invocationId, target, arguments, streamIds)
                |> this.ApplyHeaders headers :> HubMessage

            true

        member inline private this.CreateStreamItemMessage (reader: byref<MsgPack.Reader>, binder: IInvocationBinder, message: byref<HubMessage>) =
            let headers = this.ReadHeaders(&reader)
            let invocationId = this.ReadInvocationId(&reader)
            
            let value = binder.GetStreamItemType(invocationId) |> reader.Read

            message <-
                StreamItemMessage(invocationId, value)
                |> this.ApplyHeaders headers :> HubMessage

            true

        member inline private this.CreateCompletionMessage (reader: byref<MsgPack.Reader>, binder: IInvocationBinder, message: byref<HubMessage>) =
            let headers = this.ReadHeaders(&reader)
            let invocationId = this.ReadInvocationId(&reader)
            let resultKind = reader.ReadInt32()

            let error,result,hasResult =
                match resultKind with
                | CompletionKind.ErrorResult -> (Option.defaultValue null (reader.TryReadString()), null, false)
                | CompletionKind.NonVoidResult ->
                    binder.GetReturnType(invocationId)
                    |> reader.Read
                    |> fun res -> (null, res, true)
                | CompletionKind.VoidResult ->
                    (null, null, false)
                | _ -> failwith "Invalid invocation result kind"

            message <-
                CompletionMessage(invocationId, error, result, hasResult)
                |> this.ApplyHeaders headers :> HubMessage

            true

        member inline private this.CreateCancelInvocationMessage (reader: byref<MsgPack.Reader>, message: byref<HubMessage>) =
            let headers = this.ReadHeaders(&reader)
            let invocationId = this.ReadInvocationId(&reader)
            
            message <-
                CancelInvocationMessage(invocationId)
                |> this.ApplyHeaders headers :> HubMessage

            true

        member inline private this.CreateCloseMessage (reader: byref<MsgPack.Reader>, message: byref<HubMessage>, itemCount: int) =
            let error = reader.TryReadString()

            let allowReconnect =
                if itemCount > 2 then reader.TryReadBool()
                else None
                |> Option.defaultValue false
            
            message <-
                match error with
                | None when not allowReconnect -> CloseMessage.Empty
                | Some error -> CloseMessage(error, allowReconnect)
                | _ -> CloseMessage(null, allowReconnect)

            true

        interface IHubProtocol with
            member _.Name = ProtocolName

            member _.Version = ProtocolVersion

            member _.TransferFormat = TransferFormat.Binary

            member _.IsVersionSupported version = version = ProtocolVersion

            member _.WriteMessage (message: HubMessage, output: IBufferWriter<byte>) = 
                use ms = new MemoryStream()
                let readSpan = new ReadOnlySpan<byte>()

                MsgPack.Write.object message ms
                ms.Write(readSpan)
                readSpan.CopyTo(output.GetSpan())

            member this.TryParseMessage (input: byref<ReadOnlySequence<byte>>, binder: IInvocationBinder, message: byref<HubMessage>) = 
                let mutable payload = ReadOnlySequence<byte>()
                if not (BinaryMessageParser.tryParse &input &payload) then
                    message <- Unchecked.defaultof<HubMessage>
                    false
                else
                    let mutable reader = MsgPack.Reader(input.ToArray())
                    let itemCount = reader.TryReadArrayHeader().Value
                    
                    let messageType = reader.ReadInt32()
                        
                    match messageType with
                    | HubProtocolConstants.InvocationMessageType ->
                        this.CreateInvocationMessage(&reader, binder, &message, itemCount)
                    | HubProtocolConstants.StreamItemMessageType ->
                        this.CreateStreamItemMessage(&reader, binder, &message)
                    | HubProtocolConstants.CompletionMessageType ->
                        this.CreateCompletionMessage(&reader, binder, &message)
                    | HubProtocolConstants.StreamInvocationMessageType ->
                        this.CreateStreamInvocationMessage(&reader, binder, &message, itemCount)
                    | HubProtocolConstants.CancelInvocationMessageType ->
                        this.CreateCancelInvocationMessage(&reader, &message)
                    | HubProtocolConstants.PingMessageType ->
                        message <- PingMessage.Instance :> HubMessage
                        true
                    | HubProtocolConstants.CloseMessageType -> 
                        this.CreateCloseMessage(&reader, &message, itemCount)
                    | _ -> 
                        message <- Unchecked.defaultof<HubMessage>
                        true

            member _.GetMessageBytes (message: HubMessage) =
                use ms = new MemoryStream()

                Fable.Remoting.MsgPack.Write.object message ms
            
                ReadOnlyMemory<byte>(ms.GetBuffer())

            