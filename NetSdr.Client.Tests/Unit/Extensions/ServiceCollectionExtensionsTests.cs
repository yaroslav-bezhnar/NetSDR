using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetSDR.Client.Extensions;
using NetSDR.Client.Interfaces;
using NetSDR.Client.Tcp;
using NetSDR.Client.Udp;

namespace NetSdr.Client.Test.Unit.Extensions;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceProvider _provider;

    public ServiceCollectionExtensionsTests()
    {
        var services = new ServiceCollection();
        services.AddNetSdrClient();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void NetSdrClient_IsRegisteredAsSingleton()
    {
        var a = _provider.GetRequiredService<INetSdrClient>();
        var b = _provider.GetRequiredService<INetSdrClient>();
        a.Should().BeSameAs(b);
        a.Should().BeOfType<NetSdrTcpClient>();
    }

    [Fact]
    public void TcpNetworkClient_IsRegisteredAsSingleton()
    {
        var a = _provider.GetRequiredService<ITcpNetworkClient>();
        var b = _provider.GetRequiredService<ITcpNetworkClient>();
        a.Should().BeSameAs(b);
        a.Should().BeOfType<TcpNetworkClient>();
    }

    [Fact]
    public void UdpDataReceiver_IsRegisteredAsSingleton()
    {
        var a = _provider.GetRequiredService<IUdpDataReceiver>();
        var b = _provider.GetRequiredService<IUdpDataReceiver>();
        a.Should().BeSameAs(b);
        a.Should().BeOfType<UdpDataReceiver>();
    }
}