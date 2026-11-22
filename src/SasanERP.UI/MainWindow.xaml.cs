using System.Windows;
using System.Windows.Input;
using SasanERP.UI.ViewModels;
using Wpf.Ui.Controls;

namespace SasanERP.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Coordinates enterprise ERP navigation across Dashboard, POS, Stock Registry, Intake, Daybook, GST &amp; Wireless Hub.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) =>
        {
            await ViewModel.InitializeAsync();
        };
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F1:
                ViewModel.NavigateToSection(NavigationSection.Dashboard);
                e.Handled = true;
                break;

            case Key.F2:
                ViewModel.NavigateToSection(NavigationSection.PosBilling);
                e.Handled = true;
                break;

            case Key.F3:
                ViewModel.NavigateToSection(NavigationSection.InventoryRegistry);
                e.Handled = true;
                break;

            case Key.F4:
                ViewModel.NavigateToSection(NavigationSection.InvoiceHistory);
                e.Handled = true;
                break;

            case Key.F5:
                ViewModel.NavigateToSection(NavigationSection.DeviceIntake);
                e.Handled = true;
                break;

            case Key.F6:
                ViewModel.NavigateToSection(NavigationSection.DaybookLedger);
                e.Handled = true;
                break;

            case Key.F7:
                ViewModel.NavigateToSection(NavigationSection.GstReports);
                e.Handled = true;
                break;

            case Key.F8:
                ViewModel.NavigateToSection(NavigationSection.WirelessStudio);
                e.Handled = true;
                break;

            case Key.F9:
                if (ViewModel.CurrentSection != NavigationSection.PosBilling)
                {
                    ViewModel.NavigateToSection(NavigationSection.PosBilling);
                }
                ViewModel.PosVM.CheckoutAndPrintInvoiceCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F10:
                ViewModel.NavigateToSection(NavigationSection.StoreSettings);
                e.Handled = true;
                break;

            case Key.F11:
                ViewModel.SettingsVM.CreateInstantBackupCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F12:
                ViewModel.NavigateToSection(NavigationSection.GodMode);
                e.Handled = true;
                break;
        }
    }

    private void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.Dashboard);
    }

    private void BtnNavPos_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.PosBilling);
    }

    private void BtnNavInventory_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.InventoryRegistry);
    }

    private void BtnNavInvoices_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.InvoiceHistory);
    }

    private void BtnNavIntake_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.DeviceIntake);
    }

    private void BtnNavDaybook_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.DaybookLedger);
    }

    private void BtnNavGstReports_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.GstReports);
    }

    private void BtnNavWireless_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.WirelessStudio);
    }

    private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.StoreSettings);
    }

    private void BtnNavGodMode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NavigateToSection(NavigationSection.GodMode);
    }
}
