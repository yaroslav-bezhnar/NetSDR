namespace NetSDR.Client.Interfaces;

/// <summary>
/// Represents a network client capable of connecting, sending, and receiving data asynchronously.
/// </summary>
public interface ITcpNetworkClient : IDisposable
{
    #region methods

    /// <summary>
    /// Asynchronously connects to a remote host at the specified address and port.
    /// </summary>
    /// <param name="host">The remote host address.</param>
    /// <param name="port">The remote port number.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous connect operation.</returns>
    Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the connection and releases any associated resources.
    /// </summary>
    void Close();

    /// <summary>
    /// Asynchronously writes data to the network stream.
    /// </summary>
    /// <param name="buffer">The buffer containing data to send.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the network stream.</param>
    /// <param name="count">The number of bytes to write.</param>
    /// <param name="cancellationToken">A token to cancel the write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously reads data from the network stream.
    /// </summary>
    /// <param name="buffer">The buffer to store the read data.</param>
    /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the network stream.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <param name="cancellationToken">A token to cancel the read operation.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the number of bytes read.</returns>
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    #endregion
}
