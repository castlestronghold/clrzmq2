namespace ZMQ.Extensions
{
	using System;
	using System.Collections.Concurrent;
	using System.Runtime.ExceptionServices;
	using System.Security;
	using System.Threading;
	using Castle.Core.Logging;

	public abstract class BasePublisher<T> : IDisposable
	{
		private readonly ZContextAccessor _zContextAccessor;
		private readonly string _address;
		private readonly uint _port;

		private readonly ManualResetEventSlim _waitHandle = new ManualResetEventSlim();
		private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

		private Thread _thread;
		private ZSocket _socket;
		private volatile bool _disposed;

		protected BasePublisher(ZContextAccessor zContextAccessor, string address, int port)
		{
			this._zContextAccessor = zContextAccessor;
			this._address = address;
			this._port = (uint)port;

			Logger = NullLogger.Instance;
		}

		public ILogger Logger { get; set; }

		protected virtual void Enqueue(T message)
		{
			_queue.Enqueue(message);

			_waitHandle.Set();
		}

		public virtual void Start()
		{
			try
			{
				_thread = new Thread(Worker)
				{
					IsBackground = true,
					Name = "Publisher Worker " + this.GetType().Name
				};

				_thread.Start();

				Logger.Info("Worker for Order Execution started");
			}
			catch (Exception e)
			{
				Logger.Error("Error starting Worker", e);
			}
		}

		private void Worker()
		{
			_socket = _zContextAccessor.SocketFactory(SocketType.PUB);

			try
			{
				Logger.Info("Biding socket on " + this._address + ":" + this._port);
				_socket.Bind(Transport.TCP, this._address, this._port);

				while (true)
				{
					_waitHandle.Wait();

					T message;

					if (!_queue.TryDequeue(out message))
					{
						if (_disposed) break;

						_waitHandle.Reset();
						continue;
					}

					Logger.Debug("Publishing message of " + message);

					_socket.Send(Serialize(message));
				}
			}
			catch (Exception e)
			{
				Logger.Error("Error on publisher Worker", e);
			}
			finally
			{
				CleanUp();
			}
		}

		protected abstract byte[] Serialize(T message);

		public virtual void Stop()
		{
			Dispose();
		}

		[SecurityCritical]
		public virtual void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			Thread.MemoryBarrier();

			if (_thread != null && _thread.IsAlive)
			{
				_waitHandle.Set(); // let thread finish it
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

			_waitHandle.Dispose();
		}
	}
}