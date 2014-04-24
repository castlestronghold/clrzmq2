namespace FacilitySerializationTest
{
	using System;
	using System.IO;
	using System.Linq;
	using FluentAssertions;
	using NUnit.Framework;
	using Castle.Facilities.ZMQ;
	using Castle.Facilities.ZMQ.Internals;
	using ProtoBuf;

	[ProtoContract]
	public class MyCustomClass
	{
		[ProtoMember(1)]
		public string Name;
		[ProtoMember(2)]
		public int Age;
	}

	[TestFixture]
    public class ParameterSerializationTestCase
    {
		[Test]
		public void With_simple_types_should_serialize_both_ways()
		{
			var types = new[] { typeof(string), typeof(int), typeof(DateTime) };

			var dt = DateTime.Now;
			var buffers = TransportSerialization.serialize_parameters(
				new object[]
				{
					"123", 1, dt
				}, 
				types);
					
			var args = TransportSerialization.deserialize_params(buffers, types);

			args.Length.Should().Be(3);
			args[0].Should().Be("123");
			args[1].Should().Be(1);
			args[2].Should().Be(dt);
		}

		[Test]
		public void With_other_protocol_should_serialize_both_ways()
		{
			var types = new[] { typeof(MyCustomClass) };

			var dt = DateTime.Now;
			var buffers = TransportSerialization.serialize_parameters(
				new object[]
				{
					new MyCustomClass() { Age = 33, Name = "test" }
				},
				types);

			var args = TransportSerialization.deserialize_params(buffers, types);

			args.Length.Should().Be(1);
			args[0].Should().NotBeNull();
			var my = (MyCustomClass)args[0];
			my.Age.Should().Be(33);
			my.Name.Should().Be("test");
		}
    }

	[TestFixture]
	public class FromProtoBufJsTestCase
	{
		[Test]
		public void Simple_RequestObject()
		{
			var contentInBase64 = "CgtzZXJ2aWNlbmFtZRIKbWV0aG9kbmFtZRoXCg0KC3N0cmluZ3ZhbHVlEgZzdHJpbmcaLAobCg10eXBlbmFtZSBoZXJlEgp2YWx1ZSBoZXJlEg1FeGNlcHRpb25JbmZvIgZzdHJpbmciDUV4Y2VwdGlvbkluZm8=";
			var buffer = Convert.FromBase64String(contentInBase64);

			var reqMes = ProtoBuf.Serializer.Deserialize<RequestMessage>(new MemoryStream(buffer));
			


		}
	}
}
