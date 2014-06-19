namespace ZMQ.Extensions2
{
	using System.Text;

	public interface IZSocket
	{
		void Connect(Transport transport, string address, uint port);

		void Connect(string uri);

		void Bind(Transport transport, string address, uint port);

		void Bind(string endpoint);

		void Send(string message, Encoding encoding);

		void SendMore(string key);

		void Send(byte[] message);

		byte[] Recv(/*int timeout = DefaultTimeout*/);

		string Recv(Encoding encoding /*, int timeout = DefaultTimeout*/);

		void Subscribe(string filter);
	}
}