namespace Server 
{
	using System;
	using Castle.Facilities.ZMQ;

	[RemoteService]
	public interface IRemoteService 
	{
		int WeirdSum(int a, int b);

		void Foo();
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
		}
	}
}