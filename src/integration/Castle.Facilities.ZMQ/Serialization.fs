[<AutoOpen>]
module Serialization

    open System
    open System.Collections.Generic
    open System.Reflection
    open System.IO
    open ProtoBuf
    open System.Runtime.Serialization

    let  is_collection o =
        if o = null then false
        else
            let t = o.GetType()
            let isGen = t.IsGenericType
            if t = typeof<string> then false
            elif isGen && (t.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>) then
                true
            else 
                t.IsArray

    let to_array (o:obj) =
        if o = null then [||]
        else
            let t = o.GetType()
            let isGen = t.IsGenericType
            if isGen && (t.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>) then
                let elem = (o :?> IEnumerable<obj>) 
                let items = 
                    seq {
                        use enumerator = elem.GetEnumerator()
                        while enumerator.MoveNext() do
                            yield enumerator.Current
                       }
                items |> Seq.toArray
            else // isArray
                o :?> obj[]
                //[||]

    let make_strongly_typed_array (expectedType:Type) (items:obj[]) = 
        let typedArray = Array.CreateInstance(expectedType, items.Length)
        items |> Array.iteri (fun i e -> typedArray.SetValue(e,i) )
        typedArray

    let make_strongly_typed_enumerable (expectedType:Type) (items:obj[]) = 
        let listType = typedefof<List<_>>.MakeGenericType(expectedType)
        let typedList = Activator.CreateInstance(listType)

        let addMethod = listType.GetMethod("Add")

        items |> Array.iter (fun e -> addMethod.Invoke(typedList, [| e |]) |> ignore )

        typedList


    let serialize_param i (t:ParameterInfo) (v:obj) = 
        let pType = t.ParameterType
        if pType.IsArray then failwithf "ZMQ facility doesnt support array as parameters"

        if v = null then 
            null 
        else 
            if pType.IsPrimitive then
                v.ToString() :> obj

            // non primitive but common

            elif pType = typeof<decimal> then
                v.ToString() :> obj

            // Enum
            elif pType.IsEnum then
                System.Convert.ToInt32( v ).ToString() :> obj

            elif pType = typeof<string> then
                v

            // Structs
            elif pType = typeof<Guid> then
                v.ToString() :> obj
            elif pType = typeof<DateTime> then
                let dt = (v :?> DateTime).Ticks
                dt.ToString() :> obj
            else
                v        

    let serialize_parameters (originalArgs:obj[]) (ps:ParameterInfo[]) = 
        let args = 
            ps
            |> Seq.mapi (fun i t -> serialize_param i t (originalArgs.[i]))
            |> Seq.toArray
        args

    let deserialize_param i (v:obj) (pType:Type) = 
        if pType = typeof<decimal> then
            System.Convert.ToDecimal(v) :> obj
                                        
        elif pType.IsEnum then
            let iVal = System.Convert.ToInt32(v)
            iVal :> obj

        elif pType = typeof<int> then
            System.Convert.ToInt32(v) :> obj
        elif pType = typeof<int16> then
            System.Convert.ToInt16(v) :> obj
        elif pType = typeof<int64> then
            System.Convert.ToInt64(v) :> obj
        elif pType = typeof<byte> then
            System.Convert.ToByte(v) :> obj

        elif pType = typeof<float32> then
            System.Convert.ToSingle(v) :> obj
                                        
        elif pType = typeof<double> then
            System.Convert.ToDouble(v) :> obj

        elif pType = typeof<Guid> then
            Guid.Parse(v.ToString()) :> obj

        elif pType = typeof<DateTime> then
            let long = Convert.ToInt64(v)
            DateTime(long) :> obj
        else
            v


    let deserialize_params (parms:obj[]) (ps:ParameterInfo[]) = 
        // ref / out params not supported
        if parms = null then null
        else 
            parms 
            |> Seq.mapi (fun i v -> deserialize_param i v (ps.[i].ParameterType)) 
            |> Seq.toArray


    let serialize_with_protobuf(instance: 'a) =
        // let watch = System.Diagnostics.Stopwatch()
        // watch.Start()
        use input = new MemoryStream()
        Serializer.Serialize(input, instance)
        input.Flush()
        // watch.Stop()
        // Console.Out.WriteLine ("serialize_with_protobuf {0} elapsed {1}", input.Length, watch.ElapsedTicks)
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