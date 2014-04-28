namespace FacilitySerializationTest
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Castle.Facilities.ZMQ.Internals;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class ResponseMessageTestCase
	{
		[Test]
		public void ReturnType_void_supported()
		{
			var response = TransportSerialization.build_response(null, typeof(void));

			response.Should().NotBeNull();
			response.ReturnValue.Should().BeNull();
			response.ReturnValueType.Should().BeNull();
			response.ExceptionInfo.Should().BeNull();
		}

		[Test]
		public void ReturnType_for_string_supported()
		{
			var response = TransportSerialization.build_response("test", typeof(string));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be("string");
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(string));
			ret.Should().Be("test");
		}

		[Test]
		public void ReturnType_for_primitive_supported()
		{
			var response = TransportSerialization.build_response(1, typeof(Int32));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be("string");
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(Int32));
			ret.Should().Be(1);
		}

		[Test]
		public void ReturnType_for_proto_contract_supported()
		{
			var response = TransportSerialization.build_response(new MyCustomClass(), typeof(MyCustomClass));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(MyCustomClass).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(MyCustomClass));
			ret.Should().BeOfType<MyCustomClass>();
		}

		[Test]
		public void ReturnType_for_proto_contract_empty_array_supported()
		{
			var response = TransportSerialization.build_response(new MyCustomClass[0], typeof(MyCustomClass[]));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(MyCustomClass[]).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(MyCustomClass[]));
			ret.Should().BeOfType<MyCustomClass[]>();
			(ret as MyCustomClass[]).Length.Should().Be(0);
		}

		[Test]
		public void ReturnType_for_proto_contract_array_supported()
		{
			var response = TransportSerialization.build_response(
				new [] { new MyCustomClass() }, 
				typeof(MyCustomClass[]));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(MyCustomClass[]).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(MyCustomClass[]));
			ret.Should().BeOfType<MyCustomClass[]>();
			(ret as MyCustomClass[]).Length.Should().Be(1);
		}

		[Test]
		public void ReturnType_for_string_array_supported()
		{
			var response = TransportSerialization.build_response(new [] { "1", "2" }, typeof(string[]));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(string[]).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(string[]));
			ret.Should().BeOfType<string[]>();
			var arr = (string[])ret;
			arr.Length.Should().Be(2);
			arr[0].Should().Be("1");
			arr[1].Should().Be("2");
		}

		[Test]
		public void ReturnType_for_string_enumerable_supported()
		{
			var response = TransportSerialization.build_response(new[] { "1", "2" }, typeof(IEnumerable<string>));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(IEnumerable<string>).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(IEnumerable<string>));
			ret.Should().BeAssignableTo<IEnumerable<string>>();
			var arr = (IEnumerable<string>)ret;
			arr.Count().Should().Be(2);
			arr.ElementAt(0).Should().Be("1");
			arr.ElementAt(1).Should().Be("2");
		}


		[Test]
		public void ReturnType_for_string_empty_array_supported()
		{
			var response = TransportSerialization.build_response(new string[0], typeof(string[]));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(string[]).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(string[]));
			ret.Should().BeOfType<string[]>();
			var arr = (string[])ret;
			arr.Length.Should().Be(0);
		}

		[Test]
		public void ReturnType_for_string_null_array_supported()
		{
			var response = TransportSerialization.build_response(null, typeof(string[]));

			response.Should().NotBeNull();
			response.ReturnValue.Should().NotBeNull();
			response.ReturnValueType.Should().NotBeNull();
			response.ReturnValueType.Should().Be(typeof(string[]).AssemblyQualifiedName);
			response.ExceptionInfo.Should().BeNull();

			var ret = TransportSerialization.deserialize_reponse(response, typeof(string[]));
			ret.Should().BeOfType<string[]>();
		}

		[Test]
		public void ReturningException()
		{
			var response = TransportSerialization.build_response_with_exception("SerializationException", "a message");

			response.Should().NotBeNull();
			response.ExceptionInfo.Should().NotBeNull();
			response.ReturnValue.Should().BeNull();
			response.ReturnValueType.Should().BeNull();

			response.ExceptionInfo.Typename.Should().Be("SerializationException");
			response.ExceptionInfo.Message.Should().Be("a message");
		}
	}
}
