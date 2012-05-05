namespace Server 
{
	using Castle.Facilities.ZMQ;

	[RemoteService]
	public interface IRemoteService 
	{
		int Sum(int a, int b);
	}

	public class RemoteServiceImpl : IRemoteService 
	{
		public int Sum(int a, int b) 
		{
			return a + b;
		}
	}
}