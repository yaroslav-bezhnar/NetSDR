using Microsoft.Extensions.DependencyInjection;
using NetSDR.Client.Interfaces;
using NetSDR.Client.Tcp;
using NetSDR.Client.Udp;

namespace NetSDR.Client.Extensions;

public static class ServiceCollectionExtensions
{
    #region methods

    public static IServiceCollection AddNetSdrClient(this IServiceCollection services)
    {
        services.AddSingleton<ITcpNetworkClient, TcpNetworkClient>();
        services.AddSingleton<INetSdrClient, NetSdrTcpClient>();
        services.AddSingleton<IUdpDataReceiver, UdpDataReceiver>();

        return services;
    }

    #endregion
}