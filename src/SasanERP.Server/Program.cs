using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Data.Seeding;
using SasanERP.Services.Accounting;
using SasanERP.Services.Analytics;
using SasanERP.Services.Backup;
using SasanERP.Services.Barcode;
using SasanERP.Services.Billing;
using SasanERP.Services.Invoicing;
using SasanERP.Services.Printing;
using SasanERP.Services.Reporting;
using SasanERP.Services.Security;
using SasanERP.Services.Wireless;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for Localhost Desktop WebView2 & Local Wi-Fi Tablet clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDesktop", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure JSON formatting
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// SQLite Database Configuration
string dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SasanTheGadgetLounge",
    "SasanERP.db");

string? dbDir = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
{
    Directory.CreateDirectory(dbDir);
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite($"Data Source={dbPath}");
});

// Register Core Services
builder.Services.AddSingleton<BarcodeGeneratorService>();
builder.Services.AddSingleton<InvoicePrintEngine>();
builder.Services.AddSingleton<BarcodeLabelEngine>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<GstReportService>();
builder.Services.AddScoped<ExecutiveDashboardService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<DatabaseBackupService>();
builder.Services.AddScoped<InvoiceHistoryService>();
builder.Services.AddScoped<PeriodAnalyticsService>();
builder.Services.AddScoped<StoreExpenseService>();
builder.Services.AddSingleton<ExecutiveReportPdfEngine>();
builder.Services.AddSingleton<NfcTagService>();
builder.Services.AddSingleton<UhfRfidService>();
builder.Services.AddScoped<StockAuditService>();
builder.Services.AddSingleton<UnifiedWirelessManager>();

var app = builder.Build();

app.UseCors("AllowLocalDesktop");
app.UseDefaultFiles();
app.UseStaticFiles();

// Ensure Database is Created and Seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
    await DbInitializer.InitializeAsync(context);
}

// =========================================================================
// 1. DASHBOARD & TELEMETRY ENDPOINTS
// =========================================================================

app.MapGet("/api/dashboard/telemetry", async (
    [FromQuery] string? timeframe,
    ExecutiveDashboardService dashboardService) =>
{
    var tf = timeframe?.ToLower() switch
    {
        "yesterday" => DashboardTimeframe.Yesterday,
        "thisweek" or "week" => DashboardTimeframe.ThisWeek,
        "thismonth" or "month" => DashboardTimeframe.ThisMonth,
        _ => DashboardTimeframe.Today
    };

    var metrics = await dashboardService.GetRealtimeMetricsAsync(tf);

    return Results.Ok(new
    {
        timeframe = tf.ToString(),
        todayRevenue = metrics.TodaySalesTotal,
        netGrossProfit = metrics.TodayNetProfitEstimated,
        totalStockValuation = metrics.TotalStockValuationNew + metrics.TotalStockValuationPreOwned,
        khataReceivables = metrics.CustomerCreditsReceivableTotal,
        cashInDrawer = metrics.TodayCashCollected,
        upiBankInflow = metrics.TodayUpiCollected,
        agingDevicesCount = metrics.StockAgingAlertsCount,
        lowStockAlertsCount = metrics.LowStockAlertsCount,
        agingDevices = metrics.AgingDevices,
        lowStockItems = metrics.LowStockItems,
        recentInvoices = metrics.RecentInvoices,
        status = "ACID Online (SQLite)"
    });
});

// =========================================================================
// 2. PRODUCTS & INVENTORY ENDPOINTS
// =========================================================================

