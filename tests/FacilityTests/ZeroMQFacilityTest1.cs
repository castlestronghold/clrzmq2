namespace FacilityTests
{
	using Castle.Facilities.ZMQ;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;
	using NUnit.Framework;

	public struct MyCustomStruct
	{
		public string Name;
		public int Age;
	}

	public class Impl1
	{
		
	}

	public interface IContract1
	{
		string Name { get; set; }
		int Age { get; set; }
	}
	public class Contract1Impl : IContract1
	{
		public string Name { get; set; }
		public int Age { get; set; }
	}

	[RemoteService]
	public interface IRemoteServ1
	{
		void NoParamsOrReturn();
		string JustReturn();
		void JustParams(string p1);
		string ParamsAndReturn(string p1);
		void ParamsWithStruct(MyCustomStruct p1);
		void ParamsWithCustomType1(Impl1 p1);
		void ParamsWithCustomType2(IContract1 p1);
	}

	public class RemoteServImpl : IRemoteServ1
	{
		public void NoParamsOrReturn()
		{
		}

		public string JustReturn()
		{
			return "abc";
		}

		public void JustParams(string p1) { }

		public string ParamsAndReturn(string p1)
		{
			return "123";
		}

		public void ParamsWithStruct(MyCustomStruct p1) { }

		public void ParamsWithCustomType1(Impl1 p1) { }

		public void ParamsWithCustomType2(IContract1 p1) { }
	}

	[TestFixture]
	public class ZeroMQFacilityTest1
	{
		private WindsorContainer _containerClient;
		private WindsorContainer _containerServer;

		[TestFixtureSetUp]
		public void Init()
		{
			_containerClient = new WindsorContainer(new XmlInterpreter("config_client.config"));
			_containerServer = new WindsorContainer(new XmlInterpreter("config_server.config"));
			
			_containerServer.Register(Component.For<IRemoteServ1>().ImplementedBy<RemoteServImpl>());
			_containerClient.Register(Component.For<IRemoteServ1>());
		}

		[TestFixtureTearDown]
		public void CleanUp()
		{
			if (_containerClient != null)
				_containerClient.Dispose();

			if (_containerServer != null)
				_containerServer.Dispose();
		}

		[Test]
		public void NoParamsOrReturnCall()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			service.NoParamsOrReturn();
		}

		[Test]
		public void JustParamsCall()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			service.JustParams("1");
		}

		[Test]
		public void JustReturnCall()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			Assert.AreEqual("abc", service.JustReturn());
		}
		
		[Test]
		public void ParamsWithStruct()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			service.ParamsWithStruct(new MyCustomStruct() { Name = "1", Age = 30 });
		}

		[Test]
		public void ParamsWithCustomType1()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			service.ParamsWithCustomType1(new Impl1() { });
		}

		[Test]
		public void ParamsWithCustomType2()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			service.ParamsWithCustomType2(new Contract1Impl() { Name = "2", Age = 31 });
		}

		[Test]
		public void ParamsAndReturnCall()
		{
			var service = _containerClient.Resolve<IRemoteServ1>();
			Assert.AreEqual("123", service.ParamsAndReturn(""));
		}
	}
}
