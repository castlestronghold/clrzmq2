namespace ZMQ.Extensions2.Stubs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Threading;

	public class StubZSocket : IZSocket
	{
		public StubZSocket(SocketType type, bool waiting = false) 
		{
			Subscriptions = new List<string>();
			WaitHandle = new AutoResetEvent(!waiting);
		}

		public AutoResetEvent WaitHandle { get; set; }

		public uint Port { get; set; }
		public string Address { get; set; }
		public Transport Transport { get; set; }

		public byte[] Bytes { get; set; }
		public string LastStringMessage { get; set; }
		public byte[] LastByteMessage { get; set; }

		public bool Connected { get; set; }
		public bool Binded { get; set; }
		public bool Disposed { get; private set; }

		public List<string> Subscriptions { get; set; }

		public bool LastRecvHasTimeout { get; set; }

		public void Connect(Transport transport, string address, uint port)
		{
			Transport = transport;
			Address = address;
			Port = port;

			Connected = true;
		}

		public void Connect(string uri)
		{
			Connected = true;
		}

		public void Bind(Transport transport, string address, uint port)
		{
			Transport = transport;
			Address = address;
			Port = port;

			Binded = true;
		}

		public void Bind(string endpoint)
		{
			Binded = true;
		}

		public void Dispose()
		{
			Disposed = true;
		}


		public void Send(string message, Encoding encoding)
		{
			LastStringMessage = message;
		}

		public void Send(byte[] message)
		{
			LastByteMessage = message;
		}

		public byte[] Recv()
		{
			// LastRecvHasTimeout = !IsInfinite(timeout);

			if (!WaitHandle.WaitOne(TimeSpan.FromSeconds(2)))
				throw new TimeoutException();

			return Bytes;
		}

		public string Recv(Encoding encoding)
		{
			// LastRecvHasTimeout = !IsInfinite(timeout);

			return "";
		}

		public static StubZSocket Create(SocketType type)
		{
			return new StubZSocket(type);
		}

		public void Post(byte[] bytes = null)
		{
			Bytes = bytes;

			WaitHandle.Set();
		}

		public void Subscribe(string filter)
		{
			Subscriptions.Add(filter);
		}

		public void SendMore(string key)
		{
			LastStringMessage = key;
		}
	}
}
