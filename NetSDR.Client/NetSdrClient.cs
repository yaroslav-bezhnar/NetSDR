using System.Net.Sockets;
using System.Text;

namespace NetSDR.Client
{
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

        #endregion

        #region events

        public event Action OnTransmissionStarted;

        #endregion

        #region properties

        public bool IsConnected { get; private set; }

        #endregion

        #region constructors

        public NetSdrClient(string host = DefaultAddress,
                            int tcpPort = DefaultTcpPort,
                            INetworkClient networkClient = null)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _tcpPort = tcpPort;

            _networkClient = networkClient ?? new NetworkClient();
        }

        #endregion

        #region methods

        public async Task ConnectAsync()
        {
            if (IsConnected)
            {
                Console.WriteLine("Already connected.");
                return;
            }

            try
            {
                await _networkClient.ConnectAsync(_host, _tcpPort);

                IsConnected = true;

                Console.WriteLine($"Connected to {_host}:{_tcpPort}");

                // _ = Task.Run(async () => await ListenForUnsolicitedMessagesAsync();
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }

            try
            {
                _networkClient?.Close();

                IsConnected = false;

                Console.WriteLine("Disconnected successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public async Task ToggleTransmissionAsync(bool start)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }

            try
            {
                var command = start ? "START_IQ" : "STOP_IQ";
                var bytes = Encoding.ASCII.GetBytes(command);

                await _networkClient.WriteAsync(bytes, 0, bytes.Length);

                if (start)
                {
                    OnTransmissionStarted?.Invoke();
                }

                await HandleResponseAsync();

                Console.WriteLine($"Transmission {(start ? "started" : "stopped")} ");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public async Task SetFrequencyAsync(double frequency)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Not connected.");
                return;
            }

            try
            {
                var command = $"SET_FREQUENCY {frequency:F2}";
                var bytes = Encoding.ASCII.GetBytes(command);

                await _networkClient.WriteAsync(bytes, 0, bytes.Length);

                await HandleResponseAsync();

                Console.WriteLine($"New frequency is '{frequency}' Hz.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error while setting frequency: {ex.Message}");
            }
        }

        private async Task HandleResponseAsync()
        {
            try
            {
                var responseBuffer = new byte[1024];
                var bytesRead = await _networkClient.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                var response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

                if (response.Contains(Nak))
                {
                    HandleNak();
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        //private async Task ListenForUnsolicitedMessagesAsync() // CancellationToken cancellationToken
        //{
        //    var buffer = new byte[1024];
        //    while (/*!cancellationToken.IsCancellationRequested*/ IsConnected)
        //    {
        //       // cancellationToken.ThrowIfCancellationRequested();

        //        try
        //        {
        //            // Additional logic

        //        }
        //        catch (OperationCanceledException)
        //        {
        //            Console.WriteLine("Listening message was canceled.");
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error while listening messages: {ex.Message}");
        //        }
        //    }
        //}

        private void HandleNak()
        {
            Console.WriteLine("NAK received.");

            // Additional logic
        }

        public void Dispose()
        {
            _networkClient?.Dispose();
        }

        #endregion
    }
}
