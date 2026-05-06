using AcmeUI.Services;
using AcmeUI.ViewModels;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinRT.Interop;

namespace AcmeUI;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_MAXIMIZE = 3;
    private const int SW_RESTORE = 9;

    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        this.InitializeComponent();
        RootGrid.DataContext = ViewModel;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);       

        appWindow.Title = "Flux Scanner ver. 1.0";

        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        {
            p.IsResizable = false;          // ❌ disabilita ridimensionamento
            p.IsMaximizable = false;        // ❌ rimuove il pulsante di massimizzazione
            p.IsMinimizable = true;         // ✔ tieni il pulsante "–"
        }

        //var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        ShowWindow(hWnd, SW_MAXIMIZE);

        ViewModel.AlarmRequested += OnAlarmRequested;

        this.Closed += OnMainWindowClosed;

    }

    private void OnMainWindowClosed(object sender, WindowEventArgs e)
    {
        // Se la finestra di configurazione è aperta → chiudila
        App.SettingsWindow?.Close();
    }

    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.LoadSettings();

        if (cfg.Port is null)
        {
            await ShowError("Nessuna porta configurata nelle impostazioni locali.");
            ViewModel.Status = "Errore: porta non configurata";
            return;
        }

        bool ok = SerialService.Instance.TryOpen(cfg.Port, cfg.BaudRate);

        if (!ok)
        {
            await ShowError($"Impossibile aprire la porta {cfg.Port}. Verifica che Arduino sia collegato.");
            ViewModel.Status = "Errore: impossibile aprire la porta";
            return;
        }

        await ConfigService.LoadArduinoParametersAsync();

        ViewModel.StartPosition = ConfigService.ToRelativePosition(ConfigService.Arduino.StartPosition);
        ViewModel.EndPosition = ConfigService.ToRelativePosition(ConfigService.Arduino.EndPosition);
        ViewModel.CenterPosition = ConfigService.ToRelativePosition(ConfigService.Arduino.CenterPosition);

        ViewModel.Status = "Connesso";       

    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSystemSettingsClick(object sender, RoutedEventArgs e)
    {
        if (App.SettingsWindow is not null)
        {
            // Porta la finestra in primo piano
            BringToFront(App.SettingsWindow);
            return;
        }

        var win = new Views.SettingsWindow();
        App.SettingsWindow = win;
        win.Activate();

        // Naviga alla pagina ArduinoSettingsPage
        win.ContentFrameControl.Navigate(typeof(Views.SettingsPages.ArduinoSettingsPage));
    }

    private void OnLocalSettingsClick(object sender, RoutedEventArgs e)
    {
        if (App.SettingsWindow is not null)
        {
            // Porta la finestra in primo piano
            BringToFront(App.SettingsWindow);
            return;
        }

        var win = new Views.SettingsWindow();
        App.SettingsWindow = win;
        win.Activate();

        // Naviga alla pagina LocalSettingsPage
        win.ContentFrameControl.Navigate(typeof(Views.SettingsPages.LocalSettingsPage));
    }

    private async void OnSaveCsvClick(object sender, RoutedEventArgs e)
    {
        await StorageService.Instance.SaveCSVAsync(ViewModel.ScanRows);
    }

    private async void OnOpenCsvClick(object sender, RoutedEventArgs e)
    {
        await StorageService.Instance.LoadCSVAsync(rows =>
        {
            ViewModel.ScanRows.Clear();
            foreach (var r in rows)
                ViewModel.ScanRows.Add(r);
            ViewModel.IntegrateValues();
            ViewModel.IsDataReady = true;
        });
    }

    private void OnSensorAnalysisClick(object sender, RoutedEventArgs e)
    {
        if (App.SensorAnalysisWindow is not null)
        {
            // Porta la finestra in primo piano
            BringToFront(App.SensorAnalysisWindow);
            return;
        }

        var win = new Views.SensorAnalysis();
        App.SensorAnalysisWindow = win;
        win.Activate();        
    }

    private async void OnAlarmRequested()
    {
        ContentDialog dialog = new()
        {
            Title = "⚡Allarme dispositivo",
            Content = "Il dispositivo ha segnalato una condizione di ALLARME.\nVerificare che il dispositivo risulti alimentato e che il pulsante di emergenza non sia stato premuto. Premere OK per continuare.",
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync();

        // Quando l’utente chiude il popup, riabilitiamo la gestione degli allarmi
        ViewModel.IsAlarmPopupOpen = false;
    }


    private async Task ShowError(string message)
    {
        ContentDialog dialog = new()
        {
            Title = "Errore",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };

        await dialog.ShowAsync().AsTask();
    }

    private void BringToFront(Window window)
    {
        IntPtr hwnd = WindowNative.GetWindowHandle(window);

        // Se la finestra è minimizzata → ripristina
        ShowWindow(hwnd, SW_RESTORE);

        // Porta in primo piano
        SetForegroundWindow(hwnd);
    }

}

public class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 180, 0));
    public Brush FalseBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool b && b) ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
