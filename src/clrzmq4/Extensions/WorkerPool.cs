namespace ZMQ.Extensions2
{
	using System;
	using System.Threading;
	using fszmq;
	using ZMQ.Extensions2;

	public class WorkerPool : Device
	{
		protected static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(WorkerPool));

		private readonly ThreadStart _proc;
		private readonly Thread[] _workers;

		public WorkerPool(ZContext context, string frontEndpoint, string backEndpoint, ThreadStart proc, int workers)
			: base(context, frontEndpoint, backEndpoint)
		{
			if (proc == null) throw new ArgumentNullException("proc");
			if (workers < 1) throw new ArgumentOutOfRangeException("workers");

			_proc = proc;
			_workers = new Thread[workers];

			for (int i = 0; i < workers; i++)
			{
				var thread = new Thread(proc) { IsBackground = true };
				thread.Start();
				
				_workers[i] = thread;
			}
		}		
	}
}
