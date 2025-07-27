using System.Net;
using System.Net.Sockets;

namespace NetSDR.Client.Tests.Helpers;

public class LoopbackServer : IDisposable
{
    #region fields

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();

    #endregion

    #region properties

    public int Port { get; }
    public NetworkStream? Stream { get; private set; }

    #endregion

    #region constructors

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

    #endregion

    #region methods

    public void Dispose()
    {
        _cts.Cancel();
        Stream?.Dispose();
        _listener.Stop();
    }

    #endregion
}