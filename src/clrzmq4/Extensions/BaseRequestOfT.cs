namespace ZMQ.Extensions2
{
	using System;

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
					socket.Connect(config.Transport, config.Ip, config.Port/*, Timeout */);

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
}