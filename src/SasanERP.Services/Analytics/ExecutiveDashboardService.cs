using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;

namespace SasanERP.Services.Analytics;

public record DashboardMetrics(
    decimal TodaySalesTotal,
    int TodayInvoicesCount,
    decimal TodayNetProfitEstimated,
    decimal TodayCashCollected,
    decimal TodayUpiCollected,
    decimal TodayCardCollected,
    decimal TodayFinanceCollected,
    decimal TodayExchangeDeductions,
    decimal TotalStockValuationNew,
    decimal TotalStockValuationPreOwned,
    decimal TotalStockCostValuation,
    int TotalInStockDevicesCount,
    int TotalInStockBulkItemsCount,
    int LowStockAlertsCount,
    int StockAgingAlertsCount,
    decimal CustomerCreditsReceivableTotal,
    decimal MonthSalesTotal,
    decimal MonthRule32_5MarginProfit,
    decimal MonthMarginGstPayable,
    List<RecentInvoiceSummary> RecentInvoices,
    List<AgingDeviceSummary> AgingDevices,
    List<LowStockItemSummary> LowStockItems,
    decimal TotalStockValuationCombined = 0,
    int NearAgingDevicesCount = 0,
    List<AgingDeviceSummary>? NearAgingDevices = null,
    string SelectedTimeframeLabel = "Today",
    decimal PeriodSalesTotal = 0,
    int PeriodInvoicesCount = 0,
    decimal PeriodNetProfitEstimated = 0,
    decimal PeriodCashCollected = 0,
    decimal PeriodUpiCollected = 0,
    decimal PeriodCardCollected = 0,
    List<RecentInvoiceSummary>? AllTimeRecentInvoices = null
);

public enum DashboardTimeframe
{
    Today,
    Yesterday,
    ThisWeek,
    ThisMonth,
    AllTime
}

public record RecentInvoiceSummary(
    int Id,
    string InvoiceNumber,
    string CustomerName,
    DateTime InvoiceDate,
    decimal GrandTotal,
    string PaymentModes,
    bool IsMarginScheme,
    bool IsGstEnabled
);

public record AgingDeviceSummary(
    int Id,
    string DeviceName,
    string IMEI1,
    string SerialNumber,
    string ConditionGrade,
    int AgingDays,
    decimal PurchaseCost,
    decimal TaggedPrice
);

public record LowStockItemSummary(
    int Id,
    string Name,
    string SKU,
    string Category,
    int CurrentStock,
    int ReorderLevel,
    decimal SellingPrice
);

/// <summary>
/// High-Performance Executive Dashboard Analytics Engine for Sasan The Gadget Lounge ERP
/// Aggregates real-time KPIs, aging inventory, GST margins, and multi-tender collections.
/// </summary>
public class ExecutiveDashboardService
{
    private readonly AppDbContext _context;

    public ExecutiveDashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetrics> GetRealtimeMetricsAsync(DashboardTimeframe timeframe = DashboardTimeframe.Today)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTime periodStart;
        DateTime periodEnd = now.AddDays(1);
        string timeframeLabel = "Today";

        switch (timeframe)
        {
            case DashboardTimeframe.Yesterday:
                periodStart = today.AddDays(-1);
                periodEnd = today;
                timeframeLabel = "Yesterday";
                break;
            case DashboardTimeframe.ThisWeek:
                int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                periodStart = today.AddDays(-1 * diff);
                timeframeLabel = "This Week";
                break;
            case DashboardTimeframe.ThisMonth:
                periodStart = monthStart;
                timeframeLabel = "This Month";
                break;
            case DashboardTimeframe.AllTime:
                periodStart = DateTime.MinValue;
                timeframeLabel = "All Time";
                break;
            case DashboardTimeframe.Today:
            default:
                periodStart = today;
                timeframeLabel = "Today";
                break;
        }

        // 1. Period Invoices & Telemetry
        var periodInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Include(i => i.Items)
            .Where(i => i.InvoiceDate >= periodStart && i.InvoiceDate < periodEnd && !i.IsVoided)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        decimal periodSales = periodInvoices.Sum(i => i.GrandTotal);
        int periodInvoicesCount = periodInvoices.Count;