app.MapGet("/api/products/search", async (
    [FromQuery] string? query,
    AppDbContext context) =>
{
    string q = (query ?? string.Empty).Trim().ToLower();

    var products = await context.Products
        .AsNoTracking()
        .Where(p => string.IsNullOrEmpty(q) ||
                    p.Name.ToLower().Contains(q) ||
                    p.SKU.ToLower().Contains(q) ||
                    (p.Barcode != null && p.Barcode.Contains(q)))
        .Take(50)
        .ToListAsync();

    var units = await context.SerializedUnits
        .Include(u => u.Product)
        .AsNoTracking()
        .Where(u => u.Status == UnitStatus.InStock &&
                    (string.IsNullOrEmpty(q) ||
                     u.IMEI1.Contains(q) ||
                     (u.IMEI2 != null && u.IMEI2.Contains(q)) ||
                     (u.SerialNumber != null && u.SerialNumber.Contains(q)) ||
                     u.Product.Name.ToLower().Contains(q)))
        .Take(50)
        .ToListAsync();

    return Results.Ok(new
    {
        bulkProducts = products,
        serializedUnits = units.Select(u => new
        {
            u.Id,
            u.ProductId,
            ProductName = u.Product?.Name ?? "Serialized Device",
            u.IMEI1,
            u.IMEI2,
            u.SerialNumber,
            u.Color,
            u.StorageCapacity,
            Condition = u.ConditionGrade.ToString(),
            u.BatteryHealthPercentage,
            u.PurchaseCost,
            SellingPrice = u.TaggedPrice,
            u.AgingDays,
            u.Status
        })
    });
});

app.MapGet("/api/inventory/units", async (
    [FromQuery] string? status,
    [FromQuery] string? condition,
    AppDbContext context) =>
{
    var q = context.SerializedUnits.Include(u => u.Product).AsNoTracking();

    if (!string.IsNullOrEmpty(status) && Enum.TryParse<UnitStatus>(status, true, out var st))
    {
        q = q.Where(u => u.Status == st);
    }
    else
    {
        q = q.Where(u => u.Status == UnitStatus.InStock);
    }

    var units = await q.OrderByDescending(u => u.PurchaseDate).Take(150).ToListAsync();
    return Results.Ok(units.Select(u => new
    {
        u.Id,
        u.ProductId,
        ProductName = u.Product?.Name ?? "Device",
        u.IMEI1,
        u.IMEI2,
        u.SerialNumber,
        u.Color,
        u.StorageCapacity,
        Condition = u.ConditionGrade.ToString(),
        u.BatteryHealthPercentage,
        u.PurchaseCost,
        u.TaggedPrice,
        u.AgingDays,
        u.Status
    }));
});

app.MapPost("/api/inventory/intake-unit", async (
    [FromBody] IntakeUnitRequest request,
    AppDbContext context) =>
{
    var product = await context.Products.FindAsync(request.ProductId);
    if (product == null)
    {
        product = new Product
        {
            Name = request.ProductName,
            Brand = request.Brand ?? "Generic",
            Category = request.Category,
            IsSerialized = true,
            SKU = $"SKU-{DateTime.UtcNow.Ticks % 1000000}",
            DefaultCostPrice = request.PurchaseCost,
            SellingPrice = request.TaggedPrice,
            CurrentStock = 1,
            ReorderLevel = 1
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
    }
    else
    {
        product.CurrentStock++;
    }

    var unit = new SerializedUnit
    {
        ProductId = product.Id,
        IMEI1 = request.IMEI1,
        IMEI2 = request.IMEI2,
        SerialNumber = request.SerialNumber ?? string.Empty,
        Color = request.Color ?? "Space Gray",
        StorageCapacity = request.StorageCapacity ?? "128GB",
        ConditionGrade = request.ConditionGrade,
        BatteryHealthPercentage = request.BatteryHealthPercentage,
        PurchaseCost = request.PurchaseCost,
        TaggedPrice = request.TaggedPrice,
        PurchaseDate = DateTime.UtcNow,
        Status = UnitStatus.InStock
    };

    context.SerializedUnits.Add(unit);
    await context.SaveChangesAsync();
    return Results.Ok(new { success = true, unitId = unit.Id, message = "Device safely taken into stock." });
});

// =========================================================================
// 3. POS CHECKOUT & CUSTOMERS
// =========================================================================

app.MapGet("/api/pos/customers", async (
    [FromQuery] string? search,
    AppDbContext context) =>
{
    var q = context.Customers.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(search))
    {
        string s = search.Trim().ToLower();
        q = q.Where(c => c.FullName.ToLower().Contains(s) || c.PhoneNumber.Contains(s));
    }

    var customers = await q.ToListAsync();
    var sorted = customers.OrderByDescending(c => c.OutstandingCredit).ThenBy(c => c.FullName).Take(50).ToList();
    return Results.Ok(sorted);
});

