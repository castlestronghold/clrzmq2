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
type ResponseMessage(ret:obj, excp:Exception) =
    let mutable returnValue = ret
    let mutable exceptionThrown = excp

    new () = ResponseMessage(null, null)

    [<ProtoMember(1, DynamicType = true)>]
    member this.ReturnValue
        with get() = returnValue
        and set(value) = returnValue <- value

    [<ProtoMember(2, DynamicType = true)>]
    member this.ExceptionThrown
        with get() = exceptionThrown
        and set(value) = exceptionThrown <- value