        decimal periodCash = periodInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.Cash).Sum(p => p.Amount);
        decimal periodUpi = periodInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.UPI).Sum(p => p.Amount);
        decimal periodCard = periodInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.CreditDebitCard).Sum(p => p.Amount);
        decimal periodFinance = periodInvoices.SelectMany(i => i.Payments)
            .Where(p => p.Mode == PaymentMode.BajajFinservEMI || p.Mode == PaymentMode.HdbFinanceEMI || p.Mode == PaymentMode.PineLabsEMI)
            .Sum(p => p.Amount);
        decimal periodExchange = periodInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.OldPhoneExchange).Sum(p => p.Amount);

        decimal periodCost = periodInvoices.SelectMany(i => i.Items).Sum(item => item.UnitCostPrice * item.Quantity);
        decimal periodNetProfit = Math.Max(0, periodSales - periodCost);

        // Also fetch Today metrics specifically if timeframe is different
        decimal todaySales = periodSales;
        int todayInvoicesCount = periodInvoicesCount;
        decimal todayNetProfit = periodNetProfit;
        decimal todayCash = periodCash;
        decimal todayUpi = periodUpi;
        decimal todayCard = periodCard;
        decimal todayFinance = periodFinance;
        decimal todayExchange = periodExchange;

        if (timeframe != DashboardTimeframe.Today)
        {
            var todayOnly = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Items)
                .Where(i => i.InvoiceDate >= today && !i.IsVoided)
                .ToListAsync();

            todaySales = todayOnly.Sum(i => i.GrandTotal);
            todayInvoicesCount = todayOnly.Count;
            todayCash = todayOnly.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.Cash).Sum(p => p.Amount);
            todayUpi = todayOnly.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.UPI).Sum(p => p.Amount);
            todayCard = todayOnly.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.CreditDebitCard).Sum(p => p.Amount);
            todayFinance = todayOnly.SelectMany(i => i.Payments)
                .Where(p => p.Mode == PaymentMode.BajajFinservEMI || p.Mode == PaymentMode.HdbFinanceEMI || p.Mode == PaymentMode.PineLabsEMI)
                .Sum(p => p.Amount);
            todayExchange = todayOnly.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.OldPhoneExchange).Sum(p => p.Amount);
            decimal tc = todayOnly.SelectMany(i => i.Items).Sum(item => item.UnitCostPrice * item.Quantity);
            todayNetProfit = Math.Max(0, todaySales - tc);
        }

        // 2. Month's Sales & Margin Scheme Profit
        var monthInvoices = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.InvoiceDate >= monthStart && !i.IsVoided)
            .ToListAsync();

        decimal monthSales = monthInvoices.Sum(i => i.GrandTotal);
        decimal monthMarginProfit = monthInvoices.Sum(i => i.TotalMarginAmount);
        decimal monthMarginGst = monthInvoices.Sum(i => i.TotalMarginTax);

        // 3. Inventory Valuations & Aging
        var inStockUnits = await _context.SerializedUnits
            .Include(u => u.Product)
            .Where(u => u.Status == UnitStatus.InStock)
            .ToListAsync();

        decimal valNew = inStockUnits
            .Where(u => u.ConditionGrade == ConditionGrade.BrandNew)
            .Sum(u => u.TaggedPrice > 0 ? u.TaggedPrice : (u.Product?.SellingPrice ?? 0));

        decimal valPreOwned = inStockUnits
            .Where(u => u.ConditionGrade != ConditionGrade.BrandNew)
            .Sum(u => u.TaggedPrice > 0 ? u.TaggedPrice : (u.Product?.SellingPrice ?? 0));

        decimal totalCost = inStockUnits.Sum(u => u.PurchaseCost);

        var bulkProducts = await _context.Products
            .Where(p => !p.IsSerialized)
            .ToListAsync();

        int inStockBulkCount = bulkProducts.Sum(p => p.CurrentStock);
        decimal bulkValuation = bulkProducts.Sum(p => p.CurrentStock * p.SellingPrice);
        valNew += bulkValuation;

        var agingUnits = inStockUnits
            .Where(u => u.AgingDays > 60)
            .OrderByDescending(u => u.AgingDays)
            .Select(u => new AgingDeviceSummary(
                u.Id,
                $"{u.Product?.Name ?? "Device"} ({u.Color}, {u.StorageCapacity})",
                u.IMEI1,
                u.SerialNumber,
                u.ConditionGrade.ToString(),
                u.AgingDays,
                u.PurchaseCost,
                u.TaggedPrice
            ))
            .ToList();

        var nearAgingUnits = inStockUnits
            .Where(u => u.AgingDays >= 30 && u.AgingDays <= 60)
            .OrderByDescending(u => u.AgingDays)
            .Select(u => new AgingDeviceSummary(
                u.Id,
                $"{u.Product?.Name ?? "Device"} ({u.Color}, {u.StorageCapacity})",
                u.IMEI1,
                u.SerialNumber,
                u.ConditionGrade.ToString(),
                u.AgingDays,
                u.PurchaseCost,
                u.TaggedPrice
            ))
            .ToList();

        // 4. Low Stock Alerts
        var lowStockList = await _context.Products
            .Where(p => p.CurrentStock <= p.ReorderLevel)
            .Select(p => new LowStockItemSummary(
                p.Id,
                p.Name,
                p.SKU,
                p.Category.ToString(),
                p.CurrentStock,
                p.ReorderLevel,
                p.SellingPrice
            ))
            .ToListAsync();

        // 5. Customer Credits Receivable
        decimal totalReceivables = (await _context.Customers.Select(c => c.OutstandingCredit).ToListAsync()).Sum();

        // 6. Recent Invoices
        var topInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Where(i => !i.IsVoided)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(15)
            .ToListAsync();

        var allTimeRecentInvoices = topInvoices
            .Select(i => new RecentInvoiceSummary(
                i.Id,
                i.InvoiceNumber,
                i.Customer?.FullName ?? "Walk-in Customer",
                i.InvoiceDate,
                i.GrandTotal,
                string.Join(", ", i.Payments.Select(p => p.Mode.ToString()).Distinct()),
                i.HasMarginSchemeItems,
                i.IsGstEnabled
            ))
            .ToList();

        var periodRecentInvoices = periodInvoices
            .Take(15)
            .Select(i => new RecentInvoiceSummary(
                i.Id,
                i.InvoiceNumber,
                i.Customer?.FullName ?? "Walk-in Customer",
                i.InvoiceDate,
                i.GrandTotal,
                string.Join(", ", i.Payments.Select(p => p.Mode.ToString()).Distinct()),
                i.HasMarginSchemeItems,
                i.IsGstEnabled
            ))
            .ToList();

        // If period invoices are empty, we still provide all-time recent invoices so table is informative
        var activeRecentList = periodRecentInvoices.Count > 0 ? periodRecentInvoices : allTimeRecentInvoices;

        return new DashboardMetrics(
            TodaySalesTotal: todaySales,
            TodayInvoicesCount: todayInvoicesCount,
            TodayNetProfitEstimated: todayNetProfit,
            TodayCashCollected: todayCash,
            TodayUpiCollected: todayUpi,
            TodayCardCollected: todayCard,
            TodayFinanceCollected: todayFinance,
            TodayExchangeDeductions: todayExchange,
            TotalStockValuationNew: valNew,
            TotalStockValuationPreOwned: valPreOwned,
            TotalStockCostValuation: totalCost,
            TotalInStockDevicesCount: inStockUnits.Count,
            TotalInStockBulkItemsCount: inStockBulkCount,
            LowStockAlertsCount: lowStockList.Count,
            StockAgingAlertsCount: agingUnits.Count,
            CustomerCreditsReceivableTotal: totalReceivables,
            MonthSalesTotal: monthSales,
            MonthRule32_5MarginProfit: monthMarginProfit,
            MonthMarginGstPayable: monthMarginGst,
            RecentInvoices: activeRecentList,
            AgingDevices: agingUnits,
            LowStockItems: lowStockList,
            TotalStockValuationCombined: valNew + valPreOwned,
            NearAgingDevicesCount: nearAgingUnits.Count,
            NearAgingDevices: nearAgingUnits,
            SelectedTimeframeLabel: timeframeLabel,
            PeriodSalesTotal: periodSales,
            PeriodInvoicesCount: periodInvoicesCount,
            PeriodNetProfitEstimated: periodNetProfit,
            PeriodCashCollected: periodCash,
            PeriodUpiCollected: periodUpi,
            PeriodCardCollected: periodCard,
            AllTimeRecentInvoices: allTimeRecentInvoices
        );
    }
}
