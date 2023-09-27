using Google.Protobuf;
using Grpc.Net.Client;
using VolatilityService.Generated;
namespace volatilityService.client;

public class VolatilityCalculationClient
{
    async public Task<VolatilitySABRResponse> Call(VolatilitySABRRequest message)
    {
        var channel = GrpcChannel.ForAddress("http://localhost:5181");
        var client = new VolatilitySurfaceService.VolatilitySurfaceServiceClient(channel);
        VolatilitySABRResponse serverResponse = await client.ComputeSABRAsync(message);
        return serverResponse;
    }
    
    async public Task<VolatlityServiceHeartBeat> HearBeat(VolatlityServiceHeartBeat message)
    {
        var channel = GrpcChannel.ForAddress("http://localhost:5181");
        var client = new VolatilitySurfaceService.VolatilitySurfaceServiceClient(channel);
        VolatlityServiceHeartBeat serverResponse = await client.HeartBeatAsync(message);
        return serverResponse;
    }
}