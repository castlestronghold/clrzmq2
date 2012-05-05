namespace Castle.Facilities.ZMQ

open System

    [<AttributeUsage(AttributeTargets.Interface)>]
    type RemoteServiceAttribute() =
        inherit Attribute()