namespace Server 
{
	public interface IRemoteService 
	{
		int Sum(int a, int b);
	}

	public class RemoteService : IRemoteService 
	{
		public int Sum(int a, int b) 
		{
			return a + b;
		}
	}
}