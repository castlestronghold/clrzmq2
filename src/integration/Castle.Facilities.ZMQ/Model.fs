namespace Castle.Facilities.ZMQ.Internals

    open System
    open ProtoBuf

    [<Serializable; AllowNullLiteralAttribute>]
    [<ProtoContract(SkipConstructor=true)>]
    type ParamTuple(value:byte[], typeN:string) =
        let mutable serializedValue = value
        let mutable typeName = typeN

        [<ProtoMember(1)>]
        member x.SerializedValue    with get() = serializedValue and set(v) = serializedValue <- v
        [<ProtoMember(2)>]
        member x.TypeName   with get() = typeName and set(v) = typeName <- v


    [<Serializable; AllowNullLiteralAttribute>]
    [<ProtoContract>]
    type RequestMessage(service:string, methd:string, parms:ParamTuple array, parmTypes: string array) =
        let mutable targetService:string = service
        let mutable targetMethod:string = methd
        let mutable methodParams = parms
        let mutable methodParamTypes = parmTypes
        // let mutable methodMedta = meta
        
        new () = RequestMessage(null, null, null, null)

        [<ProtoMember(1)>]
        member this.TargetService
            with get() = targetService
            and set(value) = targetService <- value

        [<ProtoMember(2)>]
        member this.TargetMethod
            with get() = targetMethod
            and set(value) = targetMethod <- value

        [<ProtoMember(3)>]
        member this.Params with get() = methodParams and set(v) = methodParams <- v
        
        [<ProtoMember(4)>]
        member this.ParamTypes with get() = methodParamTypes and set(v) = methodParamTypes <- v


    [<Serializable; AllowNullLiteralAttribute>]
    [<ProtoContract>]
    type ExceptionInfo(typename:string, message:string) = 
        let mutable typenameV = typename
        let mutable messageV = message
    
        new () = ExceptionInfo(null, null)

        [<ProtoMember(1)>]
        member this.Typename
            with get() = typenameV
            and set(value) = typenameV <- value
    
        [<ProtoMember(2)>]
        member this.Message
            with get() = messageV
            and set(value) = messageV <- value


    [<Serializable; AllowNullLiteralAttribute>]
    [<ProtoContract>]
    [<ProtoInclude(1, typeof<ExceptionInfo>)>]
    type ResponseMessage(ret:byte[], retType:string, excp:ExceptionInfo) =
        let mutable returnValue = ret
        let mutable returnValueType : string = retType
        let mutable exceptionThrown = excp

        new () = ResponseMessage(null, null, null)

        [<ProtoMember(1)>]
        member this.ExceptionInfo
            with get() = exceptionThrown
            and set(value) = exceptionThrown <- value
        
        [<ProtoMember(2)>]
        member this.ReturnValue
            with get() = returnValue
            and set(value) = returnValue <- value

        [<ProtoMember(3)>]
        member this.ReturnValueType
            with get() = returnValueType and set(value) = returnValueType <- value

        