using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetSDR.Client.Interfaces;
using NetSDR.Client.Tcp;
using NetSDR.Client.Udp;
using NetSDR.Core.Extensions;
using NetSDR.Simulator;
using Spectre.Console;

var services = new ServiceCollection();

services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

services.AddSingleton<INetSdrClient, NetSdrTcpClient>();
services.AddSingleton<ITcpNetworkClient, TcpNetworkClient>();
services.AddSingleton<IUdpDataReceiver, UdpDataReceiver>();
services.AddSingleton<ITcpSimulatorService, TcpSimulatorService>();
services.AddSingleton<IUdpSimulatorService, UdpSimulatorService>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();

provider.GetRequiredService<ITcpSimulatorService>().Start();
provider.GetRequiredService<IUdpSimulatorService>().Start();

var client = provider.GetRequiredService<INetSdrClient>();
var tcp = provider.GetRequiredService<ITcpNetworkClient>();
var udp = provider.GetRequiredService<IUdpDataReceiver>();

client.OnTransmissionStarted += () => logger.LogInformation("Transmission started");
udp.SamplesReceived += samples => logger.LogInformation("Received {Count} samples", samples.Length);

var cts = new CancellationTokenSource();

var isTcpConnected = false;
var isUdpReceiving = false;
var timeout = TimeSpan.FromSeconds(5);

while (true)
{
    var choices = new List<string> { "Exit" };

    if (!client.IsConnected)
        choices.Add("NetSdrTcpClient => Connect");
    else
        choices.AddRange([
            "NetSdrTcpClient => Disconnect",
            "NetSdrTcpClient => Set Frequency",
            "NetSdrTcpClient => Toggle Transmission",
            "NetSdrTcpClient => Show IsConnected"
        ]);

    if (!isTcpConnected)
        choices.Add("TcpNetworkClient => Connect");
    else
        choices.AddRange([
            "TcpNetworkClient => Write",
            "TcpNetworkClient => Read",
            "TcpNetworkClient => Close"
        ]);

    choices.Add(!isUdpReceiving ? "UdpDataReceiver => Start Receiving" : "UdpDataReceiver => Stop Receiving");

    var selected = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Choose operation:")
            .PageSize(12)
            .AddChoices(choices));

    if (selected == "Exit") break;

    try
    {
        switch (selected)
        {
            case "NetSdrTcpClient => Connect":
                if (!await client.ConnectAsync(cts.Token).WithTimeoutAsync(timeout, cts))
                    AnsiConsole.MarkupLine("[red]Timeout during Connect[/]");
                break;

            case "NetSdrTcpClient => Disconnect":
                if (!await client.DisconnectAsync(cts.Token).WithTimeoutAsync(timeout, cts))
                    AnsiConsole.MarkupLine("[red]Timeout during Disconnect[/]");
                break;

            case "NetSdrTcpClient => Set Frequency":
                var frequency = AnsiConsole.Ask<double>("Enter frequency:");
                if (!await client.SetFrequencyAsync(frequency, cts.Token).WithTimeoutAsync(timeout, cts))
                    AnsiConsole.MarkupLine("[red]Timeout setting frequency[/]");
                break;

            case "NetSdrTcpClient => Toggle Transmission":
                var startTx = AnsiConsole.Confirm("Start transmission?");
                if (!await client.ToggleTransmissionAsync(startTx, cts.Token).WithTimeoutAsync(timeout, cts))
                    AnsiConsole.MarkupLine("[red]Timeout toggling transmission[/]");
                break;

            case "NetSdrTcpClient => Show IsConnected":
                AnsiConsole.MarkupLine($"Connected: [bold]{(client.IsConnected ? "[green]Yes[/]" : "[red]No[/]")}[/]");
                break;

            case "TcpNetworkClient => Connect":
                var host = AnsiConsole.Ask<string>("Enter host:");
                var port = AnsiConsole.Ask<int>("Enter port:");
                if (await tcp.ConnectAsync(host, port, cts.Token).WithTimeoutAsync(timeout, cts))
                    isTcpConnected = true;
                else AnsiConsole.MarkupLine("[red]Timeout during TCP Connect[/]");
                break;

            case "TcpNetworkClient => Write":
                var msg = AnsiConsole.Ask<string>("Enter message:");
                var data = Encoding.UTF8.GetBytes(msg);
                if (!await tcp.WriteAsync(data, 0, data.Length, cts.Token).WithTimeoutAsync(timeout, cts))
                    AnsiConsole.MarkupLine("[red]Timeout during Write[/]");
                break;

            case "TcpNetworkClient => Read":
                var buffer = new byte[1024];
                var bytesRead = await tcp.ReadAsync(buffer, 0, buffer.Length, cts.Token)
                    .WithTimeoutAsync(timeout, cts);

                if (bytesRead is null)
                    AnsiConsole.MarkupLine("[red]Timeout during Read[/]");
                else
                {
                    var received = Encoding.UTF8.GetString(buffer, 0, bytesRead.Value);
                    AnsiConsole.MarkupLine($"Received: [italic]{received}[/]");
                }
                break;

            case "TcpNetworkClient => Close":
                tcp.Close();
                isTcpConnected = false;
                break;

            case "UdpDataReceiver => Start Receiving":
                var path = AnsiConsole.Ask<string>("Enter output file path:");
                var result = await udp.StartReceivingAsync(path, cts.Token)
                    .WithTimeoutAsync(timeout, cts);

                if (result)
                    AnsiConsole.MarkupLine("[red]Timeout during UDP Start[/]");
                else
                    isUdpReceiving = true;
                break;

            case "UdpDataReceiver => Stop Receiving":
                udp.StopReceiving();
                isUdpReceiving = false;
                break;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Operation failed");
        AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
    }
}