app.MapPost("/api/pos/customers", async (
    [FromBody] Customer customer,
    AppDbContext context) =>
{
    if (customer.Id == 0)
    {
        context.Customers.Add(customer);
    }
    else
    {
        var existing = await context.Customers.FindAsync(customer.Id);
        if (existing != null)
        {
            existing.FullName = customer.FullName;
            existing.PhoneNumber = customer.PhoneNumber;
            existing.Email = customer.Email;
            existing.GSTIN = customer.GSTIN;
            existing.Address = customer.Address;
        }
    }
    await context.SaveChangesAsync();
    return Results.Ok(customer);
});

app.MapPost("/api/pos/invoice/checkout", async (
    [FromBody] CheckoutRequest request,
    BillingService billingService,
    AppDbContext context) =>
{
    var customer = await context.Customers.FindAsync(request.CustomerId);
    if (customer == null)
    {
        customer = await context.Customers.FirstOrDefaultAsync() ?? new Customer { FullName = "Walk-in Retail Customer", PhoneNumber = "9080803070" };
    }

    string invNumber = await billingService.GenerateNextInvoiceNumberAsync();
    bool hasMargin = request.Items.Any(i => i.IsMarginScheme);
    decimal grossTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
    decimal taxableValue = request.Items.Sum(i => i.TaxableValue);
    decimal totalCgst = request.Items.Sum(i => i.CgstAmount);
    decimal totalSgst = request.Items.Sum(i => i.SgstAmount);
    decimal totalMargin = request.Items.Where(i => i.IsMarginScheme).Sum(i => i.MarginAmount);
    decimal grandTotal = request.Items.Sum(i => i.LineTotal) - request.InvoiceDiscount;

    var invoice = new Invoice
    {
        InvoiceNumber = invNumber,
        CustomerId = customer.Id,
        Customer = customer,
        IsGstEnabled = request.IsGstEnabled,
        InvoiceType = !request.IsGstEnabled ? InvoiceType.BillOfSupply : (hasMargin ? InvoiceType.MarginSchemeInvoice : InvoiceType.TaxInvoice),
        HasMarginSchemeItems = request.IsGstEnabled && hasMargin,
        GrossTotal = grossTotal,
        InvoiceDiscount = request.InvoiceDiscount,
        StandardTaxableValue = request.IsGstEnabled ? taxableValue : grossTotal,
        TotalCGST = request.IsGstEnabled ? totalCgst : 0,
        TotalSGST = request.IsGstEnabled ? totalSgst : 0,
        TotalMarginAmount = request.IsGstEnabled ? totalMargin : 0,
        TotalMarginTax = 0,
        GrandTotal = grandTotal,
        RoundOff = 0
    };

    foreach (var item in request.Items)
    {
        invoice.Items.Add(new InvoiceItem
        {
            ProductId = item.ProductId,
            SerializedUnitId = item.SerializedUnitId,
            ItemDescription = item.Name,
            HSNCode = string.IsNullOrWhiteSpace(item.HsnCode) ? "8517" : item.HsnCode,
            IMEI1 = item.Imei1,
            IMEI2 = item.Imei2,
            SerialNumber = item.SerialNumber,
            BatteryHealthPercentage = item.BatteryHealthPercentage,
            ConditionGrade = item.ConditionGrade,
            WarrantyStatus = item.WarrantyStatus,
            Quantity = item.Quantity <= 0 ? 1 : item.Quantity,
            UnitPrice = item.UnitPrice,
            ItemDiscount = item.Discount,
            UnitCostPrice = item.UnitCostPrice,
            IsMarginScheme = item.IsMarginScheme,
            MarginAmount = item.MarginAmount,
            GSTPercentage = item.GstPercentage,
            TaxableValue = item.TaxableValue,
            CGSTPercentage = item.GstPercentage / 2m,
            CGSTAmount = item.CgstAmount,
            SGSTPercentage = item.GstPercentage / 2m,
            SGSTAmount = item.SgstAmount,
            LineTotal = item.LineTotal
        });
    }

    var payments = request.Payments.Select(p => new PaymentTransaction
    {
        Mode = p.Mode,
        Amount = p.Amount,
        ReferenceNumber = p.ReferenceNumber
    }).ToList();

    var savedInvoice = await billingService.ProcessCheckoutAsync(invoice, payments, request.CashierName ?? "Karthik Raja");

    return Results.Ok(new
    {
        success = true,
        invoiceId = savedInvoice.Id,
        invoiceNumber = savedInvoice.InvoiceNumber,
        grandTotal = savedInvoice.GrandTotal,
        balanceDue = savedInvoice.BalanceDue,
        message = $"Invoice #{savedInvoice.InvoiceNumber} generated successfully!"
    });
});

