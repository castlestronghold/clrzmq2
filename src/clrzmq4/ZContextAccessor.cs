namespace ZMQ.Extensions2
{
	using System;

	public class ZContextAccessor : IDisposable
	{
		private readonly ZContext _context = new ZContext();
		private readonly Func<SocketType, ZSocket> _socketFactory;
		private volatile bool _disposed;

		public ZContextAccessor()
		{
			_socketFactory = type =>
			{
				if (_disposed) throw new ObjectDisposedException("ZContextAccessor disposed");
				return new ZSocket(_context, type);
			};
		}

		public ZContextAccessor(Func<SocketType, ZSocket> socketFactory)
		{
			if (socketFactory == null) throw new ArgumentNullException("socketFactory");

			_socketFactory = socketFactory;
		}

		public ZContext Context
		{
			get
			{
				if (_disposed) throw new ObjectDisposedException("ZContextAccessor disposed");
				return _context;
			}
		}

		public Func<SocketType, ZSocket> SocketFactory
		{
			get { return _socketFactory; }
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_context.Dispose();
				_disposed = true;
			}
		}

		public static ZContextAccessor New(Func<SocketType, ZSocket> socketFactory)
		{
			return new ZContextAccessor(socketFactory);
		}
	}
}