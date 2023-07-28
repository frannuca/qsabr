using System;
using Grpc.Core;
using System.Threading.Tasks;
using static VolatilityService.Generated.VolatilitySurfaceService;
using VolatilityService.Generated;
using static VolatilityService.Generated.VolatilitySurfaceRequest.Types;
using static volatilityService.data.voldatatransformations;
namespace volatilityService.severimpl
{
	public class VolatilitySurfaceServiceImpl: VolatilitySurfaceServiceBase
    {
		public VolatilitySurfaceServiceImpl()
		{}

		override public Task<VolatilitySurfaceResponse> ComputeSABR(VolatilitySurfaceRequest request, ServerCallContext context)
		{
			if(request.CaculationType!= VolatilityCalculatationType.ComputeSabr)
			{				
				throw new Exception("Compute SABR service requires a VolatilityCalculatationType ComputSabr ");
            }
			else
			{
				var table = request.Volsurface.toTable();

			}
			throw new NotImplementedException();
		}

        override public Task<VolatilitySurfaceResponse> InterpolateSurface(VolatilitySurfaceRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }


    }
}

