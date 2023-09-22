// See https://aka.ms/new-console-template for more information
using volatilityService;
using volatilityService.server;

class Program
{
    static void Main(string[] args)
    {

        VolatilitySurfaceServiceServer server = new VolatilitySurfaceServiceServer();
        server.Start();
        Console.WriteLine("VolatilitySurfaceServiceServer service is running");
        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();
        
    }
}

