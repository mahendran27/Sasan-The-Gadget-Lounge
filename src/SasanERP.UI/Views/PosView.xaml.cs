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
}
