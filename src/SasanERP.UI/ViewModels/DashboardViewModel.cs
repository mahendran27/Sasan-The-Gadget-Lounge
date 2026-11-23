using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Analytics;
using SasanERP.Services.Printing;

namespace SasanERP.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ExecutiveDashboardService _dashboardService;
    private readonly PeriodAnalyticsService _periodAnalyticsService;
    private readonly ExecutiveReportPdfEngine _reportPdfEngine;
    private readonly AppDbContext _context;

    public event Action<NavigationSection>? NavigationRequested;

    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private int _todayInvoicesCount;

    [ObservableProperty]
    private decimal _todayNetProfit;

    [ObservableProperty]
    private decimal _todayCash;

    [ObservableProperty]
    private decimal _todayUpi;

    [ObservableProperty]
    private decimal _todayCard;

    [ObservableProperty]
    private decimal _todayFinance;

    [ObservableProperty]
    private decimal _todayExchange;

    [ObservableProperty]
    private decimal _stockValuationNew;

    [ObservableProperty]
    private decimal _stockValuationPreOwned;

    [ObservableProperty]
    private decimal _totalStockCost;

    [ObservableProperty]
    private decimal _totalStockValuationCombined;

    [ObservableProperty]
    private int _inStockDevicesCount;

    [ObservableProperty]
    private int _inStockBulkItemsCount;

    [ObservableProperty]
    private int _lowStockAlertsCount;

    [ObservableProperty]
    private int _stockAgingAlertsCount;

    [ObservableProperty]
    private int _nearAgingDevicesCount;

    [ObservableProperty]
    private decimal _customerCreditsReceivable;

    [ObservableProperty]
    private decimal _monthSales;

    [ObservableProperty]
    private decimal _monthMarginProfit;

    [ObservableProperty]
    private decimal _monthMarginGst;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DashboardTimeframe _selectedTimeframe = DashboardTimeframe.Today;

    [ObservableProperty]
    private string _selectedTimeframeLabel = "TODAY'S REVENUE";

    [ObservableProperty]
    private decimal _periodSales;

    [ObservableProperty]
    private int _periodInvoicesCount;

    [ObservableProperty]
    private decimal _periodNetProfit;

    [ObservableProperty]
    private decimal _periodCash;

    [ObservableProperty]
    private decimal _periodUpi;

    [ObservableProperty]
    private decimal _periodCard;

    [ObservableProperty]
    private decimal _averageTicketSize;

    [ObservableProperty]
    private decimal _marginPercentage;

    [ObservableProperty]
    private string _selectedRadarTab = "Aging";

    [ObservableProperty]
    private bool _hasRecentInvoices;

    [ObservableProperty]
    private bool _hasAgingDevices;

    [ObservableProperty]
    private bool _hasLowStockItems;

    [ObservableProperty]
    private bool _hasNearAgingDevices;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ObservableCollection<RecentInvoiceSummary> RecentInvoices { get; } = new();
    public ObservableCollection<RecentInvoiceSummary> AllTimeRecentInvoices { get; } = new();
    public ObservableCollection<AgingDeviceSummary> AgingDevices { get; } = new();
    public ObservableCollection<AgingDeviceSummary> NearAgingDevices { get; } = new();
    public ObservableCollection<LowStockItemSummary> LowStockItems { get; } = new();

    public DashboardViewModel(
        ExecutiveDashboardService dashboardService,
        PeriodAnalyticsService periodAnalyticsService,
        ExecutiveReportPdfEngine reportPdfEngine,
        AppDbContext context)
    {
        _dashboardService = dashboardService;
        _periodAnalyticsService = periodAnalyticsService;
        _reportPdfEngine = reportPdfEngine;
        _context = context;
    }

    [RelayCommand]
    public async Task RefreshMetricsAsync()
    {
        IsLoading = true;
        try
        {
            var metrics = await _dashboardService.GetRealtimeMetricsAsync(SelectedTimeframe);

            TodaySales = metrics.TodaySalesTotal;
            TodayInvoicesCount = metrics.TodayInvoicesCount;
            TodayNetProfit = metrics.TodayNetProfitEstimated;
            TodayCash = metrics.TodayCashCollected;
            TodayUpi = metrics.TodayUpiCollected;
            TodayCard = metrics.TodayCardCollected;
            TodayFinance = metrics.TodayFinanceCollected;
            TodayExchange = metrics.TodayExchangeDeductions;

            PeriodSales = metrics.PeriodSalesTotal;
            PeriodInvoicesCount = metrics.PeriodInvoicesCount;
            PeriodNetProfit = metrics.PeriodNetProfitEstimated;
            PeriodCash = metrics.PeriodCashCollected;
            PeriodUpi = metrics.PeriodUpiCollected;
            PeriodCard = metrics.PeriodCardCollected;

            AverageTicketSize = PeriodInvoicesCount > 0 ? Math.Round(PeriodSales / PeriodInvoicesCount, 2) : 0;
            MarginPercentage = PeriodSales > 0 ? Math.Round((PeriodNetProfit / PeriodSales) * 100, 1) : 0;

            SelectedTimeframeLabel = SelectedTimeframe switch
            {
                DashboardTimeframe.Yesterday => "YESTERDAY'S REVENUE",
                DashboardTimeframe.ThisWeek => "THIS WEEK'S REVENUE",
                DashboardTimeframe.ThisMonth => "THIS MONTH'S REVENUE",
                DashboardTimeframe.AllTime => "ALL-TIME REVENUE",
                _ => "TODAY'S REVENUE"
            };

            StockValuationNew = metrics.TotalStockValuationNew;
            StockValuationPreOwned = metrics.TotalStockValuationPreOwned;
            TotalStockCost = metrics.TotalStockCostValuation;
            TotalStockValuationCombined = metrics.TotalStockValuationCombined;

            InStockDevicesCount = metrics.TotalInStockDevicesCount;
            InStockBulkItemsCount = metrics.TotalInStockBulkItemsCount;
            LowStockAlertsCount = metrics.LowStockAlertsCount;
            StockAgingAlertsCount = metrics.StockAgingAlertsCount;
            NearAgingDevicesCount = metrics.NearAgingDevicesCount;
            CustomerCreditsReceivable = metrics.CustomerCreditsReceivableTotal;

            MonthSales = metrics.MonthSalesTotal;
            MonthMarginProfit = metrics.MonthRule32_5MarginProfit;
            MonthMarginGst = metrics.MonthMarginGstPayable;

            RecentInvoices.Clear();
            foreach (var inv in metrics.RecentInvoices) RecentInvoices.Add(inv);

            AllTimeRecentInvoices.Clear();
            if (metrics.AllTimeRecentInvoices != null)
            {
                foreach (var inv in metrics.AllTimeRecentInvoices) AllTimeRecentInvoices.Add(inv);
            }

            AgingDevices.Clear();
            foreach (var dev in metrics.AgingDevices) AgingDevices.Add(dev);

            NearAgingDevices.Clear();
            if (metrics.NearAgingDevices != null)
            {
                foreach (var dev in metrics.NearAgingDevices) NearAgingDevices.Add(dev);
            }

            LowStockItems.Clear();
            foreach (var item in metrics.LowStockItems) LowStockItems.Add(item);

            HasRecentInvoices = RecentInvoices.Count > 0;
            HasAgingDevices = AgingDevices.Count > 0;
            HasLowStockItems = LowStockItems.Count > 0;
            HasNearAgingDevices = NearAgingDevices.Count > 0;

            // Smart tab selection: if no aging devices but items are low on stock, prioritize LowStock tab
            if (!HasAgingDevices && HasLowStockItems && SelectedRadarTab == "Aging")
            {
                SelectedRadarTab = "LowStock";
            }

            StatusMessage = $"Updated {DateTime.Now:hh:mm:ss tt} ({metrics.SelectedTimeframeLabel})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Metrics refresh error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SetTimeframeAsync(string timeframe)
    {
        if (Enum.TryParse<DashboardTimeframe>(timeframe, true, out var tf))
        {
            SelectedTimeframe = tf;
            await RefreshMetricsAsync();
        }
    }

    [RelayCommand]
    public void SelectRadarTab(string tab)
    {
        SelectedRadarTab = tab;
    }

    [RelayCommand]
    public async Task PrintDailyShiftReportAsync()
    {
        IsLoading = true;
        try
        {
            var now = DateTime.Now;
            var fromUtc = now.Date.ToUniversalTime();
            var toUtc = now.Date.AddDays(1).ToUniversalTime();
            var report = await _periodAnalyticsService.ComputePeriodReportAsync(fromUtc, toUtc);
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();

            string tempDir = Path.Combine(Path.GetTempPath(), "SasanERP_Reports");
            Directory.CreateDirectory(tempDir);
            string targetPath = Path.Combine(tempDir, $"Sasan_Shift_ZReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            await Task.Run(() => _reportPdfEngine.GeneratePeriodSalesReportPdf(report, company, targetPath));
            PdfViewerLauncher.Launch(targetPath);
            StatusMessage = $"Shift Z-Report generated: {Path.GetFileName(targetPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Z-Report generation error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void NavigateToPos() => NavigationRequested?.Invoke(NavigationSection.PosBilling);

    [RelayCommand]
    public void NavigateToStock() => NavigationRequested?.Invoke(NavigationSection.InventoryRegistry);

    [RelayCommand]
    public void NavigateToIntake() => NavigationRequested?.Invoke(NavigationSection.DeviceIntake);

    [RelayCommand]
    public void NavigateToDaybook() => NavigationRequested?.Invoke(NavigationSection.DaybookLedger);

    [RelayCommand]
    public void NavigateToGodMode() => NavigationRequested?.Invoke(NavigationSection.GodMode);
}
