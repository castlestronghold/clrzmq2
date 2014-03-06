[<AutoOpen>]
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

    let deserialize_with_protobuf<'a> (bytes:byte array) : 'a =
        use input = new MemoryStream(bytes)
        Serializer.Deserialize<'a>(input)

    // let internal netbinarySerializer = new NetDataContractSerializer();

    (*
    let serialize_with_msgpack(instance: 'a when 'a : null) =
        if (instance <> null) then
            let watch = System.Diagnostics.Stopwatch()
            watch.Start()

            use input = new MemoryStream()
            let packer = MsgPack.Serialization.MessagePackSerializer.Create<'a>()
            packer.Pack(input, instance)
            input.Flush()
            watch.Stop()
            Console.Out.WriteLine ("serialize_with_netbinary {0} elapsed {1}", input.Length, watch.ElapsedTicks)
            let buffer = input.ToArray()
            buffer
        else 
            null

    let deserialize_with_msgpack<'a when 'a : null and 'a : (new : unit -> 'a)> (bytes:byte array) : 'a  =
        if bytes <> null && bytes.Length = 0 then 
            new 'a()
        elif bytes <> null then
            use input = new MemoryStream(bytes)
            let packer = MsgPack.Serialization.MessagePackSerializer.Create<'a>()
            packer.Unpack(input)
        else 
            null
    

    
    let serialize_with_netbinary(instance: 'a) =
        let watch = System.Diagnostics.Stopwatch()
        watch.Start()

        use input = new MemoryStream()
        netbinarySerializer.Serialize(input, instance)
        input.Flush()
        watch.Stop()
        Console.Out.WriteLine ("serialize_with_netbinary {0} elapsed {1}", input.Length, watch.ElapsedTicks)
        input.ToArray()

    let deserialize_with_netbinary<'a> (bytes:byte array) : 'a =
        use input = new MemoryStream(bytes)
        netbinarySerializer.Deserialize(input) :?> 'a
    
    *)