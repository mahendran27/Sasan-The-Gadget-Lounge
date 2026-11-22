using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Invoicing;
using SasanERP.Services.Security;

namespace SasanERP.Services.Analytics;

public record DeviceSalesSummaryDto(
    string DeviceName,
    int QuantitySold,
    decimal TotalRevenue,
    decimal TotalProfit);

public class PeriodReportDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int TotalInvoicesCount { get; set; }
    public int TotalItemsSoldCount { get; set; }
    public decimal TotalGrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalNetRevenue { get; set; }
    public decimal TotalProfitEstimated { get; set; }
    public decimal CashCollected { get; set; }
    public decimal UpiCollected { get; set; }
    public decimal CardCollected { get; set; }
    public decimal ExchangeDeductions { get; set; }
    public decimal FinanceEmiCollected { get; set; }
    public decimal CustomerCreditAdded { get; set; }
    public decimal StandardTaxCollected { get; set; }
    public decimal MarginTaxCollected { get; set; }
    public int ActiveStockUnitsCount { get; set; }
    public decimal ActiveStockValuationCost { get; set; }
    public decimal ActiveStockValuationRetail { get; set; }
    public List<InvoiceSummaryDto> InvoicesInPeriod { get; set; } = new();
    public List<DeviceSalesSummaryDto> TopDevicesSold { get; set; } = new();
}

