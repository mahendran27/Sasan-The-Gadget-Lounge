using SasanERP.UI.ViewModels;
using Wpf.Ui.Controls;

namespace SasanERP.UI.Views;

/// <summary>
/// Interaction logic for WirelessStudioWindow.xaml
/// </summary>
public partial class WirelessStudioWindow : FluentWindow
{
    public WirelessStudioViewModel ViewModel { get; }

    public WirelessStudioWindow(WirelessStudioViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;
        InitializeComponent();
    }
}
