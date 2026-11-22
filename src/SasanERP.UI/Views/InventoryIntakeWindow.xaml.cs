using SasanERP.UI.ViewModels;
using Wpf.Ui.Controls;

namespace SasanERP.UI.Views;

/// <summary>
/// Interaction logic for InventoryIntakeWindow.xaml
/// </summary>
public partial class InventoryIntakeWindow : FluentWindow
{
    public InventoryIntakeViewModel ViewModel { get; }

    public InventoryIntakeWindow(InventoryIntakeViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.LoadSuppliersAsync();
        };
    }
}
