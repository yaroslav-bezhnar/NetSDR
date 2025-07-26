using System;

namespace NetSDR.Simulator;

public interface INetSdrSimulator
{
    void Start(int port = 50000);
}

public class TcpSimulatorService : INetSdrSimulator
{
    public void Start(int port = 50000)
    {
        _ = Task.Run(async () =>
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            Console.WriteLine($"Test TCP server started on port {port}");

            while (true)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected");

                    var stream = client.GetStream();
                    var buffer = Encoding.ASCII.GetBytes("Hello client");
                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("TCP server error: " + ex.Message);
                }
            }
        });
    }
}
