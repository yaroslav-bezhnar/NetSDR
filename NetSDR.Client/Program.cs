using System.Net.Sockets;
using System.Net;
using System.Text;
using NetSDR.Client;

public class Program
{
    private static async Task Main(string[] args)
    {
        var serverThread = new Thread(TcpServer.StartServer);
        serverThread.Start();

        await Task.Delay(1000);

        var client = new NetSdrClient("127.0.0.1", 50000);
        var receiver = new UdpDataReceiver();

        client.OnTransmissionStarted += async () =>
        {
           await receiver.StartReceiving("iq_test.bin");
        };

        await client.ConnectAsync();
        await client.ToggleTransmissionAsync(true);

        await Task.Delay(1000);

        await client.SetFrequencyAsync(1000000);
        await client.ToggleTransmissionAsync(false);
        client.Disconnect();
    }
}

class TcpServer
{
    public static void StartServer()
    {
        using var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 50000);
        server.Start();

        while (true)
        {
            var client = server.AcceptTcpClient();

            var stream = client.GetStream();
            var buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                var received = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                var response = "test"; // "NAK"; // Simulate NAK
                var responseBytes = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }

            client.Close();
        }
    }
}
