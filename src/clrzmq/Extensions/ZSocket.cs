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
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SocketManager));
		private static readonly SocketManager socketManager = SocketManager.Instance.Value;

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
			socket = socketManager.Connect(Socket.BuildUri(transport, address, port), Type);
		}

		public virtual void Connect(string uri)
		{
			socket = socketManager.Connect(uri, Type);
		}

		public virtual void Bind(Transport transport, string address, uint port)
		{
			if (socket == null)
			{
				socket = new Socket(Type);

				socketManager.Registry(socket);
			}

			socket.Bind(Socket.BuildUri(transport, address, port));
		}

		public virtual void Dispose()
		{
			try
			{
				if (socket != null)
					socketManager.ReturnOrDispose(socket, Type);
			}
			catch (Exception e)
			{
				logger.Error("Error disposing ZSocket.", e);
			}
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
				socket = SocketManager.CreateSocket(socket.Address, Type);
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

	public class SocketManager : IDisposable
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SocketManager));

		public static Lazy<SocketManager> Instance = new Lazy<SocketManager>(() => new SocketManager());

		private readonly Dictionary<SocketType, ConcurrentDictionary<string, ConcurrentQueue<Socket>>> elasticPoll = 
			new Dictionary<SocketType, ConcurrentDictionary<string, ConcurrentQueue<Socket>>>();

		private readonly List<Socket> listeners = new List<Socket>();

		private bool disposed;

		public SocketManager()
		{
			foreach (var s in Enum.GetNames(typeof(SocketType)))
			{
				var type = (SocketType)Enum.Parse(typeof(SocketType), s);

				if (!elasticPoll.ContainsKey(type))
					elasticPoll.Add(type, new ConcurrentDictionary<string, ConcurrentQueue<Socket>>());
			}
		}

		public Socket Connect(string uri, SocketType socketType)
		{
			EnsureNotDisposed();

			var queues = elasticPoll[socketType];

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

		public void ReturnOrDispose(Socket socket, SocketType socketType)
		{
			EnsureNotDisposed();

			var queues = elasticPoll[socketType];

			ConcurrentQueue<Socket> q;

			if (queues != null && queues.TryGetValue(socket.Address, out q))
				q.Enqueue(socket);
			else
				Close(socket);
		}

		private void Close(Socket socket)
		{
			if (listeners.Contains(socket))
				listeners.Remove(socket);

			socket.Dispose();
		}

		private void EnsureNotDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("SocketManager is already disposed.");
		}

		public void Dispose()
		{
			if (disposed) return;

			disposed = true;

			foreach (var listener in listeners)
			{
				logger.Warn("Disposing listeners...");

				try
				{
					listener.Dispose();
				}
				catch (System.Exception ex)
				{
					logger.Warn("Error disposing socket", ex);
				}
			}

			foreach (var socketGroup in elasticPoll.Values)
			{
				var group = socketGroup;

				foreach (var sockets in group.Values)
				{
					while (sockets.Count > 0)
					{
						try
						{
							Socket s;

							logger.Warn("Disposing req...");

							if (sockets.TryDequeue(out s))
								s.Dispose();
						}
						catch (System.Exception ex)
						{
							logger.Warn("Error disposing socket", ex);
						}
					}
				}
				
			}
		}

		public void Registry(Socket listener)
		{
			listeners.Add(listener);
		}
	}
}