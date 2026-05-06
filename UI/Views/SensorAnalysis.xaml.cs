using AcmeUI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Threading.Tasks;

namespace AcmeUI.Views
{
    public sealed partial class SensorAnalysis : Window
    {
        public SensorAnalysis()
        {
            InitializeComponent();

            RootGrid.DataContext = new SensorAnalysisViewModel(this);

            this.Closed += (_, __) => App.SensorAnalysisWindow = null;
        }

        public async Task<bool?> ShowAcquisitionDialogAsync(float current)
        {
            var text = new TextBlock();
            text.TextWrapping = TextWrapping.Wrap;

            text.Inlines.Add(new Run { Text = "Impostare la corrente a " });
            text.Inlines.Add(new Run { Text = $"{current} mA", FontWeight = FontWeights.Bold });
            text.Inlines.Add(new Run { Text = " e premere Acquisisci." });            
            if (current <= 0)
            {
                text.Inlines.Add(new LineBreak());
                text.Inlines.Add(new Run { Text = "Per ottenere correnti negative " });
                text.Inlines.Add(new Run { Text = "INVERTIRE LA POLARITA' DI ALIMENTAZIONE DEL SOLENOIDE.", FontWeight = FontWeights.Bold });
                
            }
            var dialog = new ContentDialog
            {
                Title = "Acquisizione",
                Content = text,                
                PrimaryButtonText = "Acquisisci",
                CloseButtonText = "Annulla",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync().AsTask();
            return result == ContentDialogResult.Primary;
        }
    }
}
