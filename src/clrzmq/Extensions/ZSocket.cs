namespace ZMQ.Extensions
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Text;
	using ZMQ;
	using Exception = System.Exception;

	public class ZSocket : IDisposable
	{
		private static readonly ElasticPoll _elasticPoll = new ElasticPoll();

		public const int DefaultTimeout = 2000;

		private Socket socket;

		protected ZSocket()
		{
		}

		public ZSocket(SocketType type)
		{
			Type = type;
		}

		public SocketType Type { get; private set; }

		public virtual void Connect(Transport transport, string address, uint port)
		{
			socket = _elasticPoll.Connect(Socket.BuildUri(transport, address, port), Type);
		}

		public virtual void Connect(string uri)
		{
			socket = _elasticPoll.Connect(uri, Type);
		}

		public virtual void Bind(Transport transport, string address, uint port)
		{
			if (socket == null)
				socket = new Socket(Type);

			socket.Bind(Socket.BuildUri(transport, address, port));
		}

		public virtual void Dispose()
		{
			if (socket != null)
				_elasticPoll.Return(socket, Type);
		}

		public virtual void Send(string message, Encoding encoding)
		{
			SafeSend(() => socket.Send(message, encoding));
		}

		public virtual void SendMore(string key)
		{
			SafeSend(() => socket.SendMore(key, Encoding.UTF8));
		}

		public virtual void Send(byte[] message)
		{
			SafeSend(() => socket.Send(message));
		}

		//Crazy Pirate Pattern
		private void SafeSend(Action sender)
		{
			var failed = false;

			try
			{
				sender();
			}
			catch (ZMQ.Exception e)
			{
				failed = true;
				socket = ElasticPoll.CreateSocket(socket.Address, Type);
			}

			if (failed)
				sender();
		}

		public virtual byte[] Recv(int timeout = DefaultTimeout)
		{
			//();
			//System.Diagnostics.Debug.Print("Timeout: " + timeout + ". Is infinite: " + (timeout == int.MaxValue));

			if (timeout == int.MaxValue) 
				return socket.Recv();

			return socket.Recv(timeout);
		}

		public virtual string Recv(Encoding encoding, int timeout = DefaultTimeout)
		{
			if (timeout == int.MaxValue) 
				return socket.Recv(encoding);

			return socket.Recv(encoding, timeout);
		}

		public virtual void Subscribe(string filter)
		{
			socket.Subscribe(filter, Encoding.UTF8);
		}
	}

	public class ElasticPoll
	{
		private readonly Dictionary<SocketType, ConcurrentDictionary<string, ConcurrentQueue<Socket>>> map = 
			new Dictionary<SocketType, ConcurrentDictionary<string, ConcurrentQueue<Socket>>>();

		public ElasticPoll()
		{
			foreach (var s in Enum.GetNames(typeof(SocketType)))
			{
				var type = (SocketType)Enum.Parse(typeof(SocketType), s);

				if (!map.ContainsKey(type))
					map.Add(type, new ConcurrentDictionary<string, ConcurrentQueue<Socket>>());
			}
		}

		public Socket Connect(string uri, SocketType socketType)
		{
			var queues = map[socketType];

			ConcurrentQueue<Socket> q;

			if (!queues.TryGetValue(uri, out q))
			{
				q = new ConcurrentQueue<Socket>();

				if (!queues.TryAdd(uri, q))
				{
					q = queues[uri];
				}
			}

			Socket s;

			if (!q.TryDequeue(out s))
			{
				s = CreateSocket(uri, socketType);
			}

			return s;
		}

		public static Socket CreateSocket(string uri, SocketType socketType)
		{
			var s = new Socket(socketType);

			s.Connect(uri);

			return s;
		}

		public void Return(Socket socket, SocketType socketType)
		{
			var queues = map[socketType];

			ConcurrentQueue<Socket> q;

			if (queues != null && queues.TryGetValue(socket.Address, out q))
				q.Enqueue(socket);
			else
				socket.Dispose();
		}
	}
}