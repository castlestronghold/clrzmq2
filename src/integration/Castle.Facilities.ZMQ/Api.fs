namespace Castle.Facilities.ZMQ

open ZMQ.Extensions
open System
open Castle.Facilities.Startable
open Castle.MicroKernel.Facilities
open Castle.MicroKernel.Registration
open Castle.Facilities.ZMQ.Internals

    type ZeroMQFacility() =
        inherit AbstractFacility()

        member this.setup_server() =
            let listener = Component.For<RemoteRequestListener>().Parameters(Parameter.ForKey("bindAddress").Eq(base.FacilityConfig.Attributes.["listen"]))
            base.Kernel.Register(listener) |> ignore

        member this.setup_client() =
            let router = base.Kernel.Resolve<RemoteRouter>()

            router.ParseRoutes(base.FacilityConfig.Children.["endpoints"])

        override this.Init() =
            if not (base.Kernel.GetFacilities() |> Seq.exists (fun f -> f.GetType() = typeof<StartableFacility>)) then
                base.Kernel.AddFacility<StartableFacility>() |> ignore

            base.Kernel.Register(Component.For<ZContextAccessor>(),
                                 Component.For<RemoteRouter>(),
                                 Component.For<Dispatcher>(),
                                 Component.For<RemoteRequestInterceptor>().LifeStyle.Transient) |> ignore

            let isServer = not (String.IsNullOrEmpty(base.FacilityConfig.Attributes.["listen"]))

            base.Kernel.ComponentModelBuilder.AddContributor(new RemoteRequestInspector())

            if isServer then
                this.setup_server()

            if base.FacilityConfig.Children.["endpoints"] <> null then
                this.setup_client()