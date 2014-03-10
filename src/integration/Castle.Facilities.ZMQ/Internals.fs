namespace Castle.Facilities.ZMQ.Internals

open ZMQ
open ZMQ.Extensions
open ZMQ.ZMQDevice
open System
open System.IO
open System.Diagnostics
open System.Reflection
open System.Collections.Generic
open System.Collections.Concurrent
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
open ZMQ.Counters
open System.Runtime.Remoting.Messaging


    type Dispatcher(kernel:IKernel) =
        let _typename2Type = ConcurrentDictionary<string,Type>(StringComparer.Ordinal)

        member this.Invoke(target:string, methd:string, parms: obj array) = 
            let resolvedType = 
                let res, t = _typename2Type.TryGetValue target
                if not res then
                    let tgtType = Type.GetType(target)
                    _typename2Type.TryAdd (target, tgtType) |> ignore
                    tgtType
                else t
                
            let instance = kernel.Resolve(resolvedType)

            // assumption: overload is not supported
            let methodBase = 
                resolvedType.GetMethod(methd, BindingFlags.Instance ||| BindingFlags.Public)

            let args = deserialize_params parms (methodBase.GetParameters())

            methodBase.Invoke(instance, args)

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
                | ex -> base.Logger.Fatal("Error creating worker thread", ex)

        override this.GetConfig() = config.Force()

        override this.GetReplyFor(message, socket) = 
            let mutable response : ResponseMessage = null
             
            try
                let request = deserialize_with_protobuf<RequestMessage>(message);

                let result = 
                    dispatcher.Invoke(request.TargetService, 
                                        request.TargetMethod, 
                                        request.MethodParams )
                                
                if is_collection (result) then
                    let arrayRes = to_array result
                    response <- ResponseMessage(null, null, ReturnValueArray = arrayRes)
                else 
                    response <- ResponseMessage(result, null)
            with
                | :? TargetInvocationException as ex ->
                    let e = ex.InnerException 
                    response <- ResponseMessage(null, ExceptionInfo(e.GetType().Name, e.Message) )
                | ex -> 
                    response <- ResponseMessage(null, ExceptionInfo(ex.GetType().Name, ex.Message))

            try
                let buffer = serialize_with_protobuf(response)
                buffer
            with
                | ex -> 
                    serialize_with_protobuf ( ResponseMessage(null, ExceptionInfo(ex.GetType().Name, ex.Message)) )

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

        let sentCounter = PerfCounterRegistry.Get(PerfCounters.NumberOfRequestsSent)
        let receivedCounter = PerfCounterRegistry.Get(PerfCounters.NumberOfResponseReceived)
        let elapsedCounter = PerfCounterRegistry.Get(PerfCounters.AverageRequestTime)
        let baseElapsedCounter = PerfCounterRegistry.Get(PerfCounters.BaseRequestTime)

        let config = lazy
                        let parts = endpoint.Split(':')
                        ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        override this.GetConfig() = config.Force()

        override this.Timeout with get() = 7500

        override this.InternalGet(socket) =
            let watch = new Stopwatch()

            watch.Start()

            socket.Send(serialize_with_protobuf(message))
    
            sentCounter.Increment() |> ignore

            let bytes = socket.Recv(ZSocket.InfiniteTimeout)
            
            watch.Stop()

            if bytes <> null then
                receivedCounter.Increment() |> ignore

                elapsedCounter.IncrementBy(watch.ElapsedTicks) |> ignore
                baseElapsedCounter.Increment() |> ignore

            deserialize_with_protobuf<ResponseMessage>(bytes)

    type RemoteRouter() =
        let routes = Dictionary<string, string>()

        member this.ParseRoutes(config:IConfiguration) =
            for child in config.Children do
                routes.Add(child.Attributes.["assembly"], child.Attributes.["address"])

        member this.GetEndpoint(assembly:Assembly) =
            let overriden = CallContext.GetData("0mq.facility.endpoint") :?> string

            if String.IsNullOrEmpty(overriden) then routes.[assembly.GetName().Name] else overriden

        member this.ReRoute(assembly: string, address: string) =
            routes.[assembly] <- address

    type AlternativeRouteContext(route: string) =
        do
           CallContext.SetData("0mq.facility.endpoint", route)

        interface IDisposable with
            member x.Dispose() =
                CallContext.SetData("0mq.facility.endpoint", null)

        static member For(r: string) =
            (new AlternativeRouteContext(r)) :> IDisposable


    type RemoteRequestInterceptor(zContextAccessor:ZContextAccessor, router:RemoteRouter) =
        static let logger = log4net.LogManager.GetLogger(typeof<RemoteRequestInterceptor>)

        interface IInterceptor with
            
            member this.Intercept(invocation) =
                let stopwatch = System.Diagnostics.Stopwatch()
                
                if logger.IsDebugEnabled then
                    stopwatch.Start()

                try
                    if invocation.TargetType <> null then
                        invocation.Proceed()
                    else
                        let args = 
                            serialize_parameters (invocation.Arguments) (invocation.Method.GetParameters())

                        let request = RequestMessage(invocation.Method.DeclaringType.AssemblyQualifiedName, invocation.Method.Name, args)
                        let endpoint = router.GetEndpoint(invocation.Method.DeclaringType.Assembly)

                        let request = RemoteRequest(zContextAccessor, request, endpoint)
                        let response = request.Get()

                        if response.ExceptionInfo <> null then
                            let msg = sprintf "Remote server threw %s with message %s" (response.ExceptionInfo.Typename) (response.ExceptionInfo.Message)
                            raise (new Exception(msg))

                        if invocation.Method.ReturnType <> typeof<Void> then
                            invocation.ReturnValue <- 
                                if response.ReturnValue <> null 
                                then response.ReturnValue
                                else 
                                    if response.ReturnValueArray <> null then
                                        if invocation.Method.ReturnType.IsArray then
                                            let arrayElemType = invocation.Method.ReturnType.GetElementType()
                                            (make_strongly_typed_array arrayElemType (response.ReturnValueArray)) :> obj
                                        else
                                            let itemType = invocation.Method.ReturnType.GetGenericArguments().[0]
                                            (make_strongly_typed_enumerable itemType (response.ReturnValueArray))
                                    else null
                                    
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

    [<AllowNullLiteralAttribute>]
    type Reaper(zContextAccessor:ZContextAccessor) =
        static let logger = log4net.LogManager.GetLogger(typeof<Reaper>)

        let dispose() = 
            try
                logger.Info("Disposing ZeroMQ Facility...")

                let pool = SocketManager.Instance.Value

                pool.Dispose()

                zContextAccessor.Current.Dispose()

                logger.Info("Disposed ZeroMQ Facility.")
             with
                | ex -> logger.Error("Error disponsing ZeroMQ Facility components", ex)

        interface IDisposable with
            
            member this.Dispose() = dispose()
