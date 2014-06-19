namespace ZMQ.Extensions2
{
	using System;
	using System.Text;
	using System.Threading;
	using fszmq;
	using Microsoft.FSharp.Core;


	public class ZSocket : IDisposable, IZSocket
	{
		private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ZSocket));

		public const int InfiniteTimeout = -1;
		public const int DefaultTimeout = 300 * 1000;

		private readonly Socket _socket;

		private int _timeout;
		private Poll _pollIn;
		private Poll[] _pollitemsCache;

		private volatile bool _disposed;

//		private static int Counter = 0;

		public ZSocket(ZContext context, SocketType type, int timeout = -1)
		{
//			Interlocked.Increment(ref Counter);

			this._socket = context.Ctx.Socket((int)type);
			this._socket.SetOption(fszmq.ZMQ.LINGER, 0);

			if (timeout != -1)
			{
				SetRecvTimeout(timeout);
			}
		}

		public void SetRecvTimeout(int timeout)
		{
			this._socket.SetOption(fszmq.ZMQ.RCVTIMEO, timeout);
			this._timeout = timeout;
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
		}

		public virtual void Dispose()
		{
			if (_disposed) return;

			try
			{
				_disposed = true;

				(_socket as IDisposable).Dispose();

//				var actual = Interlocked.Decrement(ref Counter);
//				Console.WriteLine("Sockets " + actual);

				GC.SuppressFinalize(this);
			}
			catch (Exception e)
			{
				Logger.Error("Error disposing ZSocket", e);
			}
		}

		~ZSocket()
		{
			Dispose();
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

			SafeSend(() => _socket.Send(message));
		}

		public virtual byte[] Recv(/*int timeout = DefaultTimeout*/)
		{
			try
			{
				return _socket.Recv();
			}
			catch (Exception ex)
			{
				Logger.Error("Error on Recv", ex);
				throw;
			}
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
			try
			{
				sender();
			}
			catch (fszmq.ZMQError e)
			{
				Logger.Error("SafeSend error", e);
				throw;
			}
		}

//		private static bool IsInfinite(int timeout)
//		{
//			return timeout == InfiniteTimeout || timeout == int.MaxValue;
//		}
	}
}
