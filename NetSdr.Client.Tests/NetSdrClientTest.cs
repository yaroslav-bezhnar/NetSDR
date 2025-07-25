using Moq;
using System.Net.Sockets;
using NetSDR.Client;
using System.Text;

namespace NetSdr.Client.Test;

public class NetSdrClientTest
{
    private readonly Mock<INetworkClient> _mockNetworkClient;
    private readonly NetSdrClient _netSdrClient;

    public NetSdrClientTest()
    {
        _mockNetworkClient = new Mock<INetworkClient>();
        _netSdrClient = new NetSdrClient(networkClient: _mockNetworkClient.Object);
    }

    [Fact]
    public async Task ConnectAsync_WhenNotConnected_ConnectsSuccessfully()
    {
        _mockNetworkClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _netSdrClient.ConnectAsync();

        Assert.True(_netSdrClient.IsConnected);
        _mockNetworkClient.Verify(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotTryToConnectAgain()
    {
        await _netSdrClient.ConnectAsync();
        _mockNetworkClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _netSdrClient.ConnectAsync();

        Assert.True(_netSdrClient.IsConnected);
        _mockNetworkClient.Verify(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ConnectAsync_WhenSocketExceptionThrown_PrintsErrorMessage()
    {
        _mockNetworkClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SocketException());

        var ex = Assert.ThrowsAsync<SocketException>(async () => await _netSdrClient.ConnectAsync());

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task Disconnect_WhenConnected_DisconnectsSuccessfully()
    {
        await _netSdrClient.ConnectAsync();
        _mockNetworkClient.Setup(client => client.Close());

        _netSdrClient.Disconnect();

        Assert.False(_netSdrClient.IsConnected);
        _mockNetworkClient.Verify(client => client.Close(), Times.Once);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_DoesNothing()
    {
        _mockNetworkClient.Setup(client => client.Close());

        _netSdrClient.Disconnect();

        Assert.False(_netSdrClient.IsConnected);
        _mockNetworkClient.Verify(client => client.Close(), Times.Never);
    }

    [Fact]
    public async Task ToggleTransmissionAsync_WhenCommandWrittenSuccessfully_ShouldLogCorrectMessage()
    {
        await _netSdrClient.ConnectAsync();

        _mockNetworkClient.Setup(client => client.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _netSdrClient.ToggleTransmissionAsync(true);

        _mockNetworkClient.Verify(client => client.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetFrequencyAsync_WhenNotConnected_LogsMessage()
    {
        await _netSdrClient.SetFrequencyAsync(1000);

        _mockNetworkClient.Verify(client => client.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetFrequencyAsync_WhenConnected_CallsWriteAsyncWithCorrectCommand()
    {
        await _netSdrClient.ConnectAsync();

        double frequency = 1000.0;
        var expectedCommand = $"SET_FREQUENCY {frequency:F2}";

        await _netSdrClient.SetFrequencyAsync(frequency);

        _mockNetworkClient.Verify(client => client.WriteAsync(It.Is<byte[]>(buffer => Encoding.ASCII.GetString(buffer) == expectedCommand),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _mockNetworkClient
            .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _netSdrClient.ConnectAsync(cts.Token));
    }

    [Fact]
    public async Task ToggleTransmissionAsync_WhenNakReceived_ShouldCallHandleNak()
    {
        await _netSdrClient.ConnectAsync();

        var nakBytes = Encoding.ASCII.GetBytes("NAK");

        _mockNetworkClient
            .Setup(c => c.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockNetworkClient
            .Setup(c => c.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], int, int, CancellationToken>((buffer, offset, _, _) =>
            {
                Array.Copy(nakBytes, 0, buffer, offset, nakBytes.Length);
            })
            .ReturnsAsync(nakBytes.Length);

        await _netSdrClient.ToggleTransmissionAsync(true);

        _mockNetworkClient.Verify(c => c.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Disconnect_CalledMultipleTimes_OnlyClosesOnce()
    {
        _netSdrClient.ConnectAsync().Wait();
        _mockNetworkClient.Setup(c => c.Close());

        _netSdrClient.Disconnect();
        _netSdrClient.Disconnect();

        _mockNetworkClient.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public async Task ToggleTransmissionAsync_WhenWriteFails_ThrowsException()
    {
        await _netSdrClient.ConnectAsync();

        _mockNetworkClient
            .Setup(c => c.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Write failed"));

        await Assert.ThrowsAsync<Exception>(() => _netSdrClient.ToggleTransmissionAsync(true));
    }
}