using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetSDR.Simulator;

public interface ITcpSimulatorService
{
    void Start(int port = 50000);
}

public class TcpSimulatorService : ITcpSimulatorService
{
    public void Start(int port = 50000) => _ = Task.Run(() => RunAsync(port));

    private static async Task RunAsync(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        Console.WriteLine($"TCP simulator running on port {port}");

        while (true)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected");

                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP simulator error: {ex.Message}");
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        await using var stream = client.GetStream();
        var buffer = new byte[1024];

        while (client.Connected)
        {
            try
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received from client: {received}");

                // Echo or response
                var response = Encoding.UTF8.GetBytes($"Echo: {received}");
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
                break;
            }
        }

        client.Close();
        Console.WriteLine("Client disconnected");
    }
}