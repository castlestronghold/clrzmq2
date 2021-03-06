﻿[<AutoOpen>]
module Serialization

    open System
    open System.IO
    open ProtoBuf
    open System.Runtime.Serialization

    let serialize_with_protobuf(instance: 'a) =
        use input = new MemoryStream()
        Serializer.Serialize(input, instance)
        input.Flush()
        input.ToArray()

    let internal netbinarySerializer = new NetDataContractSerializer();

    let serialize_with_netbinary(instance: 'a) =
        use input = new MemoryStream()
        netbinarySerializer.Serialize(input, instance)
        input.Flush()
        input.ToArray()

    let deserialize_with_netbinary<'a> (bytes:byte array) : 'a =
        use input = new MemoryStream(bytes)
        netbinarySerializer.Deserialize(input) :?> 'a