using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetSDR.Simulator.Interfaces;

namespace NetSDR.Web.Pages;

public class SimulatorControlModel : PageModel
{
    private readonly ITcpSimulatorService _tcp;
    private readonly IUdpSimulatorService _udp;

    public SimulatorControlModel(ITcpSimulatorService tcp, IUdpSimulatorService udp)
    {
        _tcp = tcp;
        _udp = udp;
    }

    public bool TcpRunning => _tcp.IsRunning;
    public int TcpPort => _tcp.Port;

    public bool UdpRunning => _udp.IsRunning;
    public int UdpPort => _udp.Port;

    public IActionResult OnPostToggleTcp()
    {
        if (_tcp.IsRunning)
            _tcp.Stop();
        else
            _tcp.Start();

        return RedirectToPage();
    }

    public IActionResult OnPostToggleUdp()
    {
        if (_udp.IsRunning)
            _udp.Stop();
        else
            _udp.Start();

        return RedirectToPage();
    }

    public IActionResult OnPostRestartTcp()
    {
        _tcp.Restart();
        return RedirectToPage();
    }

    public IActionResult OnPostRestartUdp()
    {
        _udp.Restart();
        return RedirectToPage();
    }
}