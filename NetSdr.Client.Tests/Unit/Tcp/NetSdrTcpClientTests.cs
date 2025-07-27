using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetSDR.Client.Interfaces;
using NetSDR.Client.Tcp;
using Polly.Timeout;

namespace NetSdr.Client.Tests.Unit.Tcp;

public class NetSdrTcpClientTests
{
    #region fields

    private readonly Mock<ITcpNetworkClient> _mockNetworkClient;
    private readonly Mock<ILogger<NetSdrTcpClient>> _mockLogger;
    private readonly NetSdrTcpClient _client;

    #endregion

    #region constructors

    public NetSdrTcpClientTests()
    {
        _mockNetworkClient = new Mock<ITcpNetworkClient>();
        _mockLogger = new Mock<ILogger<NetSdrTcpClient>>();
        _client = new NetSdrTcpClient(networkClient: _mockNetworkClient.Object, timeout: Timeout.InfiniteTimeSpan, logger: _mockLogger.Object);
    }

    #endregion

    #region methods

    [Fact]
    public async Task ConnectAsync_WhenNotConnected_ConnectsSuccessfully()
    {
        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _client.ConnectAsync();

        _client.IsConnected.Should().BeTrue();
        _mockNetworkClient.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotCallConnectAgain()
    {
        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _client.ConnectAsync();
        await _client.ConnectAsync();

        _mockNetworkClient.Verify(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WhenSocketExceptionThrown_ThrowsSocketException()
    {
        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SocketException());

        var act = () => _client.ConnectAsync();

        await act.Should().ThrowAsync<SocketException>();
    }

    [Fact]
    public async Task ConnectAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var act = () => _client.ConnectAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ConnectAsync_WhenTimeoutExceeded_ThrowsTimeoutRejectedException()
    {
        var timedClient = new NetSdrTcpClient(networkClient: _mockNetworkClient.Object, timeout: TimeSpan.FromMilliseconds(50), logger: _mockLogger.Object);

        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(async () => { await Task.Delay(200); });

        var act = () => timedClient.ConnectAsync();

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Disconnect_WhenConnected_ClosesOnceAndSetsIsConnectedFalse()
    {
        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await _client.ConnectAsync();

        _client.Disconnect();

        _client.IsConnected.Should().BeFalse();
        _mockNetworkClient.Verify(x => x.Close(), Times.Once);
    }

    [Fact]
    public void Disconnect_WhenNotConnected_DoesNothing()
    {
        _client.Disconnect();

        _client.IsConnected.Should().BeFalse();
        _mockNetworkClient.Verify(x => x.Close(), Times.Never);
    }

    [Fact]
    public async Task ToggleTransmissionAsync_WhenWriteSucceeds_CallsWriteAndReadOnce()
    {
        _mockNetworkClient
            .Setup(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNetworkClient
            .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3)
            .Callback<byte[], int, int, CancellationToken>((buf, off, _, _) =>
            {
                var nak = Encoding.ASCII.GetBytes("ACK");
                Array.Copy(nak, 0, buf, off, nak.Length);
            });

        await _client.ConnectAsync();
        await _client.ToggleTransmissionAsync(true);

        _mockNetworkClient.Verify(x => x.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockNetworkClient.Verify(x => x.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleTransmissionAsync_WhenNakReceived_LogsWarning()
    {
        _mockNetworkClient
            .Setup(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNetworkClient
            .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3)
            .Callback<byte[], int, int, CancellationToken>((buf, off, _, _) =>
            {
                var nak = Encoding.ASCII.GetBytes("NAK");
                Array.Copy(nak, 0, buf, off, nak.Length);
            });

        await _client.ConnectAsync();
        await _client.ToggleTransmissionAsync(false);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("NAK received")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ToggleTransmissionAsync_WhenWriteFails_ThrowsException()
    {
        _mockNetworkClient
            .Setup(x => x.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fail"));

        await _client.ConnectAsync();
        var act = () => _client.ToggleTransmissionAsync(true);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(123.4)]
    [InlineData(9876.54)]
    public async Task SetFrequencyAsync_FormatsCommandCorrectly(double frequency)
    {
        var client = new NetSdrTcpClient(networkClient: _mockNetworkClient.Object, logger: _mockLogger.Object);

        await client.ConnectAsync();

        var expected = $"SET_FREQUENCY {frequency:F2}";
        var expectedBytes = Encoding.ASCII.GetBytes(expected);

        await client.SetFrequencyAsync(frequency);

        _mockNetworkClient.Verify(x =>
                x.WriteAsync(
                    It.Is<byte[]>(buf =>
                        buf.Length >= expectedBytes.Length &&
                        buf.Take(expectedBytes.Length).SequenceEqual(expectedBytes)),
                    0,
                    It.Is<int>(c => c >= expectedBytes.Length),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true, "START_IQ")]
    [InlineData(false, "STOP_IQ")]
    public async Task ToggleTransmissionAsync_SendsCorrectCommand(bool on, string expectedCmd)
    {
        var client = new NetSdrTcpClient(networkClient: _mockNetworkClient.Object, logger: _mockLogger.Object);

        await client.ConnectAsync();

        _mockNetworkClient
            .Setup(x => x.WriteAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockNetworkClient
            .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), 0, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCmd.Length)
            .Callback<byte[], int, int, CancellationToken>((buf, offset, _, ct) =>
            {
                var bytes = Encoding.ASCII.GetBytes(expectedCmd);
                Array.Copy(bytes, 0, buf, offset, bytes.Length);
            });

        await client.ToggleTransmissionAsync(on);

        _mockNetworkClient.Verify(x =>
            x.WriteAsync(
                It.Is<byte[]>(buf =>
                    Encoding.ASCII.GetString(buf).StartsWith(expectedCmd)),
                0, It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            ), Times.Once);
    }

    [Fact]
    public async Task MultipleConcurrentConnects_OnlyInvokesConnectOnce()
    {
        var calls = 0;
        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref calls);
                return Task.Delay(50);
            });

        var tasks = Enumerable.Range(0, 5).Select(_ => _client.ConnectAsync()).ToArray();
        await Task.WhenAll(tasks);

        calls.Should().Be(1);
    }

    [Fact]
    public async Task MultipleConcurrentDisconnects_OnlyInvokesCloseOnce()
    {
        _mockNetworkClient
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await _client.ConnectAsync();

        var closes = 0;
        _mockNetworkClient
            .Setup(x => x.Close())
            .Callback(() => Interlocked.Increment(ref closes));

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(() => _client.Disconnect()))
            .ToArray();
        await Task.WhenAll(tasks);

        closes.Should().Be(1);
        _client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CallsNetworkClientDisposeOnlyOnce()
    {
        var disposals = 0;
        _mockNetworkClient
            .Setup(x => x.Dispose())
            .Callback(() => Interlocked.Increment(ref disposals));

        _client.Dispose();
        _client.Dispose();

        disposals.Should().Be(1);
    }

    #endregion
}