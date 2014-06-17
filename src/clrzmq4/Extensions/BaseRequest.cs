namespace ZMQ.Extensions2
{
	using System.Threading.Tasks;

	public abstract class BaseRequest
	{
		protected readonly log4net.ILog Logger;

		protected BaseRequest(ZContextAccessor zContextAccessor)
		{
			ContextAccessor = zContextAccessor;

			Logger = log4net.LogManager.GetLogger(this.GetType());
		}

		protected ZContextAccessor ContextAccessor { get; set; }

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
					socket.Connect(config.Transport, config.Ip, config.Port /*, Timeout */);

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