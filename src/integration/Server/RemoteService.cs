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

		Guid Pair(Guid g);
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

		public Guid Pair(Guid g)
		{
			return g;
		}

		public void Error()
		{
			throw new InvalidOperationException("ouch");
		}
	}
}