using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetSDR.Client;

namespace NetSDR.Web.Pages
{
    public class NetSdrControlModel : PageModel
    {
        private readonly NetSdrClient _client;

        [BindProperty]
        public double Frequency { get; set; } = 1_000_000;

        public string StatusMessage { get; private set; } = "Disconnected";
        public string? ErrorMessage { get; private set; }
        public bool IsConnected => _client.IsConnected;

        public NetSdrControlModel(NetSdrClient client)
        {
            _client = client;
        }

        public void OnGet()
        {
            UpdateStatus();
        }

        public async Task<IActionResult> OnPostConnectAsync()
        {
            ErrorMessage = null;

            try
            {
                await _client.ConnectAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            UpdateStatus();
            return Page();
        }

        public IActionResult OnPostDisconnect()
        {
            ErrorMessage = null;

            try
            {
                _client.Disconnect();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            UpdateStatus();
            return Page();
        }

        public async Task<IActionResult> OnPostStartTransmissionAsync()
        {
            ErrorMessage = null;

            try
            {
                await _client.ToggleTransmissionAsync(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            UpdateStatus();
            return Page();
        }

        public async Task<IActionResult> OnPostStopTransmissionAsync()
        {
            ErrorMessage = null;

            try
            {
                await _client.ToggleTransmissionAsync(false);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            UpdateStatus();
            return Page();
        }

        public async Task<IActionResult> OnPostSetFrequencyAsync()
        {
            ErrorMessage = null;

            try
            {
                await _client.SetFrequencyAsync(Frequency);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }

            UpdateStatus();
            return Page();
        }

        private void UpdateStatus() => StatusMessage = _client.IsConnected ? "Connected" : "Disconnected";
    }
}
