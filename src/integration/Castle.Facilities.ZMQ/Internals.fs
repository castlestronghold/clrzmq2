namespace Castle.Facilities.ZMQ.Internals

open ZMQ
open ZMQ.Extensions
open ZMQ.ZMQDevice
open System
open System.IO
open System.Reflection
open System.Collections.Generic
open System.Threading
open Castle.Core
open Castle.Core.Configuration
open Castle.Core.Interceptor
open Castle.DynamicProxy
open Castle.Windsor
open Castle.MicroKernel
open Castle.MicroKernel.Facilities
open Castle.MicroKernel.ModelBuilder.Inspectors
open Castle.MicroKernel.Registration
open Castle.Facilities.ZMQ

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

            let methodBase = tgtType.GetMethod(methd, parms |> Array.map (fun p -> p.GetType()))

            (methodBase.ReturnType <> typeof<Void>, methodBase.Invoke(instance, parms))

    type RemoteRequestListener(bindAddress:String, workers:Int16, zContextAccessor:ZContextAccessor, dispatcher:Dispatcher) =
        inherit BaseListener(zContextAccessor)

        let mutable pool:WorkerPool = null

        let config = lazy
                        let parts = bindAddress.Split(':')
                        ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        member this.thread_worker (state:obj) = 
            try
                use socket = zContextAccessor.SocketFactory.Invoke(SocketType.REP)

                socket.Connect(config.Force().Local)

                base.AcceptAndHandleMessage(socket)
            with
                | :? Exception as ex -> base.Logger.Fatal("Error creating worker thread", ex)

        override this.GetConfig() = config.Force()

        override this.GetReplyFor(message, socket) = 
            let request = deserialize_with_netbinary<RequestMessage>(message);

            let response = 
                            try
                                let nonVoid, result = dispatcher.Invoke(request.TargetService, request.TargetMethod, request.MethodParams)
                                
                                //TODO: Add suport for more formatters
                                ResponseMessage(serialize_with_netbinary(result), null)
                            with
                                | :? TargetInvocationException as ex -> ResponseMessage(null, ex.InnerException)
                                | :? Exception as ex -> ResponseMessage(null, ex)

            serialize_with_netbinary(response)

        interface IStartable with
            override this.Start() = 
                base.Logger.Debug("Starting " + this.GetType().Name)

                let c = config.Force()

                pool <- new WorkerPool(c.ToString(), c.Local, new ThreadStart(this.thread_worker), workers)

                base.Logger.InfoFormat("Binding {0} on {1}:{2} with {3} workers", this.GetType().Name, c.Ip, c.Port, workers)

            override this.Stop() = 
                if pool <> null then
                    pool.Dispose()

                base.Stop()

    type RemoteRequest(zContextAccessor:ZContextAccessor, message:RequestMessage, endpoint:string) = 
        inherit BaseRequest<ResponseMessage>(zContextAccessor)

        let config = lazy
                        let parts = endpoint.Split(':')
                        ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        override this.GetConfig() = config.Force()

        override this.InternalGet(socket) =
            socket.Send(serialize_with_netbinary(message))

            let timeout = 7500
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
        static let logger = log4net.LogManager.GetLogger(typeof<RemoteRequestInterceptor>)

        interface IInterceptor with
            
            member this.Intercept(invocation) =
                let stopwatch = new System.Diagnostics.Stopwatch()
                
                if logger.IsDebugEnabled then
                    stopwatch.Start()

                try
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
                finally
                    if logger.IsDebugEnabled then
                        logger.Debug("Intercept took " + (stopwatch.ElapsedMilliseconds.ToString()))
                    
        interface IOnBehalfAware with

            member this.SetInterceptedComponentModel(target) = ()

    type RemoteRequestInspector() =
        inherit MethodMetaInspector()

        override this.ObtainNodeName() = "remote-interceptor"

        member this.add_interceptor(model:ComponentModel) =
            model.Dependencies.Add(new DependencyModel(this.ObtainNodeName(), typeof<RemoteRequestInterceptor>, false))
            model.Interceptors.Add(new InterceptorReference(typeof<RemoteRequestInterceptor>))
        
        override this.ProcessModel(kernel, model) =
            if (model.Services |> Seq.exists (fun s -> s.IsDefined(typeof<RemoteServiceAttribute>, false))) then
                this.add_interceptor(model)

    type Reaper(zContextAccessor:ZContextAccessor) =
        static let logger = log4net.LogManager.GetLogger(typeof<Reaper>)

        let dispose() = 
            try
                logger.Info("Disposing ZeroMQ Facility...")

                let pool = SocketManager.Instance.Value

                pool.Dispose()

                logger.Info("Disposed ZeroMQ Facility.")
             with
                | :? Exception as ex -> logger.Error("Error disponsing ZeroMQ Facility components", ex)

        interface IDisposable with
            
            member this.Dispose() = dispose()