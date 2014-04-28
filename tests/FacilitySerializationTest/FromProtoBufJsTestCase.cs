namespace FacilitySerializationTest
{
	using System;
	using System.IO;
	using Castle.Facilities.ZMQ.Internals;
	using NUnit.Framework;

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