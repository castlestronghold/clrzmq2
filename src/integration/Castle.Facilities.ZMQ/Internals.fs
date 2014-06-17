﻿namespace Castle.Facilities.ZMQ.Internals

    open ZMQ
    open ZMQ.Extensions2
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
    open System.Runtime.Remoting.Messaging


    type Dispatcher(kernel:IKernel) =
        static let logger = log4net.LogManager.GetLogger(typeof<Dispatcher>)
        
        member this.Invoke(target:string, methd:string, parms: ParamTuple array, meta: string array) = 

            let targetType = resolvedType(target)

            let instance = kernel.Resolve(targetType)

            let methodBase : MethodInfo = 
                if meta <> null 
                then
                    let methodMeta = deserialize_method_meta meta  
                    targetType.GetMethod(methd, BindingFlags.Instance ||| BindingFlags.Public, null, methodMeta, null)
                else targetType.GetMethod(methd, BindingFlags.Instance ||| BindingFlags.Public)

            let methodMeta = 
                methodBase.GetParameters() 
                |> Array.map (fun p -> p.ParameterType)

            let args = deserialize_params parms methodMeta
            // let args = deserialize_params parms (methodBase.GetParameters() |> Array.map (fun p -> p.ParameterType))

            let result = methodBase.Invoke(instance, args)
            (result, methodBase.ReturnType)


    type RemoteRequestListener(bindAddress:String, workers:Int16, zContextAccessor:ZContextAccessor, dispatcher:Dispatcher) =
        inherit BaseListener(zContextAccessor)

        // let mutable pool:WorkerPool = null

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
            // let response : ResponseMessage = null
            
            let response = 
                try
                    
                    let request = deserialize_with_protobuf<RequestMessage>(message);

                    try
                        let result = 
                            dispatcher.Invoke(request.TargetService, request.TargetMethod, request.Params, request.ParamTypes)
                        
                        build_response (fst result) (snd result)
                    with
                        | :? TargetInvocationException as ex ->
                            let e = ex.InnerException 
                            base.Logger.Error("Error executing remote invocation " + request.TargetService + "." + request.TargetMethod, e)
                            build_response_with_exception (e.GetType().Name) e.Message
                        | ex -> 
                            base.Logger.Error("Error executing remote invocation " + request.TargetService + "." + request.TargetMethod, ex)
                            build_response_with_exception (ex.GetType().Name) ex.Message
                with
                    | ex -> 
                        base.Logger.Error("Error executing remote invocation", ex)
                        build_response_with_exception (ex.GetType().Name) ex.Message

            try
                let buffer = serialize_with_protobuf(response)
                buffer
            with
                | ex -> 
                    serialize_with_protobuf ( ResponseMessage(null, null, ExceptionInfo(ex.GetType().Name, ex.Message)) )


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

        override this.Timeout with get() = 30 * 1000

        override this.InternalGet(socket) =
            socket.Send(serialize_with_protobuf(message))
    
            PerfCounters.IncrementSent ()

            let bytes = socket.Recv(ZSocket.InfiniteTimeout)
            
            if bytes <> null then
                PerfCounters.IncrementRcv ()
//                elapsedCounter.IncrementBy(watch.ElapsedTicks) |> ignore
//                baseElapsedCounter.Increment() |> ignore

            if bytes = null then
                let m = "Remote call took too long to respond. Is the server up? " + (config.Value.ToString())
                ResponseMessage(null, null, ExceptionInfo("Timeout", m))
            else
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
                        let pInfo = invocation.Method.GetParameters()
                        let pTypes = pInfo |> Array.map (fun p -> p.ParameterType)
                        let args = 
                            serialize_parameters (invocation.Arguments) pTypes 

                        let methodMeta = serialize_method_meta pInfo

                        let request = RequestMessage(invocation.Method.DeclaringType.AssemblyQualifiedName, 
                                                     invocation.Method.Name, args, methodMeta)
                        let endpoint = router.GetEndpoint(invocation.Method.DeclaringType.Assembly)

                        let request = RemoteRequest(zContextAccessor, request, endpoint)
                        let response = request.Get()

                        if response.ExceptionInfo <> null then
                            let msg = "Remote server threw " + (response.ExceptionInfo.Typename) + " with message " + (response.ExceptionInfo.Message)
                            raise (new Exception(msg))

                        else if invocation.Method.ReturnType <> typeof<Void> then
                            invocation.ReturnValue <- deserialize_reponse response invocation.Method.ReturnType
                                    
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
