namespace NetSDR.Simulator.Interfaces;

public interface INetworkSimulatorService
{
    #region properties

    int Port { get; }
    bool IsRunning { get; }

    #endregion

    #region methods

    void Start(int port = -1);
    void Stop();

    void Restart()
    {
        Stop();
        Start(Port);
    }

    #endregion
}