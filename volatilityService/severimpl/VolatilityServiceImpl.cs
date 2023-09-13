//using static VolatilityService.Generated.VolatilitySurfaceRequest.Types;

using Grpc.Core;
using qirvol.volatility;
using volatilityService.data;
using VolatilityService.Generated;
using VolSurface = qirvol.volatility.VolSurface;

namespace volatilityService.severimpl
{
	public class VolatilitySurfaceServiceImpl : VolatilitySurfaceService.VolatilitySurfaceServiceBase
    {
	    
		public VolatilitySurfaceServiceImpl()
		{}

		public override Task<VolatilitySABRResponse> ComputeSABR(VolatilitySABRRequest request, ServerCallContext context)
		{
			var surface = request.SurfaceGridInput.ToVolSurface();

			var beta = request.Beta;
			var nu0 = 0.01;
			var rho0 = 0.6;
			// calibration of the surface (smile) 
			SABRCube sabrCube = SABR.sigma_calibrate(surface, nu0, rho0, beta);

			var response = new VolatilitySABRResponse()
			{
				SabrCubeComputed = sabrCube.toPSBARCube()
			};
			
			return Task.FromResult(response);
		}

		

    }
}

