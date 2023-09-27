// See https://aka.ms/new-console-template for more information

using volatilityService.client;
using VolatilityService.Generated;
using volatilityService.server;

class Program
{
    static void Main(string[] args)
    {
        Task.Run(() =>
        {
            VolatilitySurfaceServiceServer server = new VolatilitySurfaceServiceServer();
            server.Start();
        });
        
        VolatilitySurfaceServiceServer server = new VolatilitySurfaceServiceServer();
        server.Start();
        Console.WriteLine("VolatilitySurfaceServiceServer service is running");
        Console.WriteLine("Press any key to stop the server...");
        

        var client = new VolatilityCalculationClient();
        for (int i = 0; i < 100; ++i)
        {
            VolatlityServiceHeartBeat message = new VolatlityServiceHeartBeat();
            message.Message = $"Message {i} sending ...";
            Console.WriteLine(message);
            var r =  client.HearBeat(message);

            var result = r.Result;
            Console.WriteLine(result.Message);
        }
        

        Console.ReadKey();
    }
}

