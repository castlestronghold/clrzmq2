namespace Castle.Facilities.ZMQ

open ZMQ
open ZMQ.Extensions
open System
open System.IO
open System.Reflection
open System.Collections.Generic
open Castle.Core
open Castle.Core.Configuration
open Castle.Core.Interceptor
open Castle.DynamicProxy
open Castle.Windsor
open Castle.MicroKernel
open Castle.MicroKernel.Facilities
open Castle.MicroKernel.ModelBuilder.Inspectors
open Castle.MicroKernel.Registration

    [<AttributeUsage(AttributeTargets.Interface)>]
    type RemoteServiceAttribute() =
        inherit Attribute()

    [<Serializable>]
    type RequestMessage(service:string, methd:string, parms:obj array) =
        let mutable targetService:string = service
        let mutable targetMethod:string = methd
        let mutable methodParams = parms
        
        new () = RequestMessage(null, null, null)

        member this.TargetService
         with get() = targetService
         and set(value) = targetService <- value

        member this.TargetMethod
         with get() = targetMethod
         and set(value) = targetMethod <- value

        member this.MethodParams
         with get() = methodParams
         and set(value) = methodParams <- value

    [<Serializable>]
    type ResponseMessage(ret:byte array, excp:Exception) =
        let mutable returnValue = ret
        let mutable exceptionThrown = excp

        new () = ResponseMessage(null, null)

        member this.ReturnValue
         with get() = returnValue
         and set(value) = returnValue <- value

        member this.ExceptionThrown
         with get() = exceptionThrown
         and set(value) = exceptionThrown <- value

    type Dispatcher(kernel:IKernel) =
        member this.Invoke(target:string, methd:string, parms: obj array) = 
            let tgtType = Type.GetType(target)

            let instance = kernel.Resolve(tgtType)

            let methodBase = tgtType.GetMethod(methd, parms |> Seq.map (fun p -> p.GetType()) |> Seq.toArray)

            (methodBase.ReturnType <> typeof<Void>, methodBase.Invoke(instance, parms))

    type RemoteRequestListener(bindAddress:String, zContextAccessor:ZContextAccessor, dispatcher:Dispatcher) =
        inherit BaseListener(zContextAccessor)

        override this.GetConfig() = 
            let parts = bindAddress.Split(':')

            ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        override this.GetReplyFor(message, socket) = 
            let request = deserialize_with_netbinary<RequestMessage>(message);

            let response = 
                            try
                                let nonVoid, result = dispatcher.Invoke(request.TargetService, request.TargetMethod, request.MethodParams)
            
                                ResponseMessage(serialize_with_netbinary(result), null)
                            with
                                | :? TargetInvocationException as ex -> ResponseMessage(null, ex.InnerException)
                                | :? Exception as ex -> ResponseMessage(null, ex)

            serialize_with_netbinary(response)

        interface IStartable with
            member this.Start() = base.Start()
            member this.Stop() = base.Stop()

    type RemoteRequest(zContextAccessor:ZContextAccessor, message:RequestMessage, endpoint:string) = 
        inherit BaseRequest<ResponseMessage>(zContextAccessor)

        override this.GetConfig() = 
            let parts = endpoint.Split(':')

            ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        override this.InternalGet(socket) =
            socket.Send(serialize_with_netbinary(message))

            let timeout = 15000
            let bytes = socket.Recv(timeout)

            deserialize_with_netbinary<ResponseMessage>(bytes)

    type RemoteRouter() =
        let routes = Dictionary<string, string>()

        member this.ParseRoutes(config:IConfiguration) =
            for child in config.Children do
                routes.Add(child.Attributes.["assembly"], child.Attributes.["address"])

        member this.GetEndpoint(assembly:Assembly) =
            routes.[assembly.GetName().Name]

    type RemoteRequestInterceptor(zContextAccessor:ZContextAccessor, router:RemoteRouter) =

        interface IInterceptor with
            
            member this.Intercept(invocation) =
                if invocation.TargetType <> null then
                    invocation.Proceed()
                else
                    let request = RequestMessage(invocation.Method.DeclaringType.AssemblyQualifiedName, invocation.Method.Name, invocation.Arguments)
                    let endpoint = router.GetEndpoint(invocation.Method.DeclaringType.Assembly)

                    let response = RemoteRequest(zContextAccessor, request, endpoint).Get()

                    if response.ExceptionThrown <> null then
                        raise response.ExceptionThrown

                    if invocation.Method.ReturnType <> typeof<Void> then
                        invocation.ReturnValue <- deserialize_with_netbinary<obj>(response.ReturnValue)

        interface IOnBehalfAware with

            member this.SetInterceptedComponentModel(target) = ()

    type RemoteRequestInspector(isServer:bool) =
        inherit MethodMetaInspector()

        override this.ObtainNodeName() = "remote-interceptor"

        member this.add_interceptor(model:ComponentModel) =
            model.Dependencies.Add(new DependencyModel(this.ObtainNodeName(), typeof<RemoteRequestInterceptor>, false))
            model.Interceptors.Add(new InterceptorReference(typeof<RemoteRequestInterceptor>))
        
        override this.ProcessModel(kernel, model) =
            if (model.Services |> Seq.exists (fun s -> s.IsDefined(typeof<RemoteServiceAttribute>, false))) then
                this.add_interceptor(model)

    type ZeroMQFacility() =
        inherit AbstractFacility()

        member this.setup_server() =
            let listener = Component.For<RemoteRequestListener>().Parameters(Parameter.ForKey("bindAddress").Eq(base.FacilityConfig.Attributes.["listen"]))
            base.Kernel.Register(listener) |> ignore

        member this.setup_client() =
            let router = base.Kernel.Resolve<RemoteRouter>()

            router.ParseRoutes(base.FacilityConfig.Children.["endpoints"])

        override this.Init() =
            base.Kernel.Register(Component.For<ZContextAccessor>(),
                                 Component.For<RemoteRouter>(),
                                 Component.For<Dispatcher>(),
                                 Component.For<RemoteRequestInterceptor>().LifeStyle.Transient) |> ignore

            let isServer = not (String.IsNullOrEmpty(base.FacilityConfig.Attributes.["listen"]))

            base.Kernel.ComponentModelBuilder.AddContributor(new RemoteRequestInspector(isServer))

            if isServer then
                this.setup_server()

            if base.FacilityConfig.Children.["endpoints"] <> null then
                this.setup_client()