/// <summary>
/// Omnipotent Business Intelligence & Administrative Service ("God Mode").
/// Computes custom date/time range shift analytics, performs atomic invoice voiding with stock restoration,
/// and executes master CRUD mutations across serialized stock, products, and customer Khata ledgers.
/// </summary>
public class PeriodAnalyticsService
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;

    public PeriodAnalyticsService(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Computes full business analytics between any two date/time points down to the exact minute.
    /// </summary>
    public async Task<PeriodReportDto> ComputePeriodReportAsync(DateTime fromUtc, DateTime toUtc)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.InvoiceDate >= fromUtc && i.InvoiceDate <= toUtc && !i.IsVoided)
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        var report = new PeriodReportDto
        {
            FromUtc = fromUtc,
            ToUtc = toUtc,
            TotalInvoicesCount = invoices.Count,
            TotalGrossSales = invoices.Sum(i => i.GrossTotal),
            TotalDiscounts = invoices.Sum(i => i.InvoiceDiscount),
            TotalNetRevenue = invoices.Sum(i => i.GrandTotal),
            StandardTaxCollected = invoices.Sum(i => i.TotalCGST + i.TotalSGST + i.TotalIGST),
            MarginTaxCollected = invoices.Sum(i => i.TotalMarginTax)
        };

        decimal totalCost = 0;
        int itemsCount = 0;

        var deviceGroups = new Dictionary<string, (int Qty, decimal Revenue, decimal Profit)>();

        foreach (var inv in invoices)
        {
            foreach (var item in inv.Items)
            {
                itemsCount += (int)item.Quantity;
                decimal itemRevenue = item.LineTotal;
                decimal itemCost = item.UnitCostPrice * item.Quantity;
                totalCost += itemCost;
                decimal profit = itemRevenue - itemCost - (item.CGSTAmount + item.SGSTAmount);

                if (!deviceGroups.ContainsKey(item.ItemDescription))
                    deviceGroups[item.ItemDescription] = (0, 0, 0);

                var prev = deviceGroups[item.ItemDescription];
                deviceGroups[item.ItemDescription] = (prev.Qty + (int)item.Quantity, prev.Revenue + itemRevenue, prev.Profit + profit);
            }

            foreach (var p in inv.Payments)
            {
                switch (p.Mode)
                {
                    case PaymentMode.Cash:
                        report.CashCollected += p.Amount;
                        break;
                    case PaymentMode.UPI:
                        report.UpiCollected += p.Amount;
                        break;
                    case PaymentMode.CreditDebitCard:
                        report.CardCollected += p.Amount;
                        break;
                    case PaymentMode.OldPhoneExchange:
                        report.ExchangeDeductions += p.Amount;
                        break;
                    case PaymentMode.BajajFinservEMI:
                    case PaymentMode.HdbFinanceEMI:
                    case PaymentMode.PineLabsEMI:
                        report.FinanceEmiCollected += p.Amount;
                        break;
                }
            }

            report.CustomerCreditAdded += inv.BalanceDue;

            report.InvoicesInPeriod.Add(new InvoiceSummaryDto(
                inv.Id,
                inv.InvoiceNumber,
                inv.InvoiceDate,
                inv.Customer?.FullName ?? "Walk-in",
                inv.Customer?.PhoneNumber ?? "N/A",
                inv.InvoiceType,
                inv.HasMarginSchemeItems,
                inv.Items.Count,
                inv.GrandTotal,
                inv.AmountPaid,
                inv.BalanceDue,
                string.Join(", ", inv.Payments.Select(p => p.Mode.ToString()).Distinct())
            ));
        }

        report.TotalItemsSoldCount = itemsCount;
        report.TotalProfitEstimated = report.TotalNetRevenue - totalCost - (report.StandardTaxCollected + report.MarginTaxCollected);

        report.TopDevicesSold = deviceGroups
            .Select(g => new DeviceSalesSummaryDto(g.Key, g.Value.Qty, g.Value.Revenue, g.Value.Profit))
            .OrderByDescending(d => d.TotalRevenue)
            .Take(15)
            .ToList();

        // Active Stock Telemetry
        var activeUnits = await _context.SerializedUnits
            .Where(u => u.Status == UnitStatus.InStock)
            .AsNoTracking()
            .ToListAsync();

        report.ActiveStockUnitsCount = activeUnits.Count;
        report.ActiveStockValuationCost = activeUnits.Sum(u => u.PurchaseCost);
        report.ActiveStockValuationRetail = activeUnits.Sum(u => u.TaggedPrice);

        return report;
    }

    /// <summary>
    /// God Mode: Voids an invoice atomically. Reverses double-entry ledger vouchers,
    /// sets all serialized devices back to InStock, decrements sales counts, and adjusts customer credit.
    /// </summary>
    public async Task<bool> VoidInvoiceAsync(int invoiceId, string voidReason, string authorizedBy = "Owner")
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null || invoice.IsVoided)
                return false;

            invoice.IsVoided = true;
            invoice.VoidReason = voidReason;
            invoice.VoidedDate = DateTime.UtcNow;

            // 1. Restore Serialized Units to InStock
            var soldUnits = await _context.SerializedUnits
                .Where(u => u.SoldInvoiceId == invoiceId)
                .ToListAsync();

            foreach (var unit in soldUnits)
            {
                unit.Status = UnitStatus.InStock;
                unit.SoldInvoiceId = null;
                unit.SoldDate = null;
                unit.ActualSoldPrice = null;

                var prod = await _context.Products.FindAsync(unit.ProductId);
                if (prod != null)
                {
                    prod.CurrentStock++;
                }
            }

            // Restore non-serialized bulk merchandise
            foreach (var item in invoice.Items.Where(it => !it.SerializedUnitId.HasValue))
            {
                var prod = await _context.Products.FindAsync(item.ProductId);
                if (prod != null)
                {
                    prod.CurrentStock += (int)item.Quantity;
                }
            }

            // 2. Adjust Customer Debt if there was an unpaid balance
            if (invoice.BalanceDue > 0 && invoice.Customer != null)
            {
                invoice.Customer.OutstandingCredit = Math.Max(0, invoice.Customer.OutstandingCredit - invoice.BalanceDue);
            }

            // 3. Post Balanced Ledger Reversal Vouchers
            string reversalVoucherNo = $"REV-{invoice.InvoiceNumber}";
            var reversalEntries = new List<LedgerEntry>();

            // Find matching original vouchers to reverse
            var originalVouchers = await _context.LedgerEntries
                .Where(le => le.ReferenceId == invoice.Id && le.ReferenceType == "Invoice")
                .ToListAsync();

            foreach (var orig in originalVouchers)
            {
                var revType = orig.EntryType == LedgerEntryType.Debit ? LedgerEntryType.Credit : LedgerEntryType.Debit;
                reversalEntries.Add(new LedgerEntry
                {
                    AccountId = orig.AccountId,
                    VoucherNumber = reversalVoucherNo,
                    TransactionDate = DateTime.UtcNow,
                    EntryType = revType,
                    Amount = orig.Amount,
                    Narration = $"[VOID REVERSAL] {voidReason} for Inv #{invoice.InvoiceNumber}",
                    ReferenceId = invoice.Id,
                    ReferenceType = "InvoiceVoid"
                });

                var acc = await _context.Accounts.FindAsync(orig.AccountId);
                if (acc != null)
                {
                    if (revType == LedgerEntryType.Debit) acc.CurrentBalance += orig.Amount;
                    else acc.CurrentBalance -= orig.Amount;
                }
            }

            if (reversalEntries.Any())
            {
                await _context.LedgerEntries.AddRangeAsync(reversalEntries);
            }

            // 4. Record Audit Log
            await _auditLogService.LogAsync(
                "InvoiceVoided",
                "Invoice",
                invoice.InvoiceNumber,
                $"Voided Inv #{invoice.InvoiceNumber} (Total: ₹{invoice.GrandTotal:N2}). Reason: {voidReason}. Restocked {soldUnits.Count} devices.",
                authorizedBy);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// God Mode: Directly edits any serialized unit's IMEI, Serial, Cost, Price, Battery, Grade, or Status.
    /// </summary>
    public async Task UpdateSerializedUnitAsync(
        int unitId,
        string imei1,
        string? imei2,
        string serialNumber,
        decimal cost,
        decimal taggedPrice,
        int? batteryHealth,
        ConditionGrade grade,
        UnitStatus status,
        WarrantyStatus warranty,
        string updatedBy = "Admin")
    {
        var unit = await _context.SerializedUnits
            .Include(u => u.Product)
            .FirstOrDefaultAsync(u => u.Id == unitId);

        if (unit == null) throw new KeyNotFoundException($"Device unit with ID {unitId} not found.");

        string changeNote = $"Updated {unit.Product?.Name ?? "Device"} IMEI {unit.IMEI1} -> {imei1}, Cost: ₹{unit.PurchaseCost} -> ₹{cost}, Price: ₹{unit.TaggedPrice} -> ₹{taggedPrice}, Status: {unit.Status} -> {status}";

        unit.IMEI1 = imei1;
        unit.IMEI2 = imei2;
        unit.SerialNumber = serialNumber;
        unit.PurchaseCost = cost;
        unit.TaggedPrice = taggedPrice;
        unit.BatteryHealthPercentage = batteryHealth;
        unit.ConditionGrade = grade;
        unit.Status = status;
        unit.WarrantyStatus = warranty;

        await _auditLogService.LogAsync("UnitStockEdited", "SerializedUnit", imei1, changeNote, updatedBy);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// God Mode: Adjusts bulk non-serialized accessory stock, cost price, and retail selling price.
    /// </summary>
    public async Task UpdateBulkProductStockAsync(
        int productId,
        int newStock,
        decimal costPrice,
        decimal sellingPrice,
        string updatedBy = "Admin")
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) throw new KeyNotFoundException($"Product ID {productId} not found.");

        string note = $"Stock adjusted for {product.Name}: Qty {product.CurrentStock} -> {newStock}, Cost: ₹{product.DefaultCostPrice} -> ₹{costPrice}, Price: ₹{product.SellingPrice} -> ₹{sellingPrice}";
        product.CurrentStock = newStock;
        product.DefaultCostPrice = costPrice;
        product.SellingPrice = sellingPrice;

        await _auditLogService.LogAsync("ProductStockAdjusted", "Product", product.SKU, note, updatedBy);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// God Mode: In-place update of customer profile data (Name, Phone, GSTIN, Aadhaar, Address).
    /// </summary>
    public async Task UpdateCustomerAsync(
        int customerId,
        string fullName,
        string phone,
        string? gstin,
        string? aadhaar,
        string? address,
        string updatedBy = "Admin")
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) throw new KeyNotFoundException($"Customer ID {customerId} not found.");

        customer.FullName = fullName;
        customer.PhoneNumber = phone;
        customer.GSTIN = gstin;
        customer.AadhaarNumber = aadhaar;
        customer.Address = address;

        await _auditLogService.LogAsync("CustomerEdited", "Customer", phone, $"Customer {fullName} updated by {updatedBy}", updatedBy);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// God Mode: Settles a customer's Khata credit balance with double-entry ledger voucher.
    /// </summary>
    public async Task SettleCustomerCreditAsync(
        int customerId,
        decimal amountPaid,
        PaymentMode paymentMode,
        string reference = "",
        string receivedBy = "Cashier")
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) throw new KeyNotFoundException($"Customer ID {customerId} not found.");

        if (amountPaid <= 0 || amountPaid > customer.OutstandingCredit)
            throw new InvalidOperationException($"Invalid settlement amount: ₹{amountPaid:N2}. Outstanding: ₹{customer.OutstandingCredit:N2}");

        customer.OutstandingCredit -= amountPaid;

        // Double-entry ledger: Cash/Bank Dr. to Customer Debtors Cr.
        var targetAccCode = paymentMode == PaymentMode.Cash ? "1001" : "1002";
        var receiptAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == targetAccCode);
        var debtorAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1003");

        string voucherNo = $"RCPT-KHATA-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

        if (receiptAcc != null)
        {
            _context.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = receiptAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = DateTime.UtcNow,
                EntryType = LedgerEntryType.Debit,
                Amount = amountPaid,
                Narration = $"Khata credit settlement from {customer.FullName} via {paymentMode}",
                ReferenceId = customer.Id,
                ReferenceType = "CustomerSettlement"
            });
            receiptAcc.CurrentBalance += amountPaid;
        }

        if (debtorAcc != null)
        {
            _context.LedgerEntries.Add(new LedgerEntry
            {
                AccountId = debtorAcc.Id,
                VoucherNumber = voucherNo,
                TransactionDate = DateTime.UtcNow,
                EntryType = LedgerEntryType.Credit,
                Amount = amountPaid,
                Narration = $"Khata credit clearance for {customer.FullName}",
                ReferenceId = customer.Id,
                ReferenceType = "CustomerSettlement"
            });
            debtorAcc.CurrentBalance -= amountPaid;
        }

        await _auditLogService.LogAsync(
            "CustomerKhataSettled",
            "Customer",
            customer.PhoneNumber,
            $"Received ₹{amountPaid:N2} via {paymentMode} from {customer.FullName}. Remaining Credit: ₹{customer.OutstandingCredit:N2}",
            receivedBy);

        await _context.SaveChangesAsync();
    }
}
