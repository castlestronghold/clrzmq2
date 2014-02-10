namespace FacilityTests
{
	using Castle.Facilities.ZMQ;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;
	using NUnit.Framework;


	[RemoteService]
	public interface IRemoteServ1
	{
		void NoParamsOrReturn();

		string JustReturn();

		void JustParams(string p1);

		string ParamsAndReturn(string p1);
	}

	public class RemoteServImpl : IRemoteServ1
	{
		public void NoParamsOrReturn()
		{
		}

		public string JustReturn()
		{
			return string.Empty;
		}

		public void JustParams(string p1)
		{
		}

		public string ParamsAndReturn(string p1)
		{
			return string.Empty;
		}
	}

	[TestFixture]
	public class ZeroMQFacilityTest1
	{
		private WindsorContainer _containerClient;
		private WindsorContainer _containerServer;

		[SetUp]
		public void Init()
		{
			_containerClient = new WindsorContainer(new XmlInterpreter("config_client.config"));
			_containerServer = new WindsorContainer(new XmlInterpreter("config_server.config"));
			
			_containerServer.Register(Component.For<IRemoteServ1>().ImplementedBy<RemoteServImpl>());
			_containerClient.Register(Component.For<IRemoteServ1>().ImplementedBy<RemoteServImpl>());
		}

		[TearDown]
		public void CleanUp()
		{
			if (_containerClient != null)
				_containerClient.Dispose();

			if (_containerServer != null)
				_containerServer.Dispose();
		}

		[Test]
		public void A()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			service.NoParamsOrReturn();
		}
	}
}
