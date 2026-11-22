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
    List<LowStockItemSummary> LowStockItems
);

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

    public async Task<DashboardMetrics> GetRealtimeMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1. Today's Invoices & Sales
        var todayInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .Include(i => i.Items)
            .Where(i => i.InvoiceDate >= today)
            .ToListAsync();

        decimal todaySales = todayInvoices.Sum(i => i.GrandTotal);
        int todayInvoicesCount = todayInvoices.Count;

        decimal todayCash = todayInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.Cash).Sum(p => p.Amount);
        decimal todayUpi = todayInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.UPI).Sum(p => p.Amount);
        decimal todayCard = todayInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.CreditDebitCard).Sum(p => p.Amount);
        decimal todayFinance = todayInvoices.SelectMany(i => i.Payments)
            .Where(p => p.Mode == PaymentMode.BajajFinservEMI || p.Mode == PaymentMode.HdbFinanceEMI || p.Mode == PaymentMode.PineLabsEMI)
            .Sum(p => p.Amount);
        decimal todayExchange = todayInvoices.SelectMany(i => i.Payments).Where(p => p.Mode == PaymentMode.OldPhoneExchange).Sum(p => p.Amount);

        // Estimate today's profit: (Selling Price - Cost Price) across items sold today
        decimal todayCost = todayInvoices.SelectMany(i => i.Items).Sum(item => item.UnitCostPrice * item.Quantity);
        decimal todayNetProfit = Math.Max(0, todaySales - todayCost);

        // 2. Month's Sales & Margin Scheme Profit
        var monthInvoices = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.InvoiceDate >= monthStart)
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
        decimal totalReceivables = await _context.Customers.SumAsync(c => c.OutstandingCredit);

        // 6. Recent 10 Invoices
        var recentInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(10)
            .Select(i => new RecentInvoiceSummary(
                i.Id,
                i.InvoiceNumber,
                i.Customer.FullName,
                i.InvoiceDate,
                i.GrandTotal,
                string.Join(", ", i.Payments.Select(p => p.Mode.ToString()).Distinct()),
                i.HasMarginSchemeItems,
                i.IsGstEnabled
            ))
            .ToListAsync();

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
            RecentInvoices: recentInvoices,
            AgingDevices: agingUnits,
            LowStockItems: lowStockList
        );
    }
}
