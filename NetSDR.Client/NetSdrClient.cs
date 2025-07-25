using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace NetSDR.Client;

public class NetSdrClient : IDisposable
{
    #region constants

    private const int DefaultTcpPort = 50000;
    private const string DefaultAddress = "127.0.0.1";
    private const string Nak = "NAK";

    #endregion

    #region fields

    private readonly int _tcpPort;
    private readonly string _host;
    private readonly INetworkClient _networkClient;
    private readonly IAsyncPolicy _resiliencePolicy;
    private readonly ILogger<NetSdrClient>? _logger;

    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly SemaphoreSlim _disconnectLock = new(1, 1);
    private Task? _connectTask;

    private bool _disposed;

    #endregion

    #region events

    public event Action? OnTransmissionStarted;

    #endregion

    #region properties

    public bool IsConnected { get; private set; }

    #endregion

    #region constructors

    public NetSdrClient(string host = DefaultAddress,
                        int tcpPort = DefaultTcpPort,
                        TimeSpan? timeout = null,
                        INetworkClient? networkClient = null,
                        ILogger<NetSdrClient>? logger = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _tcpPort = tcpPort;
        _networkClient = networkClient ?? new NetworkClient();
        _logger = logger;

        var retryPolicy = Policy
            .Handle<SocketException>()
            .Or<TaskCanceledException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timespan, attempt, _) =>
                {
                    _logger?.LogWarning(exception, "Connect attempt {Attempt} failed. Retrying in {Delay}s", attempt, timespan.TotalSeconds);
                });

        var timeoutPolicy = Policy
            .TimeoutAsync(
                timeout ?? TimeSpan.FromSeconds(5),
                TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (_, timespan, _, _) =>
                {
                    _logger?.LogError("Timeout after {Timeout}s while connecting.", timespan.TotalSeconds);
                    return Task.CompletedTask;
                });

        _resiliencePolicy = Policy.WrapAsync(timeoutPolicy, retryPolicy);
    }

    #endregion

    #region methods

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger?.LogInformation("Already connected.");
            return;
        }

        await _connectLock.WaitAsync(cancellationToken);

        try
        {
            if (IsConnected)
                return;

            if (_connectTask != null)
            {
                await _connectTask;
                return;
            }

            _connectTask = _resiliencePolicy.ExecuteAsync(async ct =>
            {
                await _networkClient.ConnectAsync(_host, _tcpPort, ct);
                IsConnected = true;
                _logger?.LogInformation("Connected to {Host}:{Port}", _host, _tcpPort);
            }, cancellationToken);

            await _connectTask;
        }
        finally
        {
            _connectTask = null;
            _connectLock.Release();
        }
    }

    public void Disconnect()
    {
        DisconnectAsync().GetAwaiter().GetResult();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger?.LogInformation("Not connected.");
            return;
        }

        await _disconnectLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsConnected)
            {
                _logger?.LogInformation("Not connected.");
                return;
            }

            _networkClient.Close();
            IsConnected = false;
            _logger?.LogInformation("Disconnected successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error while disconnecting.");
            throw;
        }
        finally
        {
            _disconnectLock.Release();
        }
    }

    public async Task ToggleTransmissionAsync(bool start, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger?.LogWarning("Not connected.");
            return;
        }

        try
        {
            var command = start ? "START_IQ" : "STOP_IQ";
            var bytes = Encoding.ASCII.GetBytes(command);

            await _networkClient.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

            if (start)
            {
                OnTransmissionStarted?.Invoke();
            }

            await HandleResponseAsync(cancellationToken);

            _logger?.LogInformation("Transmission {Status}", start ? "started" : "stopped");
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException)
        {
            _logger?.LogError(ex, "IO error during ToggleTransmissionAsync");
            throw;
        }
    }

    public async Task SetFrequencyAsync(double frequency, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger?.LogWarning("Not connected.");
            return;
        }

        try
        {
            var command = $"SET_FREQUENCY {frequency:F2}";
            var bytes = Encoding.ASCII.GetBytes(command);

            await _networkClient.WriteAsync(bytes, 0, bytes.Length, cancellationToken);

            await HandleResponseAsync(cancellationToken);

            _logger?.LogInformation($"New frequency is '{frequency}' Hz.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting frequency.");
            throw;
        }
    }

    private async Task HandleResponseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var responseBuffer = new byte[1024];
            var bytesRead = await _networkClient.ReadAsync(responseBuffer, 0, responseBuffer.Length, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

            if (response.Contains(Nak))
            {
                HandleNak();
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException)
        {
            _logger?.LogError(ex, "IO error during HandleResponseAsync");
            throw;
        }
    }

    private void HandleNak()
    {
        _logger?.LogWarning("NAK received.");

        // Additional logic
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _networkClient.Dispose();
        }

        _disposed = true;
    }

    #endregion
}