using System.Net.Sockets;
using System.Text;

namespace NetSDR.Simulator;

public interface IUdpSimulatorService
{
    void Start(int port = 50001);
}

public class UdpSimulatorService : IUdpSimulatorService
{
    public void Start(int port = 50001) => _ = Task.Run(() => RunAsync(port));

    private static async Task RunAsync(int port)
    {
        using var udpClient = new UdpClient(port);
        Console.WriteLine($"Test UDP server started on port {port}");

        while (true)
        {
            try
            {
                var result = await udpClient.ReceiveAsync();
                var message = Encoding.ASCII.GetString(result.Buffer);
                Console.WriteLine($"Received UDP: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP server error: {ex.Message}");
            }
        }
    }
}