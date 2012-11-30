using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
	using System;
	using System.Threading;
	using Castle.Facilities.ZMQ;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;
	using Server;

	class Program
	{
		static ManualResetEventSlim wait = new ManualResetEventSlim(false);

		static void Main(string[] args)
		{
			var container = new WindsorContainer(new XmlInterpreter());

			//container.Resolve<RemoteRequestListener>();

			container.Register(Component.For<IRemoteService>());

			var t = new Thread[10];

			for (var i = 0; i < t.Length; i++)
			{
				t[i] = GetThread(container);
				t[i].Start();
			}

			Console.WriteLine("Press any to start");
			Console.ReadKey();

			wait.Set();

			Console.WriteLine("Press any key to exit");
			Console.ReadKey();
		}

		private static Thread GetThread(WindsorContainer container)
		{
			return new Thread(() =>
			{
				wait.Wait();

				while (true)
				{
					try
					{
						var remoteService = container.Resolve<IRemoteService>();

						remoteService.Foo();

						Console.WriteLine("sum:" + remoteService.WeirdSum(1, 2));

						try
						{
							remoteService.Error();
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
						}

						Thread.Sleep(1000);
					}
					catch(System.Runtime.InteropServices.SEHException e)
					{
						Console.WriteLine(e);	
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			});
		}
	}
}
