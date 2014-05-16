namespace ZMQ.Extensions
{
	using System;
	using System.Text;
	using ZMQ;
	using Exception = System.Exception;

	public class ZSocket : IDisposable
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(SocketManager));
		private static readonly SocketManager socketManager = SocketManager.Instance.Value;

		public const int InfiniteTimeout = -1;
		public const int DefaultTimeout = 300 * 1000;

		private Socket socket;
		private int timeout = -1;
		private bool disposed = false;

		protected ZSocket()
		{
		}

		public ZSocket(SocketType type)
		{
			Type = type;
		}

		public SocketType Type { get; private set; }

		public virtual void Connect(Transport transport, string address, uint port, int timeout)
		{
			this.timeout = timeout;
			socket = socketManager.Connect(Socket.BuildUri(transport, address, port), Type, timeout);
		}

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
				socket.SetSockOpt(SocketOpt.LINGER, 0);

				socketManager.Registry(socket);
			}

			socket.Bind(Socket.BuildUri(transport, address, port));
		}

		public virtual void Bind(string endpoint)
		{
			if (socket == null)
			{
				socket = new Socket(Type);
				socket.SetSockOpt(SocketOpt.LINGER, 0);
				socketManager.Registry(socket);
			}

			socket.Bind(endpoint);
		}

		public virtual void Dispose()
		{
			try
			{
				disposed = true;

				if (socket != null)
					socketManager.ReturnOrDispose(socket, Type);
			}
			catch (Exception e)
			{
				logger.Error("Error disposing ZSocket.", e);
			}
		}

		~ZSocket()
		{
			if (!disposed)
				Dispose();
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
				socket = SocketManager.CreateSocket(socket.Address, Type, timeout);
			}

			if (failed)
				sender();
		}

		public virtual byte[] Recv(int timeout = DefaultTimeout)
		{
			//();
			//System.Diagnostics.Debug.Print("Timeout: " + timeout + ". Is infinite: " + (timeout == int.MaxValue));

			if (IsInfinite(timeout)) 
				return socket.Recv();

			return socket.Recv(timeout);
		}

		private static bool IsInfinite(int timeout)
		{
			return timeout == int.MaxValue || timeout == InfiniteTimeout; //TODO: Refactor usages to use the infinite timeout constant
		}

		public virtual string Recv(Encoding encoding, int timeout = DefaultTimeout)
		{
			if (IsInfinite(timeout)) 
				return socket.Recv(encoding);

			return socket.Recv(encoding, timeout);
		}

		public virtual void Subscribe(string filter)
		{
			socket.Subscribe(filter, Encoding.UTF8);
		}
	}
}