using SasanERP.UI.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace SasanERP.UI.Views;

/// <summary>
/// Interaction logic for CompanySettingsWindow.xaml
/// </summary>
public partial class CompanySettingsWindow : FluentWindow
{
    public CompanySettingsViewModel ViewModel { get; }

    public CompanySettingsWindow(CompanySettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.InitializeAsync();
        };
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
