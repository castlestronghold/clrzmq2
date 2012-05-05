namespace Castle.Facilities.ZMQ

open ZMQ
open ZMQ.Extensions
open System
open Castle.Core
open Castle.DynamicProxy
open Castle.Core.Interceptor
open Castle.Windsor
open Castle.MicroKernel
open Castle.MicroKernel.Facilities
open Castle.MicroKernel.ModelBuilder.Inspectors
open Castle.MicroKernel.Registration

    type RemoteRequestListener(bindAddress:String, zContextAccessor:ZContextAccessor, kernel:IKernel) =
        inherit BaseListener(zContextAccessor)

        override this.GetConfig() = 
            let parts = bindAddress.Split(':')

            ZConfig(parts.[0], Convert.ToUInt32(parts.[1]), Transport.TCP)

        override this.GetReplyFor(request, socket) = [||]

        interface IStartable with
            member this.Start() = base.Start()
            member this.Stop() = base.Stop()

    type RemoteRequestInterceptor() =

        interface IInterceptor with
            
            member this.Intercept(invocation) = ()

        interface IOnBehalfAware with

            member this.SetInterceptedComponentModel(target) = ()

    type RemoteRequestInspector() =
        inherit MethodMetaInspector()
        
        override this.ProcessModel(kernel, model) = ()

        override this.ObtainNodeName() = "remote-interceptor"

    type ZeroMQFacility() =
        inherit AbstractFacility()

        member this.setup_server() =
            let listener = Component.For<RemoteRequestListener>().Parameters(Parameter.ForKey("bindAddress").Eq(base.FacilityConfig.Attributes.["listen"]))
            base.Kernel.Register(listener) |> ignore

        member this.setup_client() = ()

        override this.Init() =
            base.Kernel.Register(Component.For<ZContextAccessor>(),
                                 Component.For<RemoteRequestInterceptor>().LifeStyle.Transient) |> ignore

            base.Kernel.ComponentModelBuilder.AddContributor(new RemoteRequestInspector());

            if not (String.IsNullOrEmpty(base.FacilityConfig.Attributes.["listen"])) then
                this.setup_server()

            if base.FacilityConfig.Children.["endpoints"] <> null then
                this.setup_client()