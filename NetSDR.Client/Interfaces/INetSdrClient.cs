namespace NetSDR.Client.Interfaces;

public interface INetSdrClient : IDisposable
{
    bool IsConnected { get; }

    event Action? OnTransmissionStarted;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task ToggleTransmissionAsync(bool start, CancellationToken cancellationToken = default);
    Task SetFrequencyAsync(double frequency, CancellationToken cancellationToken = default);
}
