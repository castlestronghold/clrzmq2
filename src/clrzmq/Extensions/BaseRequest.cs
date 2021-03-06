﻿namespace ZMQ.Extensions
{
	using System;
	using System.Threading.Tasks;
	using Castle.Core.Logging;
	using ZMQ;

	public abstract class BaseRequest<T> : BaseRequest
	{
		protected BaseRequest(ZContextAccessor zContextAccessor) : base(zContextAccessor)
		{
		}

		protected override void InternalInvoke(ZSocket socket)
		{
			InternalGet(socket);
		}

		protected abstract T InternalGet(ZSocket socket);

		public virtual T Get()
		{
			try
			{
				var config = GetConfig();

				using (var socket = ContextAccessor.SocketFactory(SocketType.REQ))
				{
					socket.Connect(config.Transport, config.Ip, config.Port, Timeout);

					Logger.DebugFormat("Connecting {0} on {1}:{2}", GetType().Name, config.Ip, config.Port);

					return InternalGet(socket);
				}
			}
			catch (System.Exception e)
			{
				Logger.Error("Error invoking " + GetType().Name, e);

				throw;
			}
			catch
			{
				Logger.Fatal("Possible SEH Exception");

				throw new InvalidOperationException("Possible SEH Exception");
			}
		}
	}

	public abstract class BaseRequest
	{
		protected BaseRequest(ZContextAccessor zContextAccessor)
		{
			ContextAccessor = zContextAccessor;
			
			Logger = NullLogger.Instance;
		}

		protected ZContextAccessor ContextAccessor { get; set; }

		public ILogger Logger { get; set; }

		protected abstract ZConfig GetConfig();

		protected abstract void InternalInvoke(ZSocket socket);

		protected virtual int Timeout
		{
			get { return -1; }
		}

		public virtual void Invoke()
		{
			try
			{
				var config = GetConfig();

				using (var socket = ContextAccessor.SocketFactory(SocketType.REQ))
				{
					socket.Connect(config.Transport, config.Ip, config.Port, Timeout);

					Logger.DebugFormat("Connecting {0} on {1}:{2}", GetType().Name, config.Ip, config.Port);

					InternalInvoke(socket);
				}
			}
			catch (System.Exception e)
			{
				Logger.Error("Error invoking " + GetType().Name, e);
			}
		}

		public void Async()
		{
			Task.Factory.StartNew(Invoke);
		}
	}
}