// =========================================================================
// 4. REPORTS, EXCEL & PDF EXPORTS
// =========================================================================

app.MapGet("/api/reports/live-stock", async (
    PeriodAnalyticsService analyticsService) =>
{
    var report = await analyticsService.ComputeStockValuationReportAsync();
    return Results.Ok(report);
});

app.MapGet("/api/reports/customer-khata/{customerId:int}", async (
    int customerId,
    PeriodAnalyticsService analyticsService) =>
{
    var statement = await analyticsService.GetCustomerKhataStatementAsync(customerId);
    return Results.Ok(statement);
});

app.MapGet("/api/reports/live-stock-pdf", async (
    PeriodAnalyticsService analyticsService,
    ExecutiveReportPdfEngine pdfEngine,
    AppDbContext context) =>
{
    var report = await analyticsService.ComputeStockValuationReportAsync();
    var company = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    string tempFile = Path.Combine(Path.GetTempPath(), $"StockValuation_{DateTime.UtcNow.Ticks}.pdf");
    pdfEngine.GenerateStockValuationReportPdf(report, company, tempFile);
    byte[] pdf = await File.ReadAllBytesAsync(tempFile);
    try { File.Delete(tempFile); } catch { }
    return Results.File(pdf, "application/pdf", "Executive_Live_Stock_Valuation_Report.pdf");
});

app.MapGet("/api/reports/shift-sales-pdf", async (
    [FromQuery] string? timeframe,
    PeriodAnalyticsService analyticsService,
    ExecutiveReportPdfEngine pdfEngine,
    AppDbContext context) =>
{
    DateTime toUtc = DateTime.UtcNow;
    DateTime fromUtc = timeframe?.ToLower() switch
    {
        "yesterday" => DateTime.UtcNow.Date.AddDays(-1),
        "thisweek" => DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek),
        "thismonth" => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc),
        _ => DateTime.UtcNow.Date
    };

    var report = await analyticsService.ComputePeriodReportAsync(fromUtc, toUtc);
    var company = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    string tempFile = Path.Combine(Path.GetTempPath(), $"ShiftSales_{DateTime.UtcNow.Ticks}.pdf");
    pdfEngine.GeneratePeriodSalesReportPdf(report, company, tempFile);
    byte[] pdf = await File.ReadAllBytesAsync(tempFile);
    try { File.Delete(tempFile); } catch { }
    return Results.File(pdf, "application/pdf", "Executive_Shift_Sales_Audit_Report.pdf");
});

