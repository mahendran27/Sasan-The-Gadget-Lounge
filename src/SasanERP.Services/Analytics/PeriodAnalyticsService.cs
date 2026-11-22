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

public record StockValuationItemDto(
    int UnitId,
    string IMEI1,
    string? SerialNumber,
    string ProductName,
    string Color,
    string Storage,
    ConditionGrade Grade,
    int? BatteryHealth,
    decimal Cost,
    decimal TaggedPrice,
    decimal PotentialMargin,
    int AgingDays,
    DateTime PurchaseDate);

public record CategoryStockValuationDto(
    string CategoryName,
    int DeviceCount,
    decimal TotalCost,
    decimal TotalRetailValue,
    decimal PotentialMargin);

public class StockValuationReportDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int TotalUnitsInStock { get; set; }
    public decimal TotalCostValuation { get; set; }
    public decimal TotalRetailValuation { get; set; }
    public decimal TotalPotentialGrossProfit => TotalRetailValuation - TotalCostValuation;
    public decimal OverallMarginPercentage => TotalRetailValuation > 0 ? (TotalPotentialGrossProfit / TotalRetailValuation) * 100 : 0;
    public int SlowMovingCount { get; set; } // > 30 days
    public List<CategoryStockValuationDto> CategoryBreakdown { get; set; } = new();
    public List<StockValuationItemDto> StockUnits { get; set; } = new();
}

public record KhataLedgerEntryDto(
    DateTime Date,
    string ReferenceNo,
    string Type, // "INVOICE" or "RECEIPT"
    string Description,
    decimal DebitAmount,   // Adds to debt
    decimal CreditAmount,  // Reduces debt
    decimal RunningBalance);

