using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NetSDR.Client.Interfaces;
using NetSDR.Client.Models;

namespace NetSDR.Client.Udp;

public sealed class UdpDataReceiver(int udpPort = UdpDataReceiver.DefaultUdpPort,
                                    UdpClient ? udpClient = null,
                                    ILogger<UdpDataReceiver> ? logger = null) : IUdpDataReceiver
{
    #region constants

    private const int DefaultUdpPort = 60000;
    private const int PacketSize = 4;

    #endregion

    #region fields

    private readonly UdpClient _udpClient = udpClient ?? new UdpClient(udpPort);
    private readonly ILogger<UdpDataReceiver>? _logger = logger;
    private CancellationTokenSource? _cts;
    private FileStream? _fileStream;

    #endregion

    #region events

    public event Action<IQSample[]>? SamplesReceived;

    #endregion

    #region methods

    public async Task<ConnectionResult> StartReceivingAsync(string outputFilePath, CancellationToken cancellationToken = default)
    {
        if (_cts != null)
            return new ConnectionResult(false, "Receiving is already started.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger?.LogInformation("Receiving data...");

        _fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(_cts.Token);
                var buffer = result.Buffer;

                if (buffer.Length % PacketSize != 0)
                {
                    _logger?.LogWarning("Received data is not aligned to 4-byte packets.");
                    continue;
                }

                await _fileStream.WriteAsync(buffer, _cts.Token);
                var samples = ParsePacket(buffer);

                SamplesReceived?.Invoke(samples);
                _logger?.LogInformation("Received {Count} IQ samples.", samples.Length);
            }

            return new ConnectionResult(true, "Receiving completed.");
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Receiving cancelled.");
            return new ConnectionResult(true, "Receiving was cancelled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error receiving data.");
            return new ConnectionResult(false, ex.Message);
        }
    }

    public void StopReceiving()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _logger?.LogInformation("Stopped receiving IQ data.");
    }

    public void Dispose()
    {
        StopReceiving();

        _fileStream?.Dispose();
        _cts?.Dispose();
        _udpClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        StopReceiving();

        if (_fileStream != null)
        {
            await _fileStream.DisposeAsync();
            _fileStream = null;
        }

        _cts?.Dispose();
        _udpClient.Dispose();
    }

    private static IQSample[] ParsePacket(byte[] buffer)
    {
        var count = buffer.Length / PacketSize;
        var samples = new IQSample[count];

        for (var i = 0; i < count; i++)
        {
            var iVal = BitConverter.ToInt16(buffer, i * PacketSize);
            var qVal = BitConverter.ToInt16(buffer, i * PacketSize + 2);
            samples[i] = new IQSample(iVal, qVal);
        }

        return samples;
    }

    #endregion
}