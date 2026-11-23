using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Data.Seeding;
using SasanERP.Server;
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

app.MapSasanEndpoints();
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
