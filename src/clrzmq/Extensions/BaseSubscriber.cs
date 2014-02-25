namespace ZMQ.Extensions
{
	using System;
	using System.Runtime.ExceptionServices;
	using System.Security;
	using System.Threading;
	using Castle.Core.Logging;

	public abstract class BaseSubscriber<T> : IDisposable
	{
		private readonly ZContextAccessor _zContextAccessor;
		private readonly string _address;
		private readonly uint _port;

		private Thread _thread;
		private ZSocket _socket;
		private volatile bool _disposed;

		protected BaseSubscriber(ZContextAccessor zContextAccessor, string address, int port)
		{
			this._zContextAccessor = zContextAccessor;
			this._address = address;
			this._port = (uint)port;

			Logger = NullLogger.Instance;
		}

		public ILogger Logger { get; set; }

		public void Start()
		{
			try
			{
				Logger.Info("Starting " + GetType().Name);

				_thread = new Thread(Worker)
				{
					IsBackground = true,
					Name = "Subscriber " + this.GetType().Name
				};

				_thread.Start();
			}
			catch (System.Exception e)
			{
				Logger.Error("Error on starting subscriber.", e);
			}
		}

		// [HandleProcessCorruptedStateExceptions, SecurityCritical]
		[SecurityCritical]
		private void Worker()
		{
			try
			{
				_socket = _zContextAccessor.SocketFactory(SocketType.SUB);

				_socket.Connect(Transport.TCP, _address, _port, timeout: 10000);
				_socket.Subscribe(string.Empty);

				while (true)
				{
					var raw = _socket.Recv(1000);

					if (raw == null || raw.Length == 0)
					{
						if (_disposed) break;

						continue;
					}

					var message = Deserialize(raw);

					OnReceived(message);
				}
			}
			catch (System.AccessViolationException e)
			{
				Logger.Fatal("Error on subscriber background thread.", e);
			}
			catch (System.Runtime.InteropServices.SEHException e)
			{
				Logger.Fatal("Error on subscriber background thread.", e);
			}
			catch (System.Exception e)
			{
				Logger.Fatal("Error on subscriber background thread.", e);
			}
			finally
			{
				_socket.Dispose();
			}
		}

		protected abstract void OnReceived(T message);

		protected abstract T Deserialize(byte[] bytes);

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			Thread.MemoryBarrier();
		}
	}
}