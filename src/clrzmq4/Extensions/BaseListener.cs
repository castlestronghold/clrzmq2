namespace ZMQ.Extensions2
{
	using System;
	using System.Diagnostics;
	using System.Threading;

	public abstract class BaseListener : IDisposable
	{
		protected readonly log4net.ILog Logger;

//		protected static PerformanceCounter receivedCounter = PerfCounterRegistry.Get(PerfCounters.NumberOfRequestsReceived);
//		protected static PerformanceCounter sentCounter = PerfCounterRegistry.Get(PerfCounters.NumberOfResponseSent);
//		protected static PerformanceCounter timerReplyCounter = PerfCounterRegistry.Get(PerfCounters.AverageReplyTime);
//		protected static PerformanceCounter baseReplyCounter = PerfCounterRegistry.Get(PerfCounters.BaseReplyTime);

		private Thread _thread;
		private volatile bool _disposed;

		private ZSocket _socket;

		protected BaseListener(ZContextAccessor zContextAccessor)
		{
			ContextAccessor = zContextAccessor;

			this.Logger = log4net.LogManager.GetLogger(this.GetType());
		}

		protected ZContextAccessor ContextAccessor { get; set; }

		protected abstract ZConfig GetConfig();

		protected abstract byte[] GetReplyFor(byte[] request);

		public virtual void Start()
		{
			Logger.Debug("Starting " + GetType().Name);

			try
			{
				_thread = new Thread(Worker)
				         	{
				         		IsBackground = true,
				         		Name = "Worker thread for " + GetType().Name
				         	};

				_thread.Start();
			}
			catch (System.Exception e)
			{
				Logger.Error("Error starting " + GetType().Name, e);
			}
		}

		private void Worker()
		{
			try
			{
				var config = GetConfig();

				_socket = this.ContextAccessor.SocketFactory(SocketType.REP);

				_socket.Bind(config.Transport, config.Ip, config.Port);

				Logger.InfoFormat("Binding {0} on {1}:{2}", GetType().Name, config.Ip, config.Port);

				AcceptAndHandleMessage();
			}
			catch (System.Exception e)
			{
				Logger.Fatal("Error on " + GetType().Name + " background thread", e);
			}
			finally
			{
				CloseSocket();
			}
		}

		protected void AcceptAndHandleMessage()
		{
			try
			{
				_socket.RcvReady += (sender, args) =>
				{
					var bytes = _socket.Recv();
					byte[] reply = null;

					try
					{
						if (bytes == null)
							reply = new byte[0];
						else
							reply = GetReplyFor(bytes);
					}
					catch (Exception e)
					{
						Logger.Error("Error getting reply.", e);
					}
					finally
					{
						_socket.Send(reply ?? new byte[0]);
//								sentCounter.Increment();
					}
				};

				while (!_disposed)
				{
					_socket.DoPoll(1000);
				}
			}
			catch (Exception e)
			{
				Logger.Fatal("Error on working thread", e);
			}
		}

		public void Stop()
		{
			Dispose();
		}

		private void CloseSocket()
		{
			try
			{
				if (_socket == null) return;

				_socket.Dispose();
				_socket = null;
			}
			catch (Exception e)
			{
				Logger.Warn("Error closing socket.", e);
			}
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;

			CloseSocket();

			Logger.Info("Disposing " + GetType().Name);

			if (_thread != null)
			{
                // _thread.Abort(); // cr: same comment
			    _thread = null;
			}
		}
	}
}