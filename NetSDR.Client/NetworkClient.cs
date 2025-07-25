using System.Net.Sockets;

namespace NetSDR.Client;

public class NetworkClient : INetworkClient
{
    #region fields

    private readonly TcpClient _tcpClient = new();
    private NetworkStream? _networkStream;

    #endregion

    #region methods

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
        _networkStream = _tcpClient.GetStream();
    }

    public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) =>
        await GetStream().WriteAsync(buffer.AsMemory(offset, count), cancellationToken);

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default) =>
        await GetStream().ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

    public void Close()
    {
        _networkStream?.Close();
        _tcpClient.Close();
    }

    public void Dispose()
    {
        _networkStream?.Dispose();
        _tcpClient.Dispose();
    }

    private NetworkStream GetStream() =>
        _networkStream ?? throw new InvalidOperationException("Client is not connected.");

    #endregion
}