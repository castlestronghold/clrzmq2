namespace Server 
{
	using System;
	using System.Threading;
	using Castle.Facilities.ZMQ;

	[RemoteService]
	public interface IRemoteService 
	{
		int WeirdSum(int a, int b);

		void Foo();

		void Error();
	}

	public class RemoteServiceImpl : IRemoteService 
	{
		public int WeirdSum(int a, int b) 
		{
			return a + b + 7;
		}

		public void Foo()
		{
			Console.WriteLine("Foo invoked");

			Thread.Sleep(200);
		}

		public void Error()
		{
			throw new InvalidOperationException("ouch");
		}
	}
}