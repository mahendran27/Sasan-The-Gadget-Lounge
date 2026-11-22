using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SasanERP.Data.Context;
using SasanERP.Services.Wireless;

namespace SasanERP.UI.ViewModels;

public enum NavigationSection
{
    Dashboard,
    PosBilling,
    InventoryRegistry,
    InvoiceHistory,
    DeviceIntake,
    DaybookLedger,
    GstReports,
    WirelessStudio,
    StoreSettings
}

public partial class MainViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    public DashboardViewModel DashboardVM { get; }
    public PosViewModel PosVM { get; }
    public InventoryViewModel InventoryVM { get; }
    public InvoiceHistoryViewModel InvoicesVM { get; }
    public InventoryIntakeViewModel IntakeVM { get; }
    public DaybookViewModel DaybookVM { get; }
    public GstReportsViewModel GstReportsVM { get; }
    public WirelessStudioViewModel WirelessVM { get; }
    public CompanySettingsViewModel SettingsVM { get; }

    [ObservableProperty]
    private NavigationSection _currentSection = NavigationSection.Dashboard;

    [ObservableProperty]
    private object _currentViewModel;

    [ObservableProperty]
    private string _storeName = "SASAN THE GADGET LOUNGE";

    [ObservableProperty]
    private string _storeAddress = "106/7, Karthik Mahall, Subban St, Theni, Tamil Nadu - 625531";

    [ObservableProperty]
    private string _activeCashier = "Karthik Raja (Manager)";

    [ObservableProperty]
    private string _currentTimeString = DateTime.Now.ToString("dd-MMM-yyyy | hh:mm tt");

    [ObservableProperty]
    private string _globalStatus = "🟢 ACID Engine: ONLINE (SQLite) | 📡 RFID/NFC: READY | 🖨️ Vector Print: READY";

    public MainViewModel(
        AppDbContext context,
        DashboardViewModel dashboardVM,
        PosViewModel posVM,
        InventoryViewModel inventoryVM,
        InvoiceHistoryViewModel invoicesVM,
        InventoryIntakeViewModel intakeVM,
        DaybookViewModel daybookVM,
        GstReportsViewModel gstReportsVM,
        WirelessStudioViewModel wirelessVM,
        CompanySettingsViewModel settingsVM,
        UnifiedWirelessManager? wirelessManager = null)
    {
        _context = context;
        DashboardVM = dashboardVM;
        PosVM = posVM;
        InventoryVM = inventoryVM;
        InvoicesVM = invoicesVM;
        IntakeVM = intakeVM;
        DaybookVM = daybookVM;
        GstReportsVM = gstReportsVM;
        WirelessVM = wirelessVM;
        SettingsVM = settingsVM;

        _currentViewModel = dashboardVM;

        if (wirelessManager != null)
        {
            wirelessManager.OnTagTappedForCheckout += (imei, freq) =>
            {
                // When an NFC/RFID tag is tapped, switch directly to POS screen!
                NavigateToSection(NavigationSection.PosBilling);
            };
        }
    }

    public async Task InitializeAsync()
    {
        var company = await _context.CompanyProfiles.FirstOrDefaultAsync();
        if (company != null)
        {
            StoreName = company.BusinessName;
            StoreAddress = $"{company.AddressLine1}, {company.City}, {company.State} - {company.Pincode}";
        }

        // Initialize default view
        await DashboardVM.RefreshMetricsAsync();
    }

    [RelayCommand]
    public void NavigateToSection(NavigationSection section)
    {
        CurrentSection = section;
        CurrentViewModel = section switch
        {
            NavigationSection.Dashboard => DashboardVM,
            NavigationSection.PosBilling => PosVM,
            NavigationSection.InventoryRegistry => InventoryVM,
            NavigationSection.InvoiceHistory => InvoicesVM,
            NavigationSection.DeviceIntake => IntakeVM,
            NavigationSection.DaybookLedger => DaybookVM,
            NavigationSection.GstReports => GstReportsVM,
            NavigationSection.WirelessStudio => WirelessVM,
            NavigationSection.StoreSettings => SettingsVM,
            _ => DashboardVM
        };

        // Auto trigger data load when switching tabs
        if (section == NavigationSection.Dashboard) _ = DashboardVM.RefreshMetricsAsync();
        else if (section == NavigationSection.InventoryRegistry) _ = InventoryVM.LoadInventoryAsync();
        else if (section == NavigationSection.InvoiceHistory) _ = InvoicesVM.SearchInvoicesAsync();
        else if (section == NavigationSection.DaybookLedger) _ = DaybookVM.LoadDaybookAsync();
        else if (section == NavigationSection.GstReports) _ = GstReportsVM.LoadGstDataAsync();
        else if (section == NavigationSection.PosBilling) _ = PosVM.InitializeAsync();
    }
}
