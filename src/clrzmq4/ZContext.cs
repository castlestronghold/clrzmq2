namespace ZMQ.Extensions2
{
	using System;
	using fszmq;

	public class ZContext : IDisposable
	{
		private readonly fszmq.Context _context = new Context();
		private volatile bool _disposed;

		internal Context Ctx
		{
			get
			{
				if (_disposed) throw new ObjectDisposedException("ZContext disposed");
				return _context;
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				(_context as IDisposable).Dispose();
			}
		}
	}
}