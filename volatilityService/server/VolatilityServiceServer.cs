using Grpc.Core;
using VolatilityService.Generated;

namespace volatilityService.server
{
    using severimpl;
    
	public class VolatilitySurfaceServiceServer<T>
	{
		private Server _server;
		public VolatilitySurfaceServiceServer(string host="localhost",int port=22222)
		{
            _server = new Server()
            {
                Services = { VolatilitySurfaceService.BindService(new VolatilitySurfaceServiceImpl()) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };

        }

        public void Start()
        {
            _server.Start();
        }

        public async Task ShutdownAsync()
        {
            await _server.ShutdownAsync();
        }
        
    }
}

