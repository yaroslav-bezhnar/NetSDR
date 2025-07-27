using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using NetSDR.Client.Models;
using NetSDR.Client.Udp;

namespace NetSdr.Client.Tests.Unit.Udp;

public class UdpDataReceiverTests
{
    private readonly int _port;
    private readonly UdpDataReceiver _receiver;

    public UdpDataReceiverTests()
    {
        var serverUdp = new UdpClient(0);
        _port = ((IPEndPoint) serverUdp.Client.LocalEndPoint!).Port;
        _receiver = new UdpDataReceiver(_port, serverUdp, NullLogger<UdpDataReceiver>.Instance);
    }

    [Fact]
    public async Task StartReceivingAsync_ValidAlignedData_WritesFileAndRaisesSamples()
    {
        var receivedSamples = new List<IQSample[]>();
        _receiver.SamplesReceived += samples => receivedSamples.Add(samples);

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dat");

        try
        {
            var task = _receiver.StartReceivingAsync(tempFile, CancellationToken.None);

            var expected = new[] { new IQSample(1, -1), new IQSample(2, -2) };
            var buffer = CreatePacket(expected);
            using var client = new UdpClient();

            await client.SendAsync(buffer, buffer.Length, new IPEndPoint(IPAddress.Loopback, _port));

            await Task.Delay(100);
            _receiver.StopReceiving();
            var result = await task;

            _receiver.Dispose();

            Assert.True(result.IsSuccess);
            Assert.Contains(result.Message, new[] { "Receiving was cancelled.", "Receiving completed." });

            Assert.Single(receivedSamples);
            var actual = receivedSamples[0];
            Assert.Equal(expected.Length, actual.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].I, actual[i].I);
                Assert.Equal(expected[i].Q, actual[i].Q);
            }

            var written = await File.ReadAllBytesAsync(tempFile);
            Assert.Equal(buffer, written);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task StartReceivingAsync_MisalignedData_SkipsSamplesAndWritesNothing()
    {
        var invoked = false;
        _receiver.SamplesReceived += _ => invoked = true;

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dat");

        try
        {
            var task = _receiver.StartReceivingAsync(tempFile, CancellationToken.None);

            var badBuffer = new byte[5];
            using var client = new UdpClient();
            await client.SendAsync(badBuffer, badBuffer.Length, new IPEndPoint(IPAddress.Loopback, _port));

            await Task.Delay(100);
            _receiver.StopReceiving();
            var result = await task;

            _receiver.Dispose();

            Assert.True(result.IsSuccess);
            Assert.Equal("Receiving was cancelled.", result.Message);
            Assert.False(invoked);

            Assert.Equal(0, new FileInfo(tempFile).Length);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task StartReceivingAsync_AlreadyStarted_ReturnsFailureImmediately()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dat");
        var cts = new CancellationTokenSource();

        var firstTask = _receiver.StartReceivingAsync(tempFile, cts.Token);
        var secondResult = await _receiver.StartReceivingAsync("unused.dat", CancellationToken.None);

        cts.Cancel();
        await firstTask;

        _receiver.Dispose();

        Assert.False(secondResult.IsSuccess);
        Assert.Equal("Receiving is already started.", secondResult.Message);

        if (File.Exists(tempFile)) File.Delete(tempFile);
    }

    [Fact]
    public async Task StopReceiving_WithoutAnyPacket_CompletesWithCancelled()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dat");

        try
        {
            var task = _receiver.StartReceivingAsync(tempFile, CancellationToken.None);
            await Task.Delay(50);

            _receiver.StopReceiving();
            var result = await task;

            _receiver.Dispose();

            Assert.True(result.IsSuccess);
            Assert.Equal("Receiving was cancelled.", result.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    private static byte[] CreatePacket(IQSample[] samples)
    {
        var buf = new byte[samples.Length * 4];
        for (var i = 0; i < samples.Length; i++)
        {
            BitConverter.GetBytes(samples[i].I).CopyTo(buf, i * 4);
            BitConverter.GetBytes(samples[i].Q).CopyTo(buf, i * 4 + 2);
        }

        return buf;
    }
}
