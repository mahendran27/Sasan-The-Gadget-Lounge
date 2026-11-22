using System.Windows.Controls;
using SasanERP.UI.ViewModels;

namespace SasanERP.UI.Views;

public partial class GodModeView : UserControl
{
    public GodModeView()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is GodModeViewModel vm)
            {
                await vm.InitializeAsync();
            }
        };
    }
}
