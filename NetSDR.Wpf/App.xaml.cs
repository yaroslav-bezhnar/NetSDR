using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NetSDR.Simulator.Interfaces;
using NetSDR.Simulator.Services;

namespace NetSDR.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            services.AddSingleton<ITcpSimulatorService, TcpSimulatorService>();

            var provider = services.BuildServiceProvider();
            var simulator = provider.GetRequiredService<ITcpSimulatorService>();
            simulator.Start();

            base.OnStartup(e);
        }
    }
}
