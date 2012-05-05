using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
	using System;
	using Castle.Facilities.ZMQ;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;
	using Server;

	class Program
	{
		static void Main(string[] args)
		{
			var container = new WindsorContainer(new XmlInterpreter());

			//container.Resolve<RemoteRequestListener>();

			container.Register(Component.For<IRemoteService>());

			var remoteService = container.Resolve<IRemoteService>();

			remoteService.Foo();
			Console.WriteLine("sum:" + remoteService.WeirdSum(1, 2));

			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}
	}
}
