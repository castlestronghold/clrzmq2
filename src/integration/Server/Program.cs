namespace Server
{
	using System;
	using System.Collections.Generic;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;

	class Program
	{
		public static List<byte[]> retainer;

		static void Main(string[] args)
		{
			retainer = new List<byte[]>();

			for (var i = 0; i < 100000; i++)
			{
				retainer.Add(new byte[5500]);
			}

			var container = new WindsorContainer(new XmlInterpreter());

			container.Register(Component.For<IRemoteService>().ImplementedBy<RemoteServiceImpl>());
			//container.Resolve<RemoteRequestListener>();

			Console.WriteLine("Press any key to exit");
			Console.ReadLine();

			container.Dispose();
		}
	}
}
