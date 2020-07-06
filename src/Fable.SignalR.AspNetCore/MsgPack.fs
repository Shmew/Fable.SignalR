namespace Fable.SignalR

open Microsoft.AspNetCore.SignalR
open Microsoft.AspNetCore.SignalR.Protocol
open Microsoft.AspNetCore.Connections
open System
open System.IO
open System.Buffers

[<RequireQualifiedAccess>]
module internal MsgPackProtocol =
    let [<Literal>] private ProtocolName = "messagepack"
    let [<Literal>] private ProtocolVersion = 1

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

    [<RequireQualifiedAccess>]
    module private Read =
        let inline private readInvocationId (reader: byref<MsgPack.Reader>) = 
            reader.ReadString(12)

        let inline private applyHeaders (source: Map<string,string>) (destination: #HubInvocationMessage) =
            if source.Count > 0 then
                destination.Headers <- source
            
            destination

        let inline private mapReadString (reader: byref<MsgPack.Reader>) =
            let value = reader.TryReadString()
            fun i -> value

        let inline private mapReadTwoStrings (reader: byref<MsgPack.Reader>) =
            let one,two = reader.TryReadString().Value, reader.TryReadString().Value
            fun i -> one,two

        let inline private readHeaders (reader: byref<MsgPack.Reader>) =
            match reader.TryReadMapHeader().Value with
            | 0 -> None
            | count ->
                [0 .. count]
                |> List.map (mapReadTwoStrings &reader)
                |> Map.ofList
                |> Some
            |> Option.get

        let inline private readStreamIds (reader: byref<MsgPack.Reader>) =
            let chooser = mapReadString(&reader)
            
            reader.TryReadArrayHeader().Value
            |> fun streamIdCount ->
                if streamIdCount > 0 then
                    [|0 .. streamIdCount|]
                    |> Array.choose chooser
                else [||]

        module Message =
            let inline invocation (reader: byref<MsgPack.Reader>) (binder: IInvocationBinder) (message: byref<HubMessage>) (itemCount: int) =
                let headers = readHeaders &reader
                let invocationId = readInvocationId &reader
                let target = reader.TryReadString().Value

                let arguments =
                    binder.GetParameterTypes(target)
                    |> Array.ofSeq
                    |> Array.map reader.Read

                let streamIds =
                    if itemCount > 0 then readStreamIds(&reader)
                    else [||]

                message <-
                    InvocationMessage(invocationId, target, arguments, streamIds)
                    |> applyHeaders headers :> HubMessage

                true

            let inline streamInvocation (reader: byref<MsgPack.Reader>) (binder: IInvocationBinder) (message: byref<HubMessage>) (itemCount: int) =
                let headers = readHeaders &reader
                let invocationId = readInvocationId &reader
                let target = reader.TryReadString().Value

                let arguments =
                    binder.GetParameterTypes(target)
                    |> Array.ofSeq
                    |> Array.map reader.Read

                let streamIds =
                    if itemCount > 0 then readStreamIds(&reader)
                    else [||]

                message <-
                    StreamInvocationMessage(invocationId, target, arguments, streamIds)
                    |> applyHeaders headers :> HubMessage

                true

            let inline streamItem (reader: byref<MsgPack.Reader>) (binder: IInvocationBinder) (message: byref<HubMessage>) =
                let headers = readHeaders &reader
                let invocationId = readInvocationId &reader
                
                let value = binder.GetStreamItemType(invocationId) |> reader.Read

                message <-
                    StreamItemMessage(invocationId, value)
                    |> applyHeaders headers :> HubMessage

                true

            let inline completion (reader: byref<MsgPack.Reader>) (binder: IInvocationBinder) (message: byref<HubMessage>) =
                let headers = readHeaders &reader
                let invocationId = readInvocationId &reader
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
                    |> applyHeaders headers :> HubMessage

                true

            let inline cancelInvocation (reader: byref<MsgPack.Reader>) (message: byref<HubMessage>) =
                let headers = readHeaders &reader
                let invocationId = readInvocationId &reader
                
                message <-
                    CancelInvocationMessage(invocationId)
                    |> applyHeaders headers :> HubMessage

                true

            let inline close (reader: byref<MsgPack.Reader>) (message: byref<HubMessage>) (itemCount: int) =
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

    [<RequireQualifiedAccess>]
    module private Write =
        let inline writeStringUnlessEmpty (str: string) (writer: byref<MemoryStream>) =
            if String.IsNullOrEmpty str then
                MsgPack.Write.nil writer
            else MsgPack.Write.str str writer

        module private Message =
            let inline invocation (writer: byref<MemoryStream>) (message: InvocationMessage) =
                MsgPack.Write.arrayHeader 6uy writer
                MsgPack.Write.int (int64 HubProtocolConstants.InvocationMessageType) writer
                MsgPack.Write.object message.Headers writer
                MsgPack.Write.object message.InvocationId writer
                MsgPack.Write.str message.Target writer
                MsgPack.Write.array writer message.Arguments
                MsgPack.Write.object message.StreamIds writer

            let inline streamInvocation (writer: byref<MemoryStream>) (message: StreamInvocationMessage) =
                MsgPack.Write.arrayHeader 6uy writer
                MsgPack.Write.int (int64 HubProtocolConstants.StreamInvocationMessageType) writer
                MsgPack.Write.object message.Headers writer
                writeStringUnlessEmpty message.InvocationId &writer
                MsgPack.Write.str message.Target writer
                MsgPack.Write.array writer message.Arguments
                MsgPack.Write.object message.StreamIds writer

            let inline streamItem (writer: byref<MemoryStream>) (message: StreamItemMessage) =
                MsgPack.Write.arrayHeader 4uy writer
                MsgPack.Write.int (int64 HubProtocolConstants.StreamItemMessageType) writer
                MsgPack.Write.object message.Headers writer
                MsgPack.Write.str message.InvocationId writer
                MsgPack.Write.object message.Item writer
                
            let inline completion (writer: byref<MemoryStream>) (message: CompletionMessage) =
                let resultKind =
                    if not (isNull message.Error) then
                        CompletionKind.ErrorResult
                    elif message.HasResult then CompletionKind.NonVoidResult
                    else CompletionKind.VoidResult
                
                MsgPack.Write.arrayHeader (4 + (if resultKind = CompletionKind.VoidResult then 0 else 1)) writer
                MsgPack.Write.int (int64 HubProtocolConstants.CompletionMessageType) writer
                MsgPack.Write.object message.Headers writer
                MsgPack.Write.str message.InvocationId writer
                MsgPack.Write.int (int64 resultKind) writer

                match resultKind with
                | CompletionKind.ErrorResult ->
                    MsgPack.Write.str message.Error writer
                | CompletionKind.NonVoidResult ->
                    MsgPack.Write.object message.Result writer
                | _ -> ()

            let inline cancel (writer: byref<MemoryStream>) (message: CancelInvocationMessage) =
                MsgPack.Write.arrayHeader 3uy writer
                MsgPack.Write.int (int64 HubProtocolConstants.CancelInvocationMessageType) writer
                MsgPack.Write.object message.Headers writer
                MsgPack.Write.str message.InvocationId writer

            let inline close (writer: byref<MemoryStream>) (message: CloseMessage) =
                MsgPack.Write.arrayHeader 3uy writer
                MsgPack.Write.int (int64 HubProtocolConstants.CloseMessageType) writer
                writeStringUnlessEmpty message.Error &writer
                MsgPack.Write.bool message.AllowReconnect writer
                
            let inline ping (writer: byref<MemoryStream>) =
                MsgPack.Write.arrayHeader 1uy writer
                MsgPack.Write.int (int64 HubProtocolConstants.PingMessageType) writer

        let inline message (message: HubMessage) (writer: byref<MemoryStream>) =
            match message with
            | :? InvocationMessage as msg ->
                Message.invocation &writer msg
            | :? StreamItemMessage as msg ->
                Message.streamItem &writer msg
            | :? CompletionMessage as msg ->
                Message.completion &writer msg
            | :? StreamInvocationMessage as msg ->
                Message.streamInvocation &writer msg
            | :? CancelInvocationMessage as msg ->
                Message.cancel &writer msg
            | :? PingMessage ->
                Message.ping &writer
            | :? CloseMessage as msg ->
                Message.close &writer msg
            | _ -> MsgPack.Write.object message writer

            writer.Flush()
            
    type FableHubProtocol () =
        interface IHubProtocol with
            member _.Name = ProtocolName

            member _.Version = ProtocolVersion

            member _.TransferFormat = TransferFormat.Binary

            member _.IsVersionSupported version = version = ProtocolVersion

            member _.WriteMessage (message: HubMessage, output: IBufferWriter<byte>) = 
                use mutable ms = new MemoryStream()
                
                Write.message message &ms
                
                output.Write(ReadOnlySpan<byte>(ms.ToArray()))

            member _.TryParseMessage (input: byref<ReadOnlySequence<byte>>, binder: IInvocationBinder, message: byref<HubMessage>) = 
                let mutable payload = ReadOnlySequence<byte>()
                if not (BinaryMessageParser.tryParse &input &payload) then
                    message <- Unchecked.defaultof<HubMessage>
                    false
                else
                    let mutable reader = MsgPack.Reader(input.ToArray())
                    let itemCount = Option.defaultValue 0 (reader.TryReadArrayHeader())
                    
                    let messageType = reader.ReadInt32()
                        
                    match messageType with
                    | HubProtocolConstants.InvocationMessageType ->
                        Read.Message.invocation &reader binder &message itemCount
                    | HubProtocolConstants.StreamItemMessageType ->
                        Read.Message.streamItem &reader binder &message
                    | HubProtocolConstants.CompletionMessageType ->
                        Read.Message.completion &reader binder &message
                    | HubProtocolConstants.StreamInvocationMessageType ->
                        Read.Message.streamInvocation &reader binder &message itemCount
                    | HubProtocolConstants.CancelInvocationMessageType ->
                        Read.Message.cancelInvocation &reader &message
                    | HubProtocolConstants.PingMessageType ->
                        message <- PingMessage.Instance :> HubMessage
                        true
                    | HubProtocolConstants.CloseMessageType -> 
                        Read.Message.close &reader &message itemCount
                    | _ -> 
                        message <- Unchecked.defaultof<HubMessage>
                        true

            member _.GetMessageBytes (message: HubMessage) =
                use mutable ms = new MemoryStream()

                Write.message message &ms

                ReadOnlyMemory<byte>(ms.ToArray())

            
