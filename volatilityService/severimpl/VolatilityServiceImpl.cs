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

        /*
		 * message SurfacePillar
		{
		float expiry_years = 1;
		float tenor_years = 2;
		float forward = 3;
		float strike = 4;
		float value = 5;	
		}
		 * */
        override public Task<VolatilitySurfaceResponse> InterpolateSurface(VolatilitySurfaceRequest request, ServerCallContext context)
        {
            
            var surfacedict = (IDictionary<double, IDictionary<int, qirvol.volatility.VolPillar[]>>)request.Volsurface.toDict();
			var surface = new qirvol.volatility.VolSurface(surfacedict);

			var msg = new VolatilitySurfaceResponse();
			msg.Volsurface = new VolSurface();
			var psurf = surface.maturities_years
						.Select(T => {
							var tenors = surface.tenors_by_maturity(T);
							var data = tenors.Select(tenor => surface.Cube_Ty[T][tenor]);
							return 0.0;
							})
						;

            msg.Volsurface.Surface.AddRange(null);


            throw new NotImplementedException();
        }


    }
}

