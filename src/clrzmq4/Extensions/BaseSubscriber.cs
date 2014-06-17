namespace ZMQ.Extensions2
{
	using System;
	using System.Security;
	using System.Threading;

	public abstract class BaseSubscriber<T> : IDisposable
	{
		protected readonly log4net.ILog Logger;

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

			Logger = log4net.LogManager.GetLogger(this.GetType());
		}

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
				Logger.Error("Error starting subscriber.", e);
			}
		}

		// [HandleProcessCorruptedStateExceptions, SecurityCritical]
		[SecurityCritical]
		private void Worker()
		{
			try
			{
				_socket = _zContextAccessor.SocketFactory(SocketType.SUB);

				_socket.Connect(Transport.TCP, _address, _port);
				_socket.Subscribe(string.Empty);

				while (true)
				{
					_socket.RcvReady += (sender, args) =>
					{
						var raw = _socket.Recv();

						if (raw != null && raw.Length != 0)
						{
							var message = Deserialize(raw);
							OnReceived(message);
						}
					};

					_socket.DoPoll(1000);

					if (_disposed) break;
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
				CleanUp();
			}
		}

		protected abstract void OnReceived(T message);

		protected abstract T Deserialize(byte[] bytes);

		[SecurityCritical]
		public void Dispose()
		{
			if (_disposed) return;

			if (_thread != null && _thread.IsAlive)
			{
				_disposed = true;
				Thread.MemoryBarrier();
			}
			else
			{
				CleanUp();
			}
		}

		private void CleanUp()
		{
			if (_socket != null)
				_socket.Dispose();
		}
	}
}