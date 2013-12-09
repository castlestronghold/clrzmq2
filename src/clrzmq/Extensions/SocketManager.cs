namespace ZMQ.Extensions
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Runtime.ExceptionServices;
	using System.Security;

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

		public Socket Connect(string uri, SocketType socketType, int timeout = -1)
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
				s = CreateSocket(uri, socketType, timeout);
			}

			return s;
		}

		public static Socket CreateSocket(string uri, SocketType socketType, int timeout)
		{
			var s = new Socket(socketType);

			if (timeout > 0)
				s.RecvTimeout = timeout;

			s.SetSockOpt(SocketOpt.LINGER, 0);

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

		[HandleProcessCorruptedStateExceptions, SecurityCritical]
		public void Dispose()
		{
			if (disposed) return;

			try
			{
				disposed = true;

				foreach (var listener in listeners)
				{
					logger.Warn("Disposing listeners...");

					try
					{
						listener.Dispose();
					}
					catch (Exception ex)
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
							catch (Exception ex)
							{
								logger.Warn("Error disposing socket", ex);
							}
						}
					}
				
				}
			}
			catch
			{
			}
		}

		public void Registry(Socket listener)
		{
			listeners.Add(listener);
		}
	}
}