namespace ZMQ.Extensions
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using Castle.Core.Logging;
	using ZMQ;
	using Exception = System.Exception;

	public abstract class BaseListener : IDisposable
	{
		private Thread thread;
		private bool disposed;

		private ZSocket socket;

		protected BaseListener(ZContextAccessor zContextAccessor)
		{
			ContextAccessor = zContextAccessor;
			
			Logger = NullLogger.Instance;
		}

		protected ZContextAccessor ContextAccessor { get; set; }

		public ILogger Logger { get; set; }

		protected abstract ZConfig GetConfig();

		protected abstract byte[] GetReplyFor(byte[] request, ZSocket socket);

		public virtual void Start()
		{
			Logger.Debug("Starting " + GetType().Name);

			try
			{
				thread = new Thread(Worker)
				         	{
				         		IsBackground = true,
				         		Name = "Worker thread for " + GetType().Name
				         	};

				thread.Start();
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

				socket = ContextAccessor.SocketFactory(SocketType.REP);

				socket.Bind(config.Transport, config.Ip, config.Port);

				Logger.InfoFormat("Binding {0} on {1}:{2}", GetType().Name, config.Ip, config.Port);

				AcceptAndHandleMessage(socket);
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

		protected void AcceptAndHandleMessage(ZSocket zSocket)
		{
			try
			{
				while (!disposed)
				{
					var watch = new Stopwatch();

					if (Logger.IsDebugEnabled)
						watch.Start();

					try
					{
						var bytes = zSocket.Recv(ZSocket.InfiniteTimeout);

						byte[] reply = null;

						try
						{
							reply = bytes == null ? new byte[0] : GetReplyFor(bytes, zSocket);
						}
						catch (Exception e)
						{
							Logger.Error("Error getting reply.", e);
						}
						finally
						{
							zSocket.Send(reply ?? new byte[0]);
						}
					}
					finally
					{
						if (Logger.IsDebugEnabled)
							Logger.Debug("Listener Recv Took: " + watch.ElapsedMilliseconds);
					}
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
				if (socket == null) return;

				socket.Dispose();
				socket = null;
			}
			catch (Exception e)
			{
				Logger.Warn("Error closing socket.", e);
			}
		}

		public void Dispose()
		{
			if (disposed) return;

			disposed = true;

			CloseSocket();

			Logger.Info("Disposing " + GetType().Name);

			if (thread != null)
			{
                thread.Abort(); // cr: same comment
			    thread = null;
			}
		}
	}
}