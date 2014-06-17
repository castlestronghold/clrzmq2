namespace ZMQ.Extensions2
{
	public sealed class ZConfig
	{
		public ZConfig(string ip, uint port, Transport transport = Transport.TCP)
		{
			Transport = transport;
			Ip = ip;
			Port = port;
		}

		public SocketType SocketType { get; set; }

		public Transport Transport { get; set; }

		public string Ip { get; set; }

		public uint Port { get; set; }

		public string Local
		{
			get { return "inproc://workers_" + Port; }
		}

		public override string ToString()
		{
			return string.Format("{0}://{1}:{2}", Transport, Ip, Port).ToLower();
		}
	}
}