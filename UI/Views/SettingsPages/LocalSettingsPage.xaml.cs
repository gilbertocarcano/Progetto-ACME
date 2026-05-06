using AcmeUI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace AcmeUI.Views.SettingsPages
{
    public sealed partial class LocalSettingsPage : Page
    {
        public LocalSettingsViewModel ViewModel { get; } = new();

        public LocalSettingsPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
