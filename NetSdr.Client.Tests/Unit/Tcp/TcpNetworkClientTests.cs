using System.Net.Sockets;
using System.Text;
using NetSdr.Client.Test.Helpers;
using NetSDR.Client.Tcp;

namespace NetSdr.Client.Test.Unit.Tcp;

public class TcpNetworkClientTests : IDisposable
{
    private readonly LoopbackServer _server;
    private readonly TcpNetworkClient _client;

    public TcpNetworkClientTests()
    {
        _server = new LoopbackServer();
        _client = new TcpNetworkClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }

    [Fact]
    public async Task ConnectAsync_ValidEndpoint_OpensConnection()
    {
        await _client.ConnectAsync("127.0.0.1", _server.Port, CancellationToken.None);

        // if no SocketException is thrown, consider it successful
    }

    [Fact]
    public async Task ConnectAsync_InvalidPort_ThrowsSocketException()
    {
        await Assert.ThrowsAsync<SocketException>(
            () => _client.ConnectAsync("127.0.0.1", 1, CancellationToken.None));
    }

    [Fact]
    public async Task WriteAsync_SendsCorrectBytesToServer()
    {
        await _client.ConnectAsync("127.0.0.1", _server.Port, CancellationToken.None);

        var payload = "HELLO"u8.ToArray();
        await _client.WriteAsync(payload, 1, 3, CancellationToken.None);

        var buffer = new byte[3];
        var read = await _server.Stream.ReadAsync(buffer, 0, buffer.Length);

        Assert.Equal(3, read);
        Assert.Equal("ELL", Encoding.UTF8.GetString(buffer));
    }

    [Fact]
    public async Task ReadAsync_ReceivesBytesFromServer()
    {
        await _client.ConnectAsync("127.0.0.1", _server.Port, CancellationToken.None);

        var _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            var msg = new byte[] { 1, 2, 3, 4 };
            await _server.Stream.WriteAsync(msg, 0, msg.Length);
        });

        var readBuffer = new byte[4];
        var count = await _client.ReadAsync(readBuffer, 0, 4, CancellationToken.None);

        Assert.Equal(4, count);
        Assert.Equal([1, 2, 3, 4], readBuffer);
    }

    [Fact]
    public async Task WriteAsync_Cancelled_ThrowsTaskCanceledException()
    {
        await _client.ConnectAsync("127.0.0.1", _server.Port, CancellationToken.None);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _client.WriteAsync(new byte[10], 0, 10, cts.Token));
    }

    [Fact]
    public async Task ReadAsync_Cancelled_ThrowsTaskCanceledException()
    {
        await _client.ConnectAsync("127.0.0.1", _server.Port, CancellationToken.None);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _client.ReadAsync(new byte[10], 0, 10, cts.Token));
    }

    [Fact]
    public async Task MethodsAfterDispose_ThrowInvalidOperationException()
    {
        await _client.ConnectAsync("127.0.0.1", _server.Port, CancellationToken.None);

        _client.Dispose();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.WriteAsync(new byte[1], 0, 1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _client.ReadAsync(new byte[1], 0, 1, CancellationToken.None));
    }
}