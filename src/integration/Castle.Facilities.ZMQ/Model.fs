namespace Castle.Facilities.ZMQ.Internals

    open System
    open ProtoBuf

    [<Serializable; AllowNullLiteralAttribute>]
    [<ProtoContract>]
    type RequestMessage(service:string, methd:string, parms:obj array) =
        let mutable targetService:string = service
        let mutable targetMethod:string = methd
        let mutable methodParams = parms
        
        new () = RequestMessage(null, null, null)

        [<ProtoMember(1)>]
        member this.TargetService
            with get() = targetService
            and set(value) = targetService <- value

        [<ProtoMember(2)>]
        member this.TargetMethod
            with get() = targetMethod
            and set(value) = targetMethod <- value

        [<ProtoMember(3, DynamicType = true)>]
        member this.MethodParams
            with get() = methodParams
            and set(value) = methodParams <- value


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
    type ResponseMessage(ret:obj, excp:ExceptionInfo) =
        let mutable returnValue = ret
        let mutable returnValueArray : obj[] = [||]
        let mutable exceptionThrown = excp

        new () = ResponseMessage(null, null)

        [<ProtoMember(5, DynamicType = true)>]
        member this.ReturnValue
            with get() = returnValue
            and set(value) = returnValue <- value

        [<ProtoMember(6, DynamicType = true)>]
        member this.ReturnValueArray
            with get() = returnValueArray
            and set(value) = returnValueArray <- value

        [<ProtoMember(7)>]
        member this.ExceptionInfo
            with get() = exceptionThrown
            and set(value) = exceptionThrown <- value
