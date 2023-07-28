using System;
using Grpc.Core;
using System.Threading.Tasks;
using static VolatilityService.Generated.VolatilitySurfaceService;
using VolatilityService.Generated;

namespace volatilityService.severimpl
{
	public class VolatilitySurfaceServiceImpl: VolatilitySurfaceServiceBase
    {
		public VolatilitySurfaceServiceImpl()
		{}

		override public Task<VolatilitySurfaceResponse> ComputeSABR(VolatilitySurfaceRequest request, ServerCallContext context)
		{
			throw new NotImplementedException();
		}

        override public Task<VolatilitySurfaceResponse> InterpolateSurface(VolatilitySurfaceRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }


    }
}