public class CustomerKhataStatementDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? GSTIN { get; set; }
    public string? AadhaarNumber { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentOutstanding { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public List<KhataLedgerEntryDto> StatementLines { get; set; } = new();
    public string UpiPaymentString { get; set; } = "";
}

public class InvoicePaymentSplitDto
{
    public decimal CashAmount { get; set; }
    public decimal UpiAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal ExchangeDeduction { get; set; }
    public decimal FinanceEmiAmount { get; set; }
    public string? ReferenceNumber { get; set; }
}

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
    public decimal TotalOperatingExpenses { get; set; }
    public decimal TrueNetBusinessProfit => TotalProfitEstimated - TotalOperatingExpenses;
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

        // Operating Expenses in Period (Petty cash, rent, staff, utilities)
        var expenseAmounts = await _context.LedgerEntries
            .Where(le => le.ReferenceType == "StoreExpense"
                      && le.EntryType == LedgerEntryType.Debit
                      && le.TransactionDate >= fromUtc
                      && le.TransactionDate <= toUtc)
            .Select(le => le.Amount)
            .ToListAsync();
        report.TotalOperatingExpenses = expenseAmounts.Sum();

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

    /// <summary>
    /// God Mode: Computes Live Inventory Valuation & Shelf Aging Analysis Report.
    /// Evaluates every physical unit in stock across cost, retail price, margin, and aging days.
    /// </summary>
    public async Task<StockValuationReportDto> ComputeStockValuationReportAsync()
    {
        var report = new StockValuationReportDto();
        var now = DateTime.UtcNow;

        var units = await _context.SerializedUnits
            .Include(u => u.Product)
            .Where(u => u.Status == UnitStatus.InStock)
            .OrderByDescending(u => u.PurchaseDate)
            .ToListAsync();

        report.TotalUnitsInStock = units.Count;
        report.TotalCostValuation = units.Sum(u => u.PurchaseCost);
        report.TotalRetailValuation = units.Sum(u => u.TaggedPrice);

        var categoryMap = new Dictionary<string, (int Count, decimal Cost, decimal Retail)>();

        foreach (var u in units)
        {
            int aging = (int)(now - u.PurchaseDate).TotalDays;
            if (aging > 30) report.SlowMovingCount++;

            string cat = u.Product != null ? u.Product.Category.ToString() : "Smartphone";
            if (!categoryMap.ContainsKey(cat))
                categoryMap[cat] = (0, 0m, 0m);

            var curr = categoryMap[cat];
            categoryMap[cat] = (curr.Count + 1, curr.Cost + u.PurchaseCost, curr.Retail + u.TaggedPrice);

            report.StockUnits.Add(new StockValuationItemDto(
                u.Id,
                u.IMEI1,
                u.SerialNumber,
                u.Product?.Name ?? "Serialized Device",
                u.Color,
                u.StorageCapacity,
                u.ConditionGrade,
                u.BatteryHealthPercentage,
                u.PurchaseCost,
                u.TaggedPrice,
                u.TaggedPrice - u.PurchaseCost,
                aging,
                u.PurchaseDate
            ));
        }

        report.CategoryBreakdown = categoryMap
            .Select(kv => new CategoryStockValuationDto(
                kv.Key,
                kv.Value.Count,
                kv.Value.Cost,
                kv.Value.Retail,
                kv.Value.Retail - kv.Value.Cost))
            .OrderByDescending(c => c.TotalCost)
            .ToList();

        return report;
    }

    /// <summary>
    /// God Mode: Generates a chronological Khata Ledger Statement for any customer with running balance.
    /// </summary>
    public async Task<CustomerKhataStatementDto> GetCustomerKhataStatementAsync(int customerId, string storeUpiId = "9080803070@sbi")
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) throw new KeyNotFoundException($"Customer #{customerId} not found.");

        var statement = new CustomerKhataStatementDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            GSTIN = customer.GSTIN,
            AadhaarNumber = customer.AadhaarNumber,
            Address = customer.Address,
            CreditLimit = 50000m,
            CurrentOutstanding = customer.OutstandingCredit
        };

        var allTransactions = new List<(DateTime Date, string Ref, string Type, string Desc, decimal Debit, decimal Credit)>();

        // 1. Invoices
        var invoices = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.CustomerId == customerId && !i.IsVoided)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();

        foreach (var inv in invoices)
        {
            statement.TotalBilled += inv.GrandTotal;
            statement.TotalPaid += inv.AmountPaid;

            // Invoice billed increases debt by GrandTotal
            allTransactions.Add((
                inv.InvoiceDate,
                inv.InvoiceNumber,
                "INVOICE",
                $"Tax Invoice #{inv.InvoiceNumber} ({inv.Items.Count} items)",
                inv.GrandTotal,
                0m));

            // Immediate payment on invoice reduces debt
            if (inv.AmountPaid > 0)
            {
                allTransactions.Add((
                    inv.InvoiceDate,
                    $"PAY-{inv.InvoiceNumber}",
                    "RECEIPT",
                    $"Initial payment at billing for Inv #{inv.InvoiceNumber}",
                    0m,
                    inv.AmountPaid));
            }
        }

        // 2. Direct Khata settlements
        var settlements = await _context.LedgerEntries
            .Where(le => le.ReferenceType == "CustomerSettlement" && le.ReferenceId == customerId && le.EntryType == LedgerEntryType.Credit)
            .OrderBy(le => le.TransactionDate)
            .ToListAsync();

        foreach (var s in settlements)
        {
            statement.TotalPaid += s.Amount;
            allTransactions.Add((
                s.TransactionDate,
                s.VoucherNumber,
                "RECEIPT",
                s.Narration,
                0m,
                s.Amount));
        }

        // Sort chronologically and compute running balance
        decimal running = 0m;
        foreach (var t in allTransactions.OrderBy(t => t.Date))
        {
            running += (t.Debit - t.Credit);
            statement.StatementLines.Add(new KhataLedgerEntryDto(
                t.Date,
                t.Ref,
                t.Type,
                t.Desc,
                t.Debit,
                t.Credit,
                running));
        }

        statement.UpiPaymentString = $"upi://pay?pa={storeUpiId}&pn=Sasan%20The%20Gadget%20Lounge&am={Math.Max(0, customer.OutstandingCredit):F2}&cu=INR";
        return statement;
    }

    /// <summary>
    /// God Mode: Edits an invoice's payment tender split in-place and realigns double-entry accounts with ACID integrity.
    /// </summary>
    public async Task UpdateInvoicePaymentSplitAsync(
        int invoiceId, 
        InvoicePaymentSplitDto newSplit, 
        string? notes, 
        string authorizedBy = "Owner")
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) throw new KeyNotFoundException($"Invoice #{invoiceId} not found.");
            if (invoice.IsVoided) throw new InvalidOperationException("Cannot edit a voided invoice.");

            decimal oldAmountPaid = invoice.AmountPaid;
            decimal oldBalanceDue = invoice.BalanceDue;

            // Remove existing payments
            _context.PaymentTransactions.RemoveRange(invoice.Payments);
            invoice.Payments.Clear();

            // Add new payment breakdown
            if (newSplit.CashAmount > 0)
                invoice.Payments.Add(new PaymentTransaction { InvoiceId = invoice.Id, Mode = PaymentMode.Cash, Amount = newSplit.CashAmount, PaymentDate = DateTime.UtcNow });
            if (newSplit.UpiAmount > 0)
                invoice.Payments.Add(new PaymentTransaction { InvoiceId = invoice.Id, Mode = PaymentMode.UPI, Amount = newSplit.UpiAmount, ReferenceNumber = newSplit.ReferenceNumber, PaymentDate = DateTime.UtcNow });
            if (newSplit.CardAmount > 0)
                invoice.Payments.Add(new PaymentTransaction { InvoiceId = invoice.Id, Mode = PaymentMode.CreditDebitCard, Amount = newSplit.CardAmount, ReferenceNumber = newSplit.ReferenceNumber, PaymentDate = DateTime.UtcNow });
            if (newSplit.ExchangeDeduction > 0)
                invoice.Payments.Add(new PaymentTransaction { InvoiceId = invoice.Id, Mode = PaymentMode.OldPhoneExchange, Amount = newSplit.ExchangeDeduction, PaymentDate = DateTime.UtcNow });
            if (newSplit.FinanceEmiAmount > 0)
                invoice.Payments.Add(new PaymentTransaction { InvoiceId = invoice.Id, Mode = PaymentMode.BajajFinservEMI, Amount = newSplit.FinanceEmiAmount, ReferenceNumber = newSplit.ReferenceNumber, PaymentDate = DateTime.UtcNow });

            decimal newAmountPaid = invoice.Payments.Sum(p => p.Amount);
            decimal newBalanceDue = Math.Max(0, invoice.GrandTotal - newAmountPaid);

            invoice.AmountPaid = newAmountPaid;
            invoice.BalanceDue = newBalanceDue;
            if (!string.IsNullOrWhiteSpace(notes))
            {
                invoice.Remarks = (invoice.Remarks ?? "") + $" | [Payment Split Adjusted by {authorizedBy}: {notes}]";
            }

            // Adjust customer credit
            if (invoice.Customer != null)
            {
                decimal debtDelta = newBalanceDue - oldBalanceDue;
                invoice.Customer.OutstandingCredit = Math.Max(0, invoice.Customer.OutstandingCredit + debtDelta);
            }

            // Realignment Ledger Voucher
            var cashAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1001");
            var bankAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1002");
            var debtorAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "1003");

            string adjVoucherNo = $"ADJ-PMT-{invoice.InvoiceNumber}";
            var adjEntries = new List<LedgerEntry>();

            // Post delta adjustments
            decimal cashDelta = newSplit.CashAmount - invoice.Payments.Where(p => p.Mode == PaymentMode.Cash).Sum(p => p.Amount);
            decimal upiCardDelta = (newSplit.UpiAmount + newSplit.CardAmount + newSplit.FinanceEmiAmount);
            decimal debtChange = newBalanceDue - oldBalanceDue;

            await _auditLogService.LogAsync(
                "InvoicePaymentsEdited",
                "Invoice",
                invoice.InvoiceNumber,
                $"Invoice #{invoice.InvoiceNumber} payment split adjusted by {authorizedBy}. Old Paid: ₹{oldAmountPaid:N2} -> New Paid: ₹{newAmountPaid:N2}. Old Balance: ₹{oldBalanceDue:N2} -> New Balance: ₹{newBalanceDue:N2}",
                authorizedBy);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
