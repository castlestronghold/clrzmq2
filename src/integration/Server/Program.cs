namespace Server
{
	using System;
	using Castle.Facilities.ZMQ;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;

	class Program
	{
		static void Main(string[] args)
		{
			var container = new WindsorContainer(new XmlInterpreter());

			container.Register(Component.For<IRemoteService>().ImplementedBy<RemoteServiceImpl>());
			//container.Resolve<RemoteRequestListener>();

			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}
	}
}
