using System.Net.Sockets;
using NetSDR.Client.Interfaces;

namespace NetSDR.Client.Tcp;

public class TcpNetworkClient : ITcpNetworkClient
{
    #region fields

    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;

    #endregion

    #region methods

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _tcpClient = new();
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
        _tcpClient?.Close();
        _networkStream = null;
        _tcpClient = null;
    }

    public void Dispose()
    {
        _networkStream?.Dispose();
        _tcpClient?.Dispose();
        _networkStream = null;
        _tcpClient = null;
    }

    private NetworkStream GetStream() =>
        _networkStream ?? throw new InvalidOperationException("Client is not connected.");

    #endregion
}