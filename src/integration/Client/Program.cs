using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
	using System;
	using System.Diagnostics;
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

			var watch = Stopwatch.StartNew();

			wait.Set();

			for (var i = 0; i < t.Length; i++)
			{
				t[i].Join();
			}

			Console.WriteLine("Took: " + watch.ElapsedMilliseconds +  ". Press any key to exit");
			Console.ReadKey();
		}

		private static Thread GetThread(WindsorContainer container)
		{
			return new Thread(() =>
			{
				wait.Wait();

				Console.WriteLine("Iterating");

				for (var i = 0; i < 30; i++)
				{
					try
					{
						var remoteService = container.Resolve<IRemoteService>();

						Console.WriteLine("Foo...");

						remoteService.Foo();

						Console.WriteLine("sum:" + remoteService.WeirdSum(1, 2));

						var g = Guid.NewGuid();

						Debug.Assert(g == remoteService.Pair(g)); 

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
					catch (System.Runtime.InteropServices.SEHException e)
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