app.MapGet("/api/reports/customer-khata-pdf/{customerId:int}", async (
    int customerId,
    PeriodAnalyticsService analyticsService,
    ExecutiveReportPdfEngine pdfEngine,
    AppDbContext context) =>
{
    var statement = await analyticsService.GetCustomerKhataStatementAsync(customerId);
    var company = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    string tempFile = Path.Combine(Path.GetTempPath(), $"CustomerKhata_{customerId}_{DateTime.UtcNow.Ticks}.pdf");
    pdfEngine.GenerateCustomerKhataStatementPdf(statement, company, tempFile);
    byte[] pdf = await File.ReadAllBytesAsync(tempFile);
    try { File.Delete(tempFile); } catch { }
    return Results.File(pdf, "application/pdf", $"Customer_Khata_Statement_{customerId}.pdf");
});

// =========================================================================
// 5. STORE EXPENSES & DAYBOOK
// =========================================================================

app.MapGet("/api/expenses/daybook", async (
    StoreExpenseService expenseService,
    ExecutiveDashboardService dashboardService) =>
{
    var summary = await expenseService.GetExpensesAsync(DateTime.UtcNow.Date, DateTime.UtcNow);
    var metrics = await dashboardService.GetRealtimeMetricsAsync(DashboardTimeframe.Today);

    return Results.Ok(new
    {
        cashInTill = metrics.TodayCashCollected,
        upiQrCollections = metrics.TodayUpiCollected,
        todayExpensesTotal = summary.TotalExpenses,
        expensesList = summary.Entries
    });
});

app.MapPost("/api/expenses", async (
    [FromBody] ExpenseVoucherRequest request,
    StoreExpenseService expenseService) =>
{
    string voucher = await expenseService.RecordExpenseAsync(
        request.AccountCode,
        request.Amount,
        request.PaymentMode,
        request.Description,
        request.CreatedBy ?? "Karthik Raja");

    return Results.Ok(new { success = true, voucherNumber = voucher });
});

// =========================================================================
// 6. STORE SETTINGS
// =========================================================================

app.MapGet("/api/settings/company", async (AppDbContext context) =>
{
    var company = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    return Results.Ok(company);
});

app.MapPost("/api/settings/company", async (
    [FromBody] CompanyProfile profile,
    AppDbContext context) =>
{
    var existing = await context.CompanyProfiles.FirstOrDefaultAsync();
    if (existing == null)
    {
        context.CompanyProfiles.Add(profile);
    }
    else
    {
        existing.BusinessName = profile.BusinessName;
        existing.Tagline = profile.Tagline;
        existing.AddressLine1 = profile.AddressLine1;
        existing.AddressLine2 = profile.AddressLine2;
        existing.City = profile.City;
        existing.State = profile.State;
        existing.Pincode = profile.Pincode;
        existing.PhoneNumber = profile.PhoneNumber;
        existing.Email = profile.Email;
        existing.GSTIN = profile.GSTIN;
        existing.BankName = profile.BankName;
        existing.BankAccountNumber = profile.BankAccountNumber;
        existing.BankIFSC = profile.BankIFSC;
        existing.BankBranch = profile.BankBranch;
        existing.UpiId = profile.UpiId;
        existing.TermsAndConditions = profile.TermsAndConditions;
        existing.IsMonochromePrintMode = profile.IsMonochromePrintMode;
    }
    await context.SaveChangesAsync();
    return Results.Ok(new { success = true, message = "Store profile persisted successfully." });
});

// =========================================================================
// 7. PRINTING & BARCODES
// =========================================================================

app.MapPost("/api/print/barcode-label", (
    [FromBody] BarcodeLabelPrintRequest req,
    BarcodeLabelEngine labelEngine) =>
{
    var data = new BarcodeStickerData(
        req.StoreName ?? "Sasan The Gadget Lounge",
        req.ItemName,
        req.Specs ?? string.Empty,
        req.BarcodeOrImei,
        req.SellingPrice,
        req.SellingPrice * 1.15m,
        "Grade A",
        98,
        "30-Day Shop Warranty",
        "Smartphone"
    );

    byte[] pdf = labelEngine.GeneratePdfBytes(req.Style, data);
    return Results.File(pdf, "application/pdf", "BarcodeLabel.pdf");
});

