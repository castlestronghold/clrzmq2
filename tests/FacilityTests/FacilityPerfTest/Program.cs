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
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			for (var i = 0; i < 1000; i++)
			{
				// Console.WriteLine("new batch ");

				service.NoParamsOrReturn();
				service.JustParams("1");
				service.JustReturn().Equals("abc");
				service.ParamsWithStruct(new MyCustomStruct() { Name = "1", Age = 30 });
				service.ParamsWithCustomType1(new Impl1() { });
				service.ParamsWithCustomType2(new Contract1Impl() { Name = "2", Age = 31 });
				service.ParamsAndReturn("", 1, DateTime.Now, 102.2m, FileAccess.ReadWrite, 1, 2, 3.0f, 4.0);
				service.WithInheritanceParam(new Derived1() { Something = 10, DerivedProp1 = 20});
				
				var b = service.WithInheritanceRet();
				Assert.IsNotNull(b);
				Assert.IsInstanceOf(typeof(Derived2), b);
				Assert.AreEqual(10, (b as Derived2).Something);
				Assert.AreEqual("test", (b as Derived2).DerivedProp2);
			}

			watch.Stop();

			Console.WriteLine("took " + watch.ElapsedMilliseconds);
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

		string ParamsAndReturn(string p1, int p2, DateTime dt, decimal p4, FileAccess acc, short s1, byte b1, float f1, double d1);

		void ParamsWithStruct(MyCustomStruct p1);
		void ParamsWithCustomType1(Impl1 p1);
		void ParamsWithCustomType2(IContract1 p1);

		void WithInheritanceParam(Base b);
		Base WithInheritanceRet();
	}

	[ProtoContract]
	[ProtoInclude(1, typeof(Derived1))]
	[ProtoInclude(2, typeof(Derived2))]
	public class Base
	{
		[ProtoMember(10)]
		public int Something { get; set; }
	}
	[ProtoContract]
	public class Derived1 : Base
	{
		[ProtoMember(20)]
		public int DerivedProp1 { get; set; }
	}
	[ProtoContract]
	public class Derived2 : Base
	{
		[ProtoMember(30)]
		public string DerivedProp2 { get; set; }
	}

	[ProtoContract]
	public class Contract2Impl : IContract1
	{
		[ProtoMember(1)]
		public string Name { get; set; }
		[ProtoMember(2)]
		public int Age { get; set; }
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

		public string ParamsAndReturn(string p1, int p2, DateTime dt, decimal p4, FileAccess acc, short s1, byte b1, float f1, double d1)
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

		public void WithInheritanceParam(Base b)
		{
			Assert.IsNotNull(b);
		}

		public Base WithInheritanceRet()
		{
			return new Derived2()
			{
				Something = 10, DerivedProp2 = "test"
			};
		}
	}
}
