using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SasanERP.UI.ViewModels;
using SasanERP.UI.Views;
using Wpf.Ui.Controls;

namespace SasanERP.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Provides global keyboard listeners for F2, F3, F4, F7, F9 and scanner auto-focus.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    public PosViewModel ViewModel { get; }

    public MainWindow(PosViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.InitializeAsync();
            TxtBarcode.Focus();
        };
    }

    /// <summary>
    /// Global keyboard shortcuts handler for POS cashier operations
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F2:
                ViewModel.NewBillCommand.Execute(null);
                TxtBarcode.Focus();
                e.Handled = true;
                break;

            case Key.F3:
                OpenIntakeStudio();
                e.Handled = true;
                break;

            case Key.F4:
                ViewModel.PrintBarcodeLabelCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F7:
                ViewModel.ToggleGstModeCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F9:
                ViewModel.CheckoutAndPrintInvoiceCommand.Execute(null);
                TxtBarcode.Focus();
                e.Handled = true;
                break;

            case Key.Escape:
                TxtBarcode.Focus();
                e.Handled = true;
                break;
        }
    }

    private void BtnNewBill_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NewBillCommand.Execute(null);
        TxtBarcode.Focus();
    }

    private void BtnOpenIntake_Click(object sender, RoutedEventArgs e)
    {
        OpenIntakeStudio();
    }

    private void BtnPrintSticker_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PrintBarcodeLabelCommand.Execute(null);
    }

    private void OpenIntakeStudio()
    {
        var window = _serviceProvider.GetRequiredService<InventoryIntakeWindow>();
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// Triggers immediate barcode lookup when scanner sends CR/Enter
    /// </summary>
    private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ViewModel.ProcessBarcodeScanCommand.Execute(null);
            e.Handled = true;
        }
    }
}
