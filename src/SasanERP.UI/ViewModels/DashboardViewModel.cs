using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SasanERP.Services.Analytics;

namespace SasanERP.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ExecutiveDashboardService _dashboardService;

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
    private int _inStockDevicesCount;

    [ObservableProperty]
    private int _inStockBulkItemsCount;

    [ObservableProperty]
    private int _lowStockAlertsCount;

    [ObservableProperty]
    private int _stockAgingAlertsCount;

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

    public ObservableCollection<RecentInvoiceSummary> RecentInvoices { get; } = new();
    public ObservableCollection<AgingDeviceSummary> AgingDevices { get; } = new();
    public ObservableCollection<LowStockItemSummary> LowStockItems { get; } = new();

    public DashboardViewModel(ExecutiveDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [RelayCommand]
    public async Task RefreshMetricsAsync()
    {
        IsLoading = true;
        try
        {
            var metrics = await _dashboardService.GetRealtimeMetricsAsync();

            TodaySales = metrics.TodaySalesTotal;
            TodayInvoicesCount = metrics.TodayInvoicesCount;
            TodayNetProfit = metrics.TodayNetProfitEstimated;
            TodayCash = metrics.TodayCashCollected;
            TodayUpi = metrics.TodayUpiCollected;
            TodayCard = metrics.TodayCardCollected;
            TodayFinance = metrics.TodayFinanceCollected;
            TodayExchange = metrics.TodayExchangeDeductions;

            StockValuationNew = metrics.TotalStockValuationNew;
            StockValuationPreOwned = metrics.TotalStockValuationPreOwned;
            TotalStockCost = metrics.TotalStockCostValuation;
            InStockDevicesCount = metrics.TotalInStockDevicesCount;
            InStockBulkItemsCount = metrics.TotalInStockBulkItemsCount;
            LowStockAlertsCount = metrics.LowStockAlertsCount;
            StockAgingAlertsCount = metrics.StockAgingAlertsCount;
            CustomerCreditsReceivable = metrics.CustomerCreditsReceivableTotal;

            MonthSales = metrics.MonthSalesTotal;
            MonthMarginProfit = metrics.MonthRule32_5MarginProfit;
            MonthMarginGst = metrics.MonthMarginGstPayable;

            RecentInvoices.Clear();
            foreach (var inv in metrics.RecentInvoices) RecentInvoices.Add(inv);

            AgingDevices.Clear();
            foreach (var dev in metrics.AgingDevices) AgingDevices.Add(dev);

            LowStockItems.Clear();
            foreach (var item in metrics.LowStockItems) LowStockItems.Add(item);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
