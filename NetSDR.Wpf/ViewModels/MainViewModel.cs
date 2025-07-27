using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetSDR.Client.Tcp;
using NetSDR.Wpf.Infrastructure;

namespace NetSDR.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NetSdrTcpClient _tcpClient = new();

    private ThemeType _currentTheme = ThemeType.Dark;

    public MainViewModel()
    {
        StatusMessage = "Disconnected";
        Frequency = 1_000_000;
    }

    [ObservableProperty]
    private double frequency;

    [ObservableProperty]
    private string statusMessage;

    [ObservableProperty]
    private string? errorMessage;

    public bool IsConnected => _tcpClient.IsConnected;
    public bool IsDisconnected => !IsConnected;

    public ThemeType CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (SetProperty(ref _currentTheme, value))
                ApplyTheme();
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        CurrentTheme = CurrentTheme == ThemeType.Light
            ? ThemeType.Dark
            : ThemeType.Light;
    }

    [RelayCommand(CanExecute = nameof(IsDisconnected))]
    private async Task ConnectAsync()
    {
        ErrorMessage = null;

        try
        {
            await _tcpClient.ConnectAsync();
            UpdateStatus("Connected");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            UpdateStatus("Connection failed");
        }
        finally
        {
            OnIsConnectedChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private void Disconnect()
    {
        ErrorMessage = null;

        try
        {
            _tcpClient.Disconnect();
            UpdateStatus("Disconnected");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            OnIsConnectedChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task StartTransmissionAsync()
    {
        ErrorMessage = null;

        try
        {
            await _tcpClient.ToggleTransmissionAsync(true);
            UpdateStatus("Transmission started");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task StopTransmissionAsync()
    {
        ErrorMessage = null;

        try
        {
            await _tcpClient.ToggleTransmissionAsync(false);
            UpdateStatus("Transmission stopped");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(IsConnected))]
    private async Task SetFrequencyAsync()
    {
        ErrorMessage = null;

        try
        {
            await _tcpClient.SetFrequencyAsync(Frequency);
            UpdateStatus($"Frequency set to {Frequency:F2} Hz");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void OnIsConnectedChanged()
    {
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(IsDisconnected));

        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
        SetFrequencyCommand.NotifyCanExecuteChanged();
        StartTransmissionCommand.NotifyCanExecuteChanged();
        StopTransmissionCommand.NotifyCanExecuteChanged();
    }

    private void UpdateStatus(string message) => StatusMessage = message;

    private void ApplyTheme()
    {
        var uri = CurrentTheme switch
        {
            ThemeType.Dark => new Uri("Themes/DarkTheme.xaml", UriKind.Relative),
            _ => new Uri("Themes/LightTheme.xaml", UriKind.Relative)
        };

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        dictionaries.Clear();
        dictionaries.Add(new ResourceDictionary { Source = uri });
    }
}
