using System.Net.Sockets;

namespace NetSDR.Client
{
    public class UdpDataReceiver(int udpPort = UdpDataReceiver.DefaultUdpPort) : IDisposable
    {
        #region constants

        private const int DefaultUdpPort = 60000;

        #endregion

        #region fields

        private readonly UdpClient _udpClient = new UdpClient(udpPort);
        private bool _isReceivingData;

        #endregion

        #region methods

        public async Task StartReceiving(string outputFilePath)
        {
            _isReceivingData = true;

            await using var fileStream = new FileStream(outputFilePath, FileMode.Create);

            Console.WriteLine("Receiving data.");

            while (_isReceivingData)
            {
                try
                {
                    const int packetSize = 4;
                    var data = await _udpClient.ReceiveAsync();
                    var bytes = data.Buffer;

                    if (bytes.Length % 4 != 0)
                    {
                        Console.WriteLine("Received data is not aligned to 4-byte packets.");
                        continue;
                    }

                    for (var i = 0; i < bytes.Length; i += packetSize)
                    {
                        var I = BitConverter.ToInt16(bytes, i);
                        var Q = BitConverter.ToInt16(bytes, i + 2);

                        var iqData = new byte[packetSize];
                        Array.Copy(bytes, i, iqData, 0, packetSize);
                        await fileStream.WriteAsync(iqData, 0, packetSize);

                        Console.WriteLine($"Received IQ data pair: I = {I}, Q = {Q}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving data: {ex.Message}");
                }
            }
        }

        public void StopReceiving()
        {
            _isReceivingData = false;
            _udpClient.Close();
            Console.WriteLine("Stopped receiving IQ data.");
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
        }

        #endregion
    }
}
