using System.Net.Sockets;

namespace NetSDR.Client
{
    public class NetworkClient : INetworkClient
    {
        #region fields

        private readonly TcpClient _tcpClient = new();
        private NetworkStream _networkStream;

        #endregion

        #region methods

        public async Task ConnectAsync(string host, int port)
        {
            await _tcpClient.ConnectAsync(host, port);
            _networkStream = _tcpClient.GetStream();
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count)
        {
            EnsureConnected();

            await _networkStream.WriteAsync(buffer, offset, count);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            EnsureConnected();

            return await _networkStream.ReadAsync(buffer, offset, count);
        }

        public void Close()
        {
            _networkStream.Close();
            _tcpClient.Close();
        }

        private void EnsureConnected()
        {
            if (!_tcpClient.Connected)
            {
                throw new InvalidOperationException("Not connected.");
            }
        }

        public void Dispose()
        {
            _tcpClient?.Dispose();
            _networkStream?.Dispose();
        }

        #endregion
    }
}
