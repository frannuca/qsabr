using Google.Protobuf;
using Grpc.Net.Client;
using VolatilityService.Generated;
namespace volatilityService.client;

public class VolatilityCalculationClient
{
    async Task<VolatilitySABRResponse> call(VolatilitySABRRequest message)
    {
        var channel = GrpcChannel.ForAddress("http://localhost:5181");
        var client = new VolatilitySurfaceService.VolatilitySurfaceServiceClient(channel);
        VolatilitySABRResponse serverResponse = await client.ComputeSABRAsync(message);
        return serverResponse;
    }
}