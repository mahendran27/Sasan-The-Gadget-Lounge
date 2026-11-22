using System.Windows.Controls;
using SasanERP.UI.ViewModels;

namespace SasanERP.UI.Views;

public partial class InvoiceHistoryView : UserControl
{
    public InvoiceHistoryView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is InvoiceHistoryViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }
}
