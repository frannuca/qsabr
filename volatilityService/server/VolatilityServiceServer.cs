using System;
using Grpc.Core;
using System.Threading.Tasks;
using VolatilityService.Generated;
using volatilityService.severimpl;

namespace volatilityService
{
	public class VolatilitySurfaceServiceServer
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

