using NetSDR.Client.Models;

namespace NetSDR.Client.Interfaces;

public interface IUdpDataReceiver : IDisposable, IAsyncDisposable
{
    event Action<IQSample[]>? SamplesReceived;

    Task<ConnectionResult> StartReceivingAsync(string outputFilePath, CancellationToken cancellationToken = default);
    void StopReceiving();
}