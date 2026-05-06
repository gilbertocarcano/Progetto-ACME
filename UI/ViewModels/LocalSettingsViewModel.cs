using AcmeUI.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using Windows.Devices.Enumeration;

namespace AcmeUI.ViewModels;

public class LocalSettingsViewModel : BaseViewModel
{
    public ObservableCollection<string> AvailablePorts { get; } = new();

    public ObservableCollection<int> BaudRates { get; } =
        new()
        {
            9600, 19200, 38400, 57600, 115200,
            230400, 250000, 500000, 1000000, 2000000
        };

    private string? _selectedPort;
    public string? SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    private int _baudRate = 250000;
    public int BaudRate
    {
        get => _baudRate;
        set => SetProperty(ref _baudRate, value);
    }

    private DeviceWatcher? _watcher;
    
    public IRelayCommand SaveSettingsCommand { get; }

    public LocalSettingsViewModel()
    {        
        SaveSettingsCommand = new RelayCommand(SaveSettings);

        RefreshPorts();
        StartPortWatcher();
        LoadSettings();
    }

    // 🔄 Refresh manuale
    private void RefreshPorts()
    {
        AvailablePorts.Clear();

        foreach (var port in SerialPort.GetPortNames())
            AvailablePorts.Add(port);

        // Se la porta salvata non esiste più, seleziona la prima disponibile
        if (SelectedPort != null && !AvailablePorts.Contains(SelectedPort))
            SelectedPort = AvailablePorts.FirstOrDefault();
    }

    // 🔄 Refresh automatico tramite DeviceWatcher
    private void StartPortWatcher()
    {
        if (App.MainAppWindow == null)
            return;

        string selector =
            "System.Devices.InterfaceClassGuid:=\"{86E0D1E0-8089-11D0-9CE4-08003E301F73}\"";

        _watcher = DeviceInformation.CreateWatcher(selector);

        _watcher.Added += (_, __) =>
            App.MainAppWindow.DispatcherQueue.TryEnqueue(RefreshPorts);

        _watcher.Removed += (_, __) =>
            App.MainAppWindow.DispatcherQueue.TryEnqueue(RefreshPorts);

        _watcher.Updated += (_, __) =>
            App.MainAppWindow.DispatcherQueue.TryEnqueue(RefreshPorts);

        _watcher.Start();
    }



    // 💾 Salvataggio impostazioni
    private void SaveSettings()
    {
        ConfigService.SaveSettings(SelectedPort, BaudRate);

        if (SelectedPort is not null)
            SerialService.Instance.Open(SelectedPort, BaudRate);
        SerialService.Instance.SendCommand("GET_STEPSPERUNIT"); //Un comando qualsiasi per riallineare seriale

    }

    // 📥 Caricamento impostazioni
    private void LoadSettings()
    {
        var cfg = ConfigService.LoadSettings();

        SelectedPort = cfg.Port;
        BaudRate = cfg.BaudRate;
    }
}
