using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetSDR.Client;

public class Program
{
    private static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Shutdown requested...");
        };

        // Create DI container with logging
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    })
                    .SetMinimumLevel(LogLevel.Debug);
            })
            .BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var receiverLogger = serviceProvider.GetRequiredService<ILogger<UdpDataReceiver>>();

        logger.LogInformation("Starting test server...");

        // Start TCP server
        var serverTask = RunServerAsync(cts.Token);

        // Start SDR client logic
        try
        {
            var client = new NetSdrClient("127.0.0.1", 50000);
            await using var receiver = new UdpDataReceiver(logger: receiverLogger);

            client.OnTransmissionStarted += async () =>
            {
                _ = receiver.StartReceivingAsync("iq_test.bin", cts.Token);
            };

            await client.ConnectAsync(cts.Token);
            await client.ToggleTransmissionAsync(true, cts.Token);

            await Task.Delay(1000, cts.Token);

            await client.SetFrequencyAsync(1_000_000, cts.Token);
            await client.ToggleTransmissionAsync(false, cts.Token);
            client.Disconnect();
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred.");
        }

        logger.LogInformation("Client shutdown complete.");
        await cts.CancelAsync();

        await serverTask;
    }

    private static async Task RunServerAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 50000);
        listener.Start();

        Console.WriteLine("TCP server started.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    await using var stream = client.GetStream();
                    var buffer = new byte[1024];

                    try
                    {
                        while (true)
                        {
                            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                            if (bytesRead == 0) break;

                            var received = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                            var response = "test"; // or "NAK" to simulate rejection
                            var responseBytes = Encoding.ASCII.GetBytes(response);
                            await stream.WriteAsync(responseBytes.AsMemory(), cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"TCP server error: {ex.Message}");
                    }
                    finally
                    {
                        client.Close();
                    }
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("TCP server cancelled.");
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("TCP server stopped.");
        }
    }
}
