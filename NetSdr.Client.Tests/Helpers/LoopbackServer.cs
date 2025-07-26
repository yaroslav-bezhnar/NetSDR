using System.Net;
using System.Net.Sockets;

namespace NetSdr.Client.Test.Helpers;

public class LoopbackServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    public int Port { get; }
    public NetworkStream? Stream { get; private set; }

    public LoopbackServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint) _listener.LocalEndpoint).Port;

        Task.Run(async () =>
        {
            using var client = await _listener.AcceptTcpClientAsync();
            Stream = client.GetStream();

            await Task.Delay(Timeout.Infinite, _cts.Token)
                .ContinueWith(_ => { }, TaskScheduler.Default);
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        Stream?.Dispose();
        _listener.Stop();
    }
}