app.MapGet("/api/print/invoice-pdf/{invoiceId:int}", async (
    int invoiceId,
    [FromQuery] string? template,
    AppDbContext context,
    InvoicePrintEngine printEngine) =>
{
    var invoice = await context.Invoices
        .Include(i => i.Items)
        .Include(i => i.Payments)
        .Include(i => i.Customer)
        .FirstOrDefaultAsync(i => i.Id == invoiceId);

    if (invoice == null) return Results.NotFound();

    var company = await context.CompanyProfiles.FirstOrDefaultAsync() ?? new CompanyProfile();
    var style = InvoiceTemplateStyle.LuxuryGadgetLounge;
    if (!string.IsNullOrEmpty(template) && Enum.TryParse<InvoiceTemplateStyle>(template, true, out var st))
    {
        style = st;
    }

    byte[] pdf = printEngine.GeneratePdfBytes(style, invoice, company);
    return Results.File(pdf, "application/pdf", $"Invoice_{invoice.InvoiceNumber.Replace('/', '_')}.pdf");
});

// =========================================================================
// 8. GOD MODE ACTIONS
// =========================================================================

app.MapPost("/api/godmode/void-invoice/{invoiceId:int}", async (
    int invoiceId,
    [FromQuery] string? reason,
    PeriodAnalyticsService analyticsService) =>
{
    await analyticsService.VoidInvoiceAsync(invoiceId, reason ?? "God Mode Void Request", "OwnerKarthik");
    return Results.Ok(new { success = true, message = $"Invoice #{invoiceId} has been voided and ledger reversed." });
});

app.MapPost("/api/godmode/settle-khata", async (
    [FromBody] KhataSettlementRequest req,
    PeriodAnalyticsService analyticsService) =>
{
    await analyticsService.SettleCustomerCreditAsync(
        req.CustomerId,
        req.Amount,
        req.PaymentMode,
        req.Notes ?? "Khata Credit Settlement",
        "Karthik Raja");

    return Results.Ok(new { success = true, message = $"Settled ₹{req.Amount:N2} for Customer #{req.CustomerId}." });
});

app.MapFallbackToFile("index.html");

app.Run();

// Export Program type for WebApplicationFactory<Program> in Tests
public partial class Program { }

// DTO Records
public record IntakeUnitRequest(
    int ProductId,
    string ProductName,
    string? Brand,
    DeviceType Category,
    string IMEI1,
    string? IMEI2,
    string? SerialNumber,
    string? Color,
    string? StorageCapacity,
    ConditionGrade ConditionGrade,
    int BatteryHealthPercentage,
    decimal PurchaseCost,
    decimal TaggedPrice
);

public record CheckoutItemRequest(
    int ProductId,
    int? SerializedUnitId,
    string Name,
    string HsnCode,
    string? Imei1,
    string? Imei2,
    string? SerialNumber,
    int? BatteryHealthPercentage,
    ConditionGrade? ConditionGrade,
    WarrantyStatus? WarrantyStatus,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal UnitCostPrice,
    bool IsMarginScheme,
    decimal MarginAmount,
    decimal GstPercentage,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal LineTotal
);

public record CheckoutPaymentRequest(
    PaymentMode Mode,
    decimal Amount,
    string? ReferenceNumber
);

public record CheckoutRequest(
    int CustomerId,
    List<CheckoutItemRequest> Items,
    List<CheckoutPaymentRequest> Payments,
    decimal InvoiceDiscount,
    bool IsGstEnabled,
    string? CashierName
);

public record ExpenseVoucherRequest(
    string AccountCode,
    decimal Amount,
    PaymentMode PaymentMode,
    string Description,
    string CreatedBy
);

public record BarcodeLabelPrintRequest(
    BarcodeLabelStyle Style,
    string StoreName,
    string ItemName,
    string BarcodeOrImei,
    decimal SellingPrice,
    string Specs
);

public record KhataSettlementRequest(
    int CustomerId,
    decimal Amount,
    PaymentMode PaymentMode,
    string Notes
);
