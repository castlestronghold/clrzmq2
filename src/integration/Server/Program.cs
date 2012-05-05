namespace Server
{
	using System;
	using Castle.Facilities.ZMQ;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;

	class Program
	{
		static void Main(string[] args)
		{
			var container = new WindsorContainer(new XmlInterpreter());

			//container.Resolve<RemoteRequestListener>();

			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}
	}
}
