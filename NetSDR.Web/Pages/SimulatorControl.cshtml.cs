using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetSDR.Simulator.Interfaces;

namespace NetSDR.Web.Pages;

public class SimulatorControlModel(ITcpSimulatorService tcp, IUdpSimulatorService udp) : PageModel
{
    #region properties

    public bool TcpRunning => tcp.IsRunning;
    public int TcpPort => tcp.Port;

    public bool UdpRunning => udp.IsRunning;
    public int UdpPort => udp.Port;

    #endregion

    #region methods

    public IActionResult OnPostToggleTcp()
    {
        if (tcp.IsRunning)
            tcp.Stop();
        else
            tcp.Start();

        return RedirectToPage();
    }

    public IActionResult OnPostToggleUdp()
    {
        if (udp.IsRunning)
            udp.Stop();
        else
            udp.Start();

        return RedirectToPage();
    }

    public IActionResult OnPostRestartTcp()
    {
        tcp.Restart();
        return RedirectToPage();
    }

    public IActionResult OnPostRestartUdp()
    {
        udp.Restart();
        return RedirectToPage();
    }

    #endregion
}