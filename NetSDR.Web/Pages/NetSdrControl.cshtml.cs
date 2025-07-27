using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetSDR.Client.Interfaces;
using NetSDR.Core.Extensions;

namespace NetSDR.Web.Pages;

public class NetSdrControlModel : PageModel
{
    #region fields

    private readonly INetSdrClient _client;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<NetSdrControlModel> _logger;
    private readonly ITcpNetworkClient _tcp;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
    private readonly IUdpDataReceiver _udp;

    #endregion

    #region properties

    [BindProperty]
    public double Frequency { get; set; } = 1_000_000;

    [BindProperty]
    public string Host { get; set; } = "127.0.0.1";

    [BindProperty]
    public int Port { get; set; } = 5000;

    [BindProperty]
    public bool StartTransmission { get; set; }

    [BindProperty]
    public string TcpMessage { get; set; } = "Hello NetSDR";

    [BindProperty]
    public string OutputPath { get; set; } = "output.dat";

    public string StatusMessage { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public string? ReceivedMessage { get; private set; }

    public bool IsConnected => _client.IsConnected;
    public bool IsTcpConnected { get; private set; }
    public bool IsUdpReceiving { get; private set; }

    #endregion

    #region constructors

    public NetSdrControlModel(INetSdrClient client,
                              ITcpNetworkClient tcp,
                              IUdpDataReceiver udp,
                              ILogger<NetSdrControlModel> logger)
    {
        _client = client;
        _tcp = tcp;
        _udp = udp;
        _logger = logger;

        _client.OnTransmissionStarted += () => _logger.LogInformation("Transmission started");
        _udp.SamplesReceived += samples => _logger.LogInformation("Received {Count} samples", samples.Length);
    }

    #endregion

    #region methods

    public void OnGet() => UpdateStatus();

    public async Task<IActionResult> OnPostConnectAsync()
    {
        ErrorMessage = null;

        try
        {
            await _client.ConnectAsync()
                .WithTimeoutAsync(_timeout, _cts);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"NetSDR Connect Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnect()
    {
        ErrorMessage = null;

        try
        {
            await _client.DisconnectAsync()
                .WithTimeoutAsync(_timeout, _cts);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"NetSDR Disconnect Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleTransmissionAsync()
    {
        ErrorMessage = null;

        try
        {
            await _client.ToggleTransmissionAsync(StartTransmission)
                .WithTimeoutAsync(_timeout, _cts);
        }
        catch (Exception ex)
        {
            var action = StartTransmission ? "Start" : "Stop";
            ErrorMessage = $"{action} Transmission Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostSetFrequencyAsync()
    {
        ErrorMessage = null;

        try
        {
            await _client.SetFrequencyAsync(Frequency)
                .WithTimeoutAsync(_timeout, _cts);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Set Frequency Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostTcpConnectAsync()
    {
        ErrorMessage = null;

        try
        {
            var success = await _tcp.ConnectAsync(Host, Port, _cts.Token)
                .WithTimeoutAsync(_timeout, _cts);

            IsTcpConnected = success;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"TCP Connect Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostTcpWriteAsync()
    {
        ErrorMessage = null;

        try
        {
            var data = Encoding.UTF8.GetBytes(TcpMessage);
            await _tcp.WriteAsync(data, 0, data.Length, _cts.Token)
                .WithTimeoutAsync(_timeout, _cts);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"TCP Write Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostTcpReadAsync()
    {
        ErrorMessage = null;

        try
        {
            var buffer = new byte[1024];
            var bytesRead = await _tcp.ReadAsync(buffer, 0, buffer.Length, _cts.Token)
                .WithTimeoutAsync(_timeout, _cts);

            if (bytesRead is null)
                ErrorMessage = "TCP Read Timeout";
            else
                ReceivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead.Value);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"TCP Read Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public IActionResult OnPostTcpClose()
    {
        _tcp.Close();
        IsTcpConnected = false;

        UpdateStatus();
        return Page();
    }

    public async Task<IActionResult> OnPostStartUdpAsync()
    {
        ErrorMessage = null;

        try
        {
            var result = await _udp.StartReceivingAsync(OutputPath)
                .WithTimeoutAsync(_timeout, _cts);

            if (!result)
                ErrorMessage = $"UDP Start Error: {result}";
            else
                IsUdpReceiving = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"UDP Start Error: {ex.Message}";
        }

        UpdateStatus();
        return Page();
    }

    public IActionResult OnPostStopUdp()
    {
        _udp.StopReceiving();
        IsUdpReceiving = false;

        UpdateStatus();
        return Page();
    }

    private void UpdateStatus() => StatusMessage = _client.IsConnected ? "Connected" : "Disconnected";

    #endregion
}