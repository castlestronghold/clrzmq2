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

    type RemoteRequestListener(bindAddress:String, zContextAccessor:ZContextAccessor, kernel:IKernel) =
        inherit BaseListener(zContextAccessor)

        override this.GetConfig() = 
            let parts = bindAddress.Split(':')

            ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        override this.GetReplyFor(request, socket) = 
            let response = ResponseMessage(serialize_with_netbinary(3), null)

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

            let bytes = socket.Recv(15000)

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
                Console.WriteLine("invoked")

                let request = RequestMessage(invocation.Method.DeclaringType.Name, invocation.Method.Name, invocation.Arguments)
                let endpoint = router.GetEndpoint(invocation.Method.DeclaringType.Assembly)

                let response = RemoteRequest(zContextAccessor, request, endpoint).Get()

                if response.ExceptionThrown <> null then
                    raise response.ExceptionThrown

                if invocation.Method.ReturnType <> typeof<Void> then
                    invocation.ReturnValue <- deserialize_with_netbinary<obj>(response.ReturnValue)

        interface IOnBehalfAware with

            member this.SetInterceptedComponentModel(target) = ()

    type RemoteRequestInspector() =
        inherit MethodMetaInspector()

        override this.ObtainNodeName() = "remote-interceptor"

        member this.add_interceptor(model:ComponentModel) =
            model.Dependencies.Add(new DependencyModel(this.ObtainNodeName(), typeof<RemoteRequestInterceptor>, false))
            model.Interceptors.Add(new InterceptorReference(typeof<RemoteRequestInterceptor>))
        
        override this.ProcessModel(kernel, model) =
            if model.Services |> Seq.exists (fun s -> s.IsDefined(typeof<RemoteServiceAttribute>, false)) then
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
                                 Component.For<RemoteRequestInterceptor>().LifeStyle.Transient) |> ignore

            base.Kernel.ComponentModelBuilder.AddContributor(new RemoteRequestInspector())

            if not (String.IsNullOrEmpty(base.FacilityConfig.Attributes.["listen"])) then
                this.setup_server()

            if base.FacilityConfig.Children.["endpoints"] <> null then
                this.setup_client()