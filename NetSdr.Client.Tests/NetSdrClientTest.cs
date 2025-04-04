using Moq;
using System.Net.Sockets;
using NetSDR.Client;
using System.Text;

namespace NetSdr.Client.Test
{
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
                .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            await _netSdrClient.ConnectAsync();

            Assert.True(_netSdrClient.IsConnected);
            _mockNetworkClient.Verify(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task ConnectAsync_WhenAlreadyConnected_DoesNotTryToConnectAgain()
        {
            await _netSdrClient.ConnectAsync();
            _mockNetworkClient
                .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            await _netSdrClient.ConnectAsync();

            Assert.True(_netSdrClient.IsConnected);
            _mockNetworkClient.Verify(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void ConnectAsync_WhenSocketExceptionThrown_PrintsErrorMessage()
        {
            _mockNetworkClient
                .Setup(client => client.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new SocketException());

            var ex = Assert.ThrowsAsync<SocketException>(async () => await _netSdrClient.ConnectAsync());

            Assert.NotNull(ex);
        }

        [Fact]
        public void Disconnect_WhenConnected_DisconnectsSuccessfully()
        {
            _netSdrClient.ConnectAsync().Wait();
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
            _netSdrClient.ConnectAsync().Wait();

            _mockNetworkClient.Setup(client => client.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            await _netSdrClient.ToggleTransmissionAsync(true);

            _mockNetworkClient.Verify(client => client.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task SetFrequencyAsync_WhenNotConnected_LogsMessage()
        {
            await _netSdrClient.SetFrequencyAsync(1000);

            _mockNetworkClient.Verify(client => client.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SetFrequencyAsync_WhenConnected_CallsWriteAsyncWithCorrectCommand()
        {
            _netSdrClient.ConnectAsync().Wait();

            double frequency = 1000.0;
            var expectedCommand = $"SET_FREQUENCY {frequency:F2}";

            await _netSdrClient.SetFrequencyAsync(frequency);

            _mockNetworkClient.Verify(client => client.WriteAsync(It.Is<byte[]>(buffer => Encoding.ASCII.GetString(buffer) == expectedCommand),
                It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        }
    }
}