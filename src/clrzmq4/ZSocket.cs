namespace ZMQ.Extensions2
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Concurrent;
	using System.Runtime.ExceptionServices;
	using System.Security;
	using System.Text;
	using fszmq;

	public class ZSocket : IDisposable
	{
		private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ZSocket));

		public const int InfiniteTimeout = -1;
		public const int DefaultTimeout = 300 * 1000;

		private readonly Socket _socket;
//		private readonly ZContext _context;
//		private readonly int _timeout;
		private Poll _pollIn;
		private Poll[] _pollitemsCache;

		private volatile bool _disposed;

		public ZSocket(ZContext context, SocketType type, int timeout = -1)
		{
//			this._context = context;
//			this._timeout = timeout;

			this._socket = context.Ctx.Socket((int)type);
			this._socket.SetOption(fszmq.ZMQ.LINGER, 0);

			if (timeout != -1)
			{
				this._socket.SetOption(fszmq.ZMQ.RCVTIMEO, timeout);
			}
		}

		public EventHandler RcvReady;

		public bool DoPoll(int timeout)
		{
			if (_pollIn == null)
			{
				_pollIn = _socket.AsPollIn((s) =>
				{
					var ev = this.RcvReady;
					if (ev != null)
					{
						ev(this, EventArgs.Empty);
					}
				});

				_pollitemsCache = new[] { _pollIn };
			}

			return PollingModule.DoPoll(timeout, _pollitemsCache);
		}

//		public virtual void Connect(Transport transport, string address, uint port, int timeout)
//		{
//			// this._timeout = timeout;
//			// socket = socketManager.Connect(Socket.BuildUri(transport, address, port), Type, timeout);
//		}

		public virtual void Connect(Transport transport, string address, uint port)
		{
			_socket.Connect(transport.ToString().ToLower() + "://" + address + ":" + port);
		}

		public virtual void Connect(string uri)
		{
			_socket.Connect(uri);
		}

		public virtual void Bind(Transport transport, string address, uint port)
		{
			_socket.Bind(transport.ToString().ToLower() + "://" + address + ":" + port);
		}

		public virtual void Bind(string endpoint)
		{
			_socket.Bind(endpoint);

//			if (socket == null)
//			{
//				socket = new Socket(Type);
//				socket.SetSockOpt(SocketOpt.LINGER, 0);
//				socketManager.Registry(socket);
//			}
//			socket.Bind(endpoint);
		}

		public virtual void Dispose()
		{
			try
			{
				_disposed = true;

				(_socket as IDisposable).Dispose();
				
				GC.SuppressFinalize(this);
			}
			catch (Exception e)
			{
				Logger.Error("Error disposing ZSocket", e);
			}
		}

		~ZSocket()
		{
			if (!_disposed)
			{
				Dispose();
			}
		}

		public virtual void Send(string message, Encoding encoding)
		{
			if (message == null) throw new ArgumentNullException("message");
			if (encoding == null) throw new ArgumentNullException("encoding");

			SafeSend(() => _socket.Send(encoding.GetBytes(message)));
		}

		public virtual void SendMore(string key)
		{
			if (key == null) throw new ArgumentNullException("key");

			SafeSend(() => _socket.SendMore(Encoding.UTF8.GetBytes(key)));
		}

		public virtual void Send(byte[] message)
		{
			if (message == null) throw new ArgumentNullException("message");
			if (message.Length == 0) throw new ArgumentException("Attempt to send zero length buffer", "message");

			SafeSend(() => _socket.Send(message));
		}

		public virtual byte[] Recv(/*int timeout = DefaultTimeout*/)
		{
			// if (IsInfinite(timeout))
			return _socket.Recv();

//			return _socket.Recv(timeout);
		}

		public virtual string Recv(Encoding encoding /*, int timeout = DefaultTimeout*/)
		{
			var buffer = this.Recv(/*timeout*/);
			if (buffer != null)
			{
				return encoding.GetString(buffer);
			}
			return null;
		}

		public virtual void Subscribe(string filter)
		{
			if (filter == null) throw new ArgumentNullException("filter");

			SocketModule.Subscribe(_socket, new[] { Encoding.UTF8.GetBytes(filter) });
		}


		//Crazy Pirate Pattern
		private void SafeSend(Action sender)
		{
//			var failed = false;
			try
			{
				sender();
			}
			catch (fszmq.ZMQError e)
			{
//				failed = true;
				Logger.Error("SafeSend error", e);
				throw;
				// socket = SocketManager.CreateSocket(socket.Address, Type, timeout);
			}

//			if (failed)
//				sender();
		}

//		private static bool IsInfinite(int timeout)
//		{
//			return timeout == InfiniteTimeout || timeout == int.MaxValue;
//		}
	}
}
