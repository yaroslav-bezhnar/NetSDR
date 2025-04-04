namespace NetSDR.Client
{
    public interface INetworkClient : IDisposable
    {
        #region methods

        Task ConnectAsync(string host, int port);
        Task WriteAsync(byte[] buffer, int offset, int count);
        Task<int> ReadAsync(byte[] buffer, int offset, int count);
        void Close();

        #endregion
    }
}
