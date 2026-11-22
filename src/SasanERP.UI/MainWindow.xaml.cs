using System.Windows.Input;
using SasanERP.UI.ViewModels;
using Wpf.Ui.Controls;

namespace SasanERP.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Provides global keyboard listeners for F2, F4, F9 and scanner auto-focus.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public PosViewModel ViewModel { get; }

    public MainWindow(PosViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
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

            case Key.F4:
                ViewModel.PrintBarcodeLabelCommand.Execute(null);
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
