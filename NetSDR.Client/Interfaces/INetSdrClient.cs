namespace NetSDR.Client.Interfaces;

/// <summary>
/// Defines a contract for managing a NetSDR client, including connection handling, transmission control, and frequency configuration.
/// </summary>
public interface INetSdrClient : IDisposable
{
    #region properties

    /// <summary>
    /// Gets a value indicating whether the client is currently connected.
    /// </summary>
    bool IsConnected { get; }

    #endregion

    #region events

    /// <summary>
    /// Occurs when transmission (TX) starts.
    /// </summary>
    event Action? OnTransmissionStarted;

    #endregion

    #region methods

    /// <summary>
    /// Asynchronously establishes a connection to the NetSDR server.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to abort the operation.</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously disconnects from the NetSDR server.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to abort the operation.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts or stops transmission (TX) mode.
    /// </summary>
    /// <param name="start">True to start transmission; false to stop.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the operation.</param>
    Task ToggleTransmissionAsync(bool start, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the transmission frequency.
    /// </summary>
    /// <param name="frequency">Target frequency in Hertz.</param>
    /// <param name="cancellationToken">Optional cancellation token to abort the operation.</param>
    /// <returns></returns>
    Task SetFrequencyAsync(double frequency, CancellationToken cancellationToken = default);

    #endregion
}