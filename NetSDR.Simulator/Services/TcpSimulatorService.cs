using System.Net;
using System.Net.Sockets;
using System.Text;
using NetSDR.Simulator.Interfaces;

namespace NetSDR.Simulator.Services;

public class TcpSimulatorService : NetworkSimulatorServiceBase, ITcpSimulatorService
{
    private TcpListener? _tcpListener;

    protected override int DefaultPort => 50000;

    private protected override async Task RunAsync(CancellationToken token)
    {
        _tcpListener = new TcpListener(IPAddress.Any, Port);
        _tcpListener.Start();

        while (!token.IsCancellationRequested)
        {
            if (_tcpListener.Pending())
            {
                var client = await _tcpListener.AcceptTcpClientAsync(token);
                _ = HandleClientAsync(client, token);
            }

            await Task.Delay(100, token);
        }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        await using var stream = client.GetStream();
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, token);
        var message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"TCP: {message}");
    }

    private protected override void StopInternal()
    {
        _tcpListener?.Stop();
        _tcpListener = null;
    }
}