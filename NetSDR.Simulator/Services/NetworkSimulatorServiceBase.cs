using NetSDR.Simulator.Interfaces;

namespace NetSDR.Simulator.Services;

public abstract class NetworkSimulatorServiceBase : INetworkSimulatorService
{
    #region fields

    private CancellationTokenSource? _cts;

    #endregion

    #region properties

    protected abstract int DefaultPort { get; }

    public int Port { get; private set; }
    public bool IsRunning { get; private set; }

    #endregion

    #region methods

    public void Start(int port)
    {
        if (IsRunning) return;

        Port = port == -1 ? DefaultPort : port;
        _cts = new CancellationTokenSource();
        IsRunning = true;

        Task.Run(() => RunAsync(_cts.Token));
        Console.WriteLine($"{GetType().Name} started on port {Port}");
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        StopInternal();

        _cts = null;
        IsRunning = false;

        Console.WriteLine($"{GetType().Name} stopped");
    }

    private protected abstract Task RunAsync(CancellationToken token);
    private protected abstract void StopInternal();

    #endregion
}