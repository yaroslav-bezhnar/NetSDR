using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace NetSDR.Client;

public class UdpDataReceiver(int udpPort = UdpDataReceiver.DefaultUdpPort,
                             UdpClient? udpClient = null,
                             ILogger<UdpDataReceiver>? logger = null) : IDisposable, IAsyncDisposable
{
    #region constants

    private const int DefaultUdpPort = 60000;
    private const int PacketSize = 4;

    #endregion

    #region fields

    private readonly UdpClient _udpClient = udpClient ?? new UdpClient(udpPort);
    private readonly ILogger<UdpDataReceiver>? _logger = logger;
    private CancellationTokenSource? _cts;

    #endregion

    #region methods

    public async Task StartReceivingAsync(string outputFilePath, CancellationToken cancellationToken = default)
    {
        if (_cts != null)
            throw new InvalidOperationException("Receiving is already started.");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger?.LogInformation("Receiving data.");

        await using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(_cts.Token);
                var bytes = result.Buffer;

                if (bytes.Length % PacketSize != 0)
                {
                    _logger?.LogWarning("Received data is not aligned to 4-byte packets.");
                    continue;
                }

                for (var i = 0; i < bytes.Length; i += PacketSize)
                {
                    var iValue = BitConverter.ToInt16(bytes, i);
                    var qValue = BitConverter.ToInt16(bytes, i + 2);

                    await fileStream.WriteAsync(bytes.AsMemory(i, PacketSize), _cts.Token);

                    _logger?.LogInformation($"Received IQ data pair: I = {iValue}, Q = {qValue}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogError("Receiving cancelled.");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error receiving data: {ex.Message}");
            throw;
        }
    }

    public void StopReceiving()
    {
        if (_cts == null) return;

        _cts.Cancel();
        _udpClient.Close();
        _logger?.LogInformation("Stopped receiving IQ data.");
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _udpClient.Dispose();
        _cts?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _udpClient.Dispose();
        await Task.CompletedTask;
    }

    #endregion
}