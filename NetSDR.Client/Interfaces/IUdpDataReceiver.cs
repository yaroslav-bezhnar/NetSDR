using NetSDR.Client.Models;

namespace NetSDR.Client.Interfaces;

/// <summary>
/// Defines a contract for receiving UDP-based IQ sample data from a NetSDR source.
/// Supports asynchronous start, graceful shutdown, and real-time sample delivery.
/// </summary>
public interface IUdpDataReceiver : IDisposable, IAsyncDisposable
{
    #region events

    /// <summary>
    /// Triggered when a new block of IQ samples is received from the UDP stream.
    /// </summary>
    event Action<IQSample[]>? SamplesReceived;

    #endregion

    #region methods

    /// <summary>
    /// Begins asynchronously receiving data from the UDP source and writes raw samples to the specified output file.
    /// </summary>
    /// <param name="outputFilePath">The full path to the file where samples will be stored.</param>
    /// <param name="cancellationToken">Optional token to cancel the operation.</param>
    /// <returns>A <see cref="ConnectionResult"/> indicating success or error details.</returns>
    Task<ConnectionResult> StartReceivingAsync(string outputFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the active UDP data reception and releases associated resources.
    /// </summary>
    void StopReceiving();

    #endregion
}