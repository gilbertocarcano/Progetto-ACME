using AcmeUI.Models;
using AcmeUI.Services;
using AcmeUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AcmeUI.Views.SettingsPages;

public sealed partial class ArduinoSettingsPage : Page
{
    public ArduinoSettingsViewModel ViewModel { get; } = new();

    public ArduinoSettingsPage()
    {
        InitializeComponent();
        this.DataContext = ViewModel;
        Loaded += ArduinoSettingsPage_Loaded;
    }

    private void OnRemoveHRRangeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is HRRange range)
        {
            ViewModel.RemoveHRRange(range);
        }
    }

    private async void ArduinoSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        var cfg = ConfigService.LoadSettings();

        if (cfg.Port is null)
        {
            await ShowError("Nessuna porta configurata nelle impostazioni locali.");
            return;
        }

        bool ok = SerialService.Instance.TryOpen(cfg.Port, cfg.BaudRate);

        if (!ok)
        {
            await ShowError($"Impossibile aprire la porta {cfg.Port}. Verifica che Arduino sia collegato.");
            return;
        }

        // Se la porta è aperta → carica i valori da Arduino
        ViewModel.LoadFromArduinoCommand.Execute(null);
    }

    private async void OnCalibrateClick(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        { 
            Title = "⚠️ Conferma calibrazione",
            Content = "La procedura effettua il rilievo automatico delle tensioni di zero offset dei sensori di sistema. " +
                      "Assicurarsi di porre il sistema in condizioni di minime interferenze magnetiche.",
            PrimaryButtonText = "Calibra",
            CloseButtonText = "Annulla",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Chiama il comando del ViewModel
            ViewModel.CalibrateCommand.Execute(null);
        }
    }

    private async Task ShowError(string message)
    {
        ContentDialog dialog = new()
        {
            Title = "Errore",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var vm = (ArduinoSettingsViewModel)DataContext;

        var errors = vm.ValidateBeforeSave();

        if (errors.Any())
        {
            var dialog = new ContentDialog
            {
                Title = "⚠️ Errori di validazione",
                Content = string.Join("\n", errors),
                PrimaryButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
            return; // ❗ niente salvataggio
        }

        // Nessun errore → salva
        await vm.SaveToArduinoAsync();

        // Chiudi la finestra di configurazione
        App.SettingsWindow?.Close();
    }

}
