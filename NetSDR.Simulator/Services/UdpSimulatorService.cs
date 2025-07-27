using System.Net.Sockets;
using System.Text;
using NetSDR.Simulator.Interfaces;

namespace NetSDR.Simulator.Services;

public class UdpSimulatorService : NetworkSimulatorServiceBase, IUdpSimulatorService
{
    private UdpClient? _udpClient;

    protected override int DefaultPort => 50001;

    private protected override async Task RunAsync(CancellationToken token)
    {
        _udpClient = new UdpClient(Port);

        while (!token.IsCancellationRequested)
        {
            var result = await _udpClient.ReceiveAsync(token);
            var message = Encoding.ASCII.GetString(result.Buffer);
            Console.WriteLine($"UDP: {message}");
        }
    }

    private protected override void StopInternal()
    {
        _udpClient?.Close();
        _udpClient = null;
    }
}