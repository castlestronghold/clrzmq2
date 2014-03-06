namespace FacilityPerfTest
{
	using System;
	using System.IO;
	using Castle.Facilities.ZMQ;
	using Castle.Facilities.ZMQ.Internals;
	using Castle.MicroKernel.Registration;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;
	using NUnit.Framework;
	using ProtoBuf;

	class Program
	{
		private static WindsorContainer _containerClient;
		private static WindsorContainer _containerServer;

		static void Main(string[] args)
		{
//			var packer1 = MsgPack.Serialization.MessagePackSerializer.Create<RequestMessage>();
//			var packer2 = MsgPack.Serialization.MessagePackSerializer.Create<ResponseMessage>();
//			packer2.Pack(new MemoryStream(), new ResponseMessage("", new Exception("test")) );
//
//
//			return;

			_containerClient = new WindsorContainer(new XmlInterpreter("config_client.config"));
			_containerServer = new WindsorContainer(new XmlInterpreter("config_server.config"));
			
			_containerServer.Register(Component.For<IRemoteServ1>().ImplementedBy<RemoteServImpl>());
			_containerClient.Register(Component.For<IRemoteServ1>());

			try
			{
				var service = _containerClient.Resolve<IRemoteServ1>();

				InvokeBatch(service);
			}
			finally
			{
				_containerClient.Dispose();
				_containerServer.Dispose();
			}
		}

		private static void InvokeBatch(IRemoteServ1 service)
		{
			for (var i = 0; i < 10; i++)
			{
				Console.WriteLine("new batch ");

				service.NoParamsOrReturn();
				service.JustParams("1");
				service.JustReturn().Equals("abc");
				service.ParamsWithStruct(new MyCustomStruct() { Name = "1", Age = 30 });
				service.ParamsWithCustomType1(new Impl1() { });
				service.ParamsWithCustomType2(new Contract1Impl() { Name = "2", Age = 31 });
//				service.ParamsAndReturn("", 1, DateTime.Now, 102.2m);
			}

		}

		public void CleanUp()
		{
			if (_containerClient != null)
				_containerClient.Dispose();

			if (_containerServer != null)
				_containerServer.Dispose();
		}
	}

	[ProtoContract]
	public struct MyCustomStruct
	{
		[ProtoMember(1)]
		public string Name;
		[ProtoMember(2)]
		public int Age;
	}

	[ProtoContract]
	public class Impl1
	{
	}

	public interface IContract1
	{
		string Name { get; set; }
		int Age { get; set; }
	}
	[ProtoContract]
	public class Contract1Impl : IContract1
	{
		[ProtoMember(1)]
		public string Name { get; set; }
		[ProtoMember(2)]
		public int Age { get; set; }
	}

	[RemoteService]
	public interface IRemoteServ1
	{
		void NoParamsOrReturn();

		string JustReturn();

		void JustParams(string p1);

		string ParamsAndReturn(string p1, int p2, DateTime dt, decimal p4);

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

		public void JustParams(string p1)
		{
			Assert.IsNotNull(p1);
		}

		public string ParamsAndReturn(string p1, int p2, DateTime dt, decimal p4)
		{
			return string.Empty;
		}

		public void ParamsWithStruct(MyCustomStruct p1)
		{
			Assert.IsNotNull(p1.Name);
		}

		public void ParamsWithCustomType1(Impl1 p1)
		{
			Assert.IsNotNull(p1);
		}

		public void ParamsWithCustomType2(IContract1 p1)
		{
			Assert.IsNotNull(p1);
		}
	}
}
