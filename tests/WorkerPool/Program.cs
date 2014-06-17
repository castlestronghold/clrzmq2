using System;
using System.Text;
using System.Threading;

namespace WorkerPool
{
	using System.Runtime.InteropServices;
	using ZMQ.Extensions2;

//	[Serializable]
//	internal class Message
//	{
//		private readonly string msg;
//
//		public Message(string msg)
//		{
//			this.msg = msg;
//		}
//
//		public string Msg
//		{
//			get { return msg; }
//		}
//	}

	internal class Program
	{
		private static ZContext _context = new ZContext();

		private static int _counter = 1;

		private static void WorkerReply()
		{
			var threadNum = Interlocked.Increment(ref _counter);

			using (ZSocket receiver = new ZSocket(_context, SocketType.REP))
			{
				receiver.Connect("inproc://workers");
				while (true)
				{
					string message = receiver.Recv(Encoding.UTF8);

					if (message != null)
					{
						Console.WriteLine("Thread " + threadNum + " received. Sending...");
					}

					Thread.Sleep(1000);
					receiver.Send("World", Encoding.UTF8);
				}
			}
		}

		private static void Server()
		{
			var pool = new WorkerPool(_context, "tcp://*:5555", "inproc://workers", WorkerReply, 30);
			pool.Start();

			Thread.CurrentThread.Join();
		}

		public static void Request()
		{
			using (var socket = new ZSocket(_context, SocketType.REQ))
			{
				socket.Connect("tcp://localhost:5555");
				const string request = "Hello";
				for (int requestNbr = 0; requestNbr < 10; requestNbr++)
				{
					Console.WriteLine("Sending request {0}...", requestNbr);
					
					socket.Send(request, Encoding.UTF8);

					var reply = socket.Recv(Encoding.UTF8);
					
					Console.WriteLine("Received reply {0}: {1}", requestNbr, reply);
				}
			}
		}

		private static void Main(string[] args)
		{
			var server = new Thread(Server) { IsBackground = true };
			server.Start();

			var clientThreads = new Thread[5];
			for (int count = 0; count < clientThreads.Length; count++)
			{
				clientThreads[count] = new Thread(Request) { IsBackground = true };
				clientThreads[count].Start();
			}
			Console.ReadLine();
			server.Abort();
			foreach (Thread client in clientThreads)
			{
				client.Abort();
			}

			_context.Dispose();

			Console.WriteLine("Finished");
		}
	}
}
