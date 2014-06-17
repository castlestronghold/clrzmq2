namespace ZMQ.Extensions2
{
	public enum Transport
	{
		TCP, inproc
	}

	public enum SocketType
	{
		PUB = 1,
		SUB = 2,
		REQ = 3,
		REP = 4,
		//		DEALER = 5,
		//		ROUTER = 6,
		PULL = 7,
		PUSH = 8,
		XPUB = 9,
		XSUB = 10,
		//		XREQ = DEALER,
		//		XREP = ROUTER,
	}
}