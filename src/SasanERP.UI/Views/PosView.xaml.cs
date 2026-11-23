using System.Windows.Controls;
using System.Windows.Input;
using SasanERP.UI.ViewModels;

namespace SasanERP.UI.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
        Loaded += (s, e) => TxtBarcode.Focus();
        IsVisibleChanged += (s, e) =>
        {
            if (IsVisible)
            {
                Dispatcher.BeginInvoke(new Action(() => TxtBarcode.Focus()), System.Windows.Threading.DispatcherPriority.Input);
            }
        };
    }

    private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is PosViewModel vm)
            {
                vm.ProcessBarcodeScanCommand.Execute(null);
            }
        }
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not PosViewModel vm) return;

        switch (e.Key)
        {
            case Key.F2:
                vm.NewBillCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F3:
                TxtBarcode.Focus();
                TxtBarcode.SelectAll();
                e.Handled = true;
                break;
            case Key.F4:
                vm.OpenNewCustomerCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F5:
            case Key.F10:
                vm.PreviewInvoiceCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F6:
                vm.HoldCurrentBillCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F7:
                vm.RecallHeldBillCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F8:
                vm.ToggleGstMode();
                e.Handled = true;
                break;
            case Key.F9:
                vm.SetExactUpiCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F11:
                vm.ToggleManualEntryCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F12:
                vm.CheckoutAndPrintInvoiceCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
