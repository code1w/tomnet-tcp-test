
namespace TomNet.Sockets
{
	public class BaseSocket
	{
		protected enum States
		{
			Disconnected,
			Connecting,
			Connected,

		}

		protected enum Transitions
		{
			StartConnect,
			ConnectionSuccess,
			ConnectionFailure,
			Disconnect
		}

		protected volatile bool isDisconnecting = false;



	}
}
