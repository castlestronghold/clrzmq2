namespace ZMQ.Extensions2
{
	using System;
	using System.Threading;
	using fszmq;

	public abstract class Device : IDisposable
	{
		protected static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Device));

		private readonly Socket _frontend;
		private readonly Socket _backend;
		private volatile bool _isRunning;
		private bool _ownSockets = false;
		private Thread _running;

		protected Device(ZContext context, string front, string back)
		{
			if (context == null) throw new ArgumentNullException("context");
			if (string.IsNullOrEmpty(front)) throw new ArgumentNullException("front");
			if (string.IsNullOrEmpty(back)) throw new ArgumentNullException("back");

			_frontend = context.Ctx.Router();
			_backend = context.Ctx.Dealer();
			_ownSockets = true;

			_frontend.Bind(front);
			_backend.Bind(back);
		}

		protected Device(Socket frontend, Socket backend)
		{
			if (frontend == null) throw new ArgumentNullException("frontend");
			if (backend == null) throw new ArgumentNullException("backend");

			_frontend = frontend;
			_backend = backend;
		}

		public void Start()
		{
			_isRunning = true;

			_running = new Thread(PollRunner) { IsBackground = true };
			_running.Start();
		}

		public void Stop()
		{
			_isRunning = false;
		}

		public void Dispose()
		{
			this.Stop();

			if (_ownSockets)
			{
				(_frontend as IDisposable).Dispose();
				(_backend as IDisposable).Dispose();
			}
		}

		private void PollRunner()
		{
			var item1 = _frontend.AsPollIn((s) => InternalRelay(s, _backend));
			var item2 = _backend.AsPollIn((s) => InternalRelay(s, _frontend));
			var items = new[] { item1, item2 };

			try
			{
				while (_isRunning)
				{
					// PollingModule.PollForever(items);
					PollingModule.DoPoll(1000, items);
				}
			}
			catch (fszmq.ZMQError) 
			{
				// context destroyed, ignore
			}
		}

		private void InternalRelay(Socket source, Socket destination)
		{
			try
			{
				var buffers = source.RecvAll();
				destination.SendAll(buffers);
			}
			catch (Exception ex)
			{
				Logger.Error("InternalRelay error", ex);
				throw;
			}
		}
	}
}