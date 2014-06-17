namespace clrzmq4Test
{
	using System;
	using System.Text;
	using NUnit.Framework;
	using ZMQ.Extensions2;


	[TestFixture]
    public class ReqRepSockets 
    {
		private ZContextAccessor _ctxAccessor;

		[TestFixtureSetUp]
		public void BuildUp()
		{
			_ctxAccessor = new ZContextAccessor();
		}
		[TestFixtureTearDown]
		public void TearDown()
		{
			if (_ctxAccessor != null)
			{
				_ctxAccessor.Dispose();
			}
		}

		[Test]
		public void ReqRes_1()
		{
			var socketRep = _ctxAccessor.SocketFactory(SocketType.REP);
			var socketReq = _ctxAccessor.SocketFactory(SocketType.REQ);

			try
			{
				socketRep.Bind("tcp://0.0.0.0:90111");
				socketReq.Connect("tcp://127.0.0.1:90111");

				socketReq.Send("Hello world", Encoding.UTF8);

				var msg = socketRep.Recv(Encoding.UTF8);
				Assert.AreEqual(msg, "Hello world");
			}
			finally
			{ 
				socketReq.Dispose();
				socketRep.Dispose();
			}
		}

		[Test]
		public void ReqRes_2()
		{
			var socketRep = _ctxAccessor.SocketFactory(SocketType.REP);
			var socketReq = _ctxAccessor.SocketFactory(SocketType.REQ);

			try
			{
				socketRep.Bind("tcp://0.0.0.0:90111");
				socketReq.Connect("tcp://127.0.0.1:90111");

				socketReq.Send(Encoding.UTF8.GetBytes("Hello world"));

				var buffer = socketRep.Recv();
				var msg = Encoding.UTF8.GetString(buffer);
				Assert.AreEqual(msg, "Hello world");
			}
			finally
			{
				socketReq.Dispose();
				socketRep.Dispose();
			}
		}
    